#r "FakeLib.dll"
#load "build-helpers.fsx"

open System
open System.IO
open BuildHelpers
open Fake.Core
open Fake.Core.Globbing
open Fake.Core.Globbing.Operators
open Fake.Core.Process
open Fake.Core.TargetOperators
open Fake.Core.Trace
open Fake.DotNet
open Fake.DotNet.NuGet.Restore
open Fake.DotNet.Testing.XUnit2
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.MSBuildHelper

// project properties
let projectName = "CodeHeroes.CodeAnalysis.Style"

// version properties
let versionInfo = getReleaseInfo "release-notes.md"

trace ("NOTES: " + versionInfo.Notes)

let semanticVersion = versionInfo.Version
let buildNumber = Environment.environVarOrDefault "BUILD_NUMBER" "0"
let buildUrl = Environment.environVarOrDefault "BUILD_URL" ""
let version = (semanticVersion + "." + buildNumber)

// can be overridden with -ev name value
let configuration = Environment.environVarOrDefault "CONFIGURATION" "Release"
let deployCopy = Environment.environVarAsBool "DEPLOY_COPY"
let deployDir = Environment.environVarOrDefault "DEPLOY_DIR" ""

// file and directory paths
let genDir = "Gen/"
let srcDir = "Src/"
let nugetDir = ".nuget/"
let testDir = genDir @@ "Test"
let tempDir = genDir @@ "Temp"
let solution = srcDir @@ projectName + ".sln"

let verbosity = Some(Detailed)

trace ("Starting build " + version)

Target.Create "clean-misc" (fun _ ->
    Shell.CleanDirs[genDir; testDir; tempDir]
)

Target.Create "clean" (fun _ ->
    let logFile = "clean.binlog"

    build (fun defaults ->
        {
            defaults with
                Verbosity = verbosity
                Targets = ["Clean"]
                Properties = ["Configuration", configuration]
                BinaryLoggers = Some
                    [
                        logFile
                    ]
                NoConsoleLogger = true
                NoLogo = true
        })
        (solution)
    
    if deployCopy then Shell.CopyFile (deployDir @@ logFile) logFile
)

// would prefer to use the built-in RestorePackages function, but it restores packages in the root dir (not in Src), which causes build problems
Target.Create "restore-packages" (fun _ ->
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

Target.Create "pre-build" (fun _ ->
    AssemblyInfoFile.CreateCSharpWithConfig (srcDir @@ "AssemblyInfoCommon.cs")
        [
            AssemblyInfo.Version version
            AssemblyInfo.FileVersion version
            AssemblyInfo.Configuration configuration
            AssemblyInfo.Company "Code Heroes"
            AssemblyInfo.Product projectName
            AssemblyInfo.Copyright "© Copyright. Code Heroes."
            AssemblyInfo.Trademark ""
            AssemblyInfo.Culture ""
            AssemblyInfo.StringAttribute("NeutralResourcesLanguage", "en-AU", "System.Resources")
            AssemblyInfo.StringAttribute("AssemblyInformationalVersion", semanticVersion, "System.Reflection")
        ]
        (AssemblyInfoFileConfig(false))
)

Target.Create "build" (fun _ ->
    let logFile = "build.binlog"

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
                BinaryLoggers = Some
                    [
                        logFile
                    ]
                NoConsoleLogger = true
                NoLogo = true
        })
        solution
    
    if deployCopy then Shell.CopyFile (deployDir @@ logFile) logFile
)

Target.Create "deploy-build-copy" (fun _ ->
    let nupkgFile = "Analyzers." + semanticVersion + ".nupkg"
    let nupkgPath = srcDir @@ "Analyzers" @@ "bin" @@ configuration @@ nupkgFile
    let targetFile = deployDir @@ nupkgFile
    trace ("Deploying NuGet package by copying '" + nupkgPath + "' to '" + targetFile + "'")
    if not (String.IsNullOrEmpty deployDir) then Shell.CopyFile targetFile nupkgPath
)

Target.Create "test" (fun _ ->
    ignore(Shell.Exec(nugetDir @@ "NuGet.exe", "install xunit.runner.console -ExcludeVersion -OutputDirectory " + (srcDir @@ "packages")))

    xUnit2 (fun defaults ->
        {
            defaults with
                ToolPath = (srcDir @@ "packages" @@ "xunit.runner.console" @@ "tools" @@ "net452" @@ "xunit.console.exe")
                HtmlOutputPath = Some (testDir @@ "UnitTests.html");
                XmlOutputPath = Some (testDir @@ "UnitTests.xml");
        })
        [
            srcDir @@ "Analyzers.UnitTests" @@ "bin" @@ configuration @@ "net46" @@ "Analyzers.UnitTests.dll"
        ]
)

Target.Create "deploy-test-copy" (fun _ ->
    let htmlFile = testDir @@ "UnitTests.html"
    let xmlFile = testDir @@ "UnitTests.xml"
    trace ("Deploying unit test result files")

    let deployFiles =
        Shell.CopyFile (deployDir @@ "UnitTests.html") htmlFile
        Shell.CopyFile (deployDir @@ "UnitTests.xml") xmlFile

    if not (String.IsNullOrEmpty deployDir) then deployFiles
)

Target.Create "all"
    Target.DoNothing

Target.Create "root"
    Target.DoNothing

(*

Target "tag" (fun _ ->
    ignore(Shell.Exec("git", "tag v" + version))
    ignore(Shell.Exec("git", "push origin v" + version))
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
let bitriseBuildNumber = System.Convert.ToInt32(Environment.environVarOrDefault "BITRISE_BUILD_NUMBER" "-1")
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
    // grrrrr! Disabling for now due to this FAKE bug: https://github.com/fsharp/FAKE/issues/1713
    //==> "test"
    //=?> ("deploy-test-copy", deployCopy)
    ==> "all"

Target.RunOrDefault "all"