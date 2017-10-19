#r "FakeLib.dll"
#load "build-helpers.fsx"

open System
open System.IO
open BuildHelpers
open Fake
open Fake.AssemblyInfoFile
open Fake.MSBuildHelper
open Fake.Testing
open Fake.TraceHelper
open Fake.XamarinHelper

// project properties
let projectName = "CodeHeroes.CodeAnalysis.Style"

// version properties
let versionInfo = getReleaseInfo "release-notes.md"

trace ("NOTES: " + versionInfo.Notes)

let semanticVersion = versionInfo.Version
let buildNumber = environVarOrDefault "BUILD_NUMBER" "0"
let buildUrl = environVarOrDefault "BUILD_URL" ""
let version = (semanticVersion + "." + buildNumber)

// can be overridden with -ev name value
let configuration = environVarOrDefault "CONFIGURATION" "Release"
let deployDir = environVarOrDefault "DEPLOY_DIR" ""


// file and directory paths
let genDir = "Gen/"
let srcDir = "Src/"
let testDir = genDir @@ "Test"
let tempDir = genDir @@ "Temp"
let solution = srcDir @@ projectName + ".sln"

let verbosity = Some(Detailed)

trace ("Starting build " + version)

Target "clean-misc" (fun _ ->
    CleanDirs[genDir; testDir; tempDir]
)

Target "clean" (fun _ ->
    build (fun defaults ->
        {
            defaults with
                Verbosity = verbosity
                Targets = ["Clean"]
                Properties = ["Configuration", configuration]
        })
        (solution)
)

// would prefer to use the built-in RestorePackages function, but it restores packages in the root dir (not in Src), which causes build problems
Target "restore-packages" (fun _ ->
    let nugetFeeds = [
        "https://api.nuget.org/v3/index.json"
    ]

    let solutions = [
        solution
    ]

    let sources =
        nugetFeeds
        |> Seq.map (fun nugetFeed -> "--source " + nugetFeed)
        |> String.concat " "

    trace("Sources: '" + sources + "'")

    let restoreForSolution solution =
        try
            trace ("Restoring packages for " + solution)
            ignore(Shell.Exec("dotnet", "restore " + sources + " " + solution))
        with
        | ex -> ()

    solutions
    |> List.iter restoreForSolution

    // restore packages in the old packages.config format (can remove this once we've transitioned completely to SDK-style projects)
    !! "./**/packages.config"
    |> Seq.iter (
        RestorePackage (fun defaults ->
            {
                defaults with
                    OutputPath = (srcDir @@ "packages")
                    Sources = nugetFeeds
            })
        )
)

Target "pre-build" (fun () ->
    CreateCSharpAssemblyInfoWithConfig (srcDir @@ "AssemblyInfoCommon.cs")
        [
            Attribute.Version version
            Attribute.FileVersion version
            Attribute.Configuration configuration
            Attribute.Company "Code Heroes"
            Attribute.Product projectName
            Attribute.Copyright "© Copyright. Code Heroes."
            Attribute.Trademark ""
            Attribute.Culture ""
            Attribute.StringAttribute("NeutralResourcesLanguage", "en-AU", "System.Resources")
            Attribute.StringAttribute("AssemblyInformationalVersion", semanticVersion, "System.Reflection")
        ]
        (AssemblyInfoFileConfig(false))
)

Target "build" (fun () ->
    build (fun defaults ->
        {
            defaults with
                Verbosity = verbosity
                Targets = ["Build"]
                Properties =
                    [
                        "Optimize", "True"
                        "DebugSymbols", "True"
                        "Configuration", configuration
                        "Version", semanticVersion
                    ]
        })
        solution
)

Target "deploy-build-copy" (fun () ->
    let nupkgFile = "Analyzers." + semanticVersion + ".nupkg"
    let nupkgPath = srcDir @@ "Analyzers" @@ "bin" @@ configuration @@ nupkgFile
    let targetFile = deployDir @@ nupkgFile
    trace ("Deploying NuGet package by copying '" + nupkgPath + "' to '" + targetFile + "'")
    if not (String.IsNullOrEmpty deployDir) then CopyFile targetFile nupkgPath
)

Target "all"
    DoNothing

Target "root"
    DoNothing

(*

Target "tag" (fun _ ->
    ignore(Shell.Exec("git", "tag v" + version))
    ignore(Shell.Exec("git", "push origin v" + version))
)

// this does not work on Mono
Target "test-core" (fun _ ->
    xUnit2 (fun defaults ->
        {
            defaults with
                ShadowCopy = false;
                WorkingDir = Some (srcDir @@ "UnitTests/bin" @@ configuration)
                //HtmlOutputPath = Some testDir;
                //XmlOutputPath = Some testDir;
        })
        [
            projectName + ".UnitTests.dll"
        ]
)
*)

// put the current version and release notes into an environment variable so that Bitrise workflow steps can utilize them
// note that failures are expected on non-Bitrise machines
try
    ignore(Shell.Exec("envman", "add --key VERSION --value " + version))
    ignore(Shell.Exec("envman", "add --key SEMANTIC_VERSION --value " + semanticVersion))

    let tempReleaseNotesPath = Path.GetTempFileName()
    File.WriteAllText(tempReleaseNotesPath, versionInfo.Notes)

    trace("Wrote release notes to " + tempReleaseNotesPath)

    ignore(Shell.Exec("envman", "add --key RELEASE_NOTES --valuefile " + tempReleaseNotesPath))
with
| ex -> ()

// build dependencies
let deployCopy = getEnvironmentVarAsBool "DEPLOY_COPY"
let bitriseBuildNumber = System.Convert.ToInt32(environVarOrDefault "BITRISE_BUILD_NUMBER" "-1")
let executingOnBitrise = bitriseBuildNumber <> -1

"clean-misc"
    ==> "clean"

"restore-packages"
    ==> "pre-build"
    ==> "build"

"root"
    =?> ("clean", not executingOnBitrise)
    ==> "build"
    =?> ("deploy-build-copy", deployCopy)
    ==> "all"

RunTargetOrDefault "all"