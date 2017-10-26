#addin "Cake.FileHelpers"
#addin nuget:?package=Cake.Git

var projectName = "CodeHeroes.CodeAnalysis.Style";

// Versioning.
var latestReleaseNote = ParseReleaseNotes("./release-notes.md");
var semanticVersion = latestReleaseNote.Version;
var buildNumber = int.Parse(EnvironmentVariable("BUILD_NUMBER") ?? "0");
var version = semanticVersion + "." + buildNumber;
var nugetVersion = semanticVersion.ToString();
var gitBranch = GitBranchCurrent(".");
var isMasterBuild = gitBranch.FriendlyName == "master";

if (!isMasterBuild)
{
    nugetVersion += ("-alpha" + buildNumber.ToString("000"));
}

// Parameters.
var configuration = EnvironmentVariable("CONFIGURATION") ?? "Release";
var deployLocally = bool.Parse(EnvironmentVariable("DEPLOY_LOCALLY") ?? "false");
var localDeployDir = Directory(EnvironmentVariable("LOCAL_DEPLOY_DIR") ?? ".");
var deployRemotely = bool.Parse(EnvironmentVariable("DEPLOY_REMOTELY") ?? "false");
var remoteDeploySource = EnvironmentVariable("REMOTE_DEPLOY_SOURCE") ?? null;
var remoteDeployKey = EnvironmentVariable("REMOTE_DEPLOY_KEY") ?? null;
var tag = bool.Parse(EnvironmentVariable("TAG") ?? "false");
var bitriseBuildNumber = int.Parse(EnvironmentVariable("BITRISE_BUILD_NUMBER") ?? "-1");
var executingOnBitrise = bitriseBuildNumber != -1;
var msBuildVerbosity = (Verbosity)Enum.Parse(typeof(Verbosity), EnvironmentVariable("MSBUILD_VERBOSITY") ?? "Verbose");

// Paths.
var genDir = Directory("Gen");
var srcDir = Directory("Src");
var solution = srcDir + File(projectName + ".sln");

// Debug output.
Information("Starting build {0} against git branch '{1}'.", version, gitBranch.CanonicalName);

if (deployRemotely)
{
    Information("NuGet version {0}", nugetVersion);
}

Task("Clean")
    .WithCriteria(!executingOnBitrise)
    .Does(
        () =>
        {
            CleanDirectories(genDir);

            var logFile = File("clean.binlog");

            MSBuild(
                solution,
                new MSBuildSettings
                {
                    Configuration = configuration,
                    MaxCpuCount = 0,
                    NoConsoleLogger = true,
                    BinaryLogger = new MSBuildBinaryLogSettings
                    {
                        Enabled = true,
                        FileName = logFile
                    },
                    Verbosity = msBuildVerbosity
                }
                .WithTarget("Clean"));
            
            if (deployLocally)
            {
                CopyFile(logFile, localDeployDir + logFile);
            }
        });

Task("Build")
    .IsDependentOn("Clean")
    .Does(
        () =>
        {
            CreateAssemblyInfo(
                srcDir + File("AssemblyInfoCommon.cs"),
                new AssemblyInfoSettings
                {
                    Company = "Code Heroes",
                    Product = projectName,
                    Copyright = "© Copyright. Code Heroes.",
                    Version = version,
                    FileVersion = version,
                    InformationalVersion = semanticVersion.ToString(),
                    Configuration = configuration
                }
                .AddCustomAttribute("NeutralResourcesLanguage", "System.Resources", "en-AU"));

            DotNetCoreRestore(
                new DotNetCoreRestoreSettings
                {
                    WorkingDirectory = srcDir
                });

            var logFile = File("build.binlog");

            MSBuild(
                solution,
                new MSBuildSettings
                {
                    Configuration = configuration,
                    MaxCpuCount = 0,
                    NoConsoleLogger = true,
                    BinaryLogger = new MSBuildBinaryLogSettings
                    {
                        Enabled = true,
                        FileName = logFile
                    },
                    Verbosity = msBuildVerbosity
                }
                .WithProperty("Version", nugetVersion)
                .WithTarget("Build"));
            
            if (deployLocally)
            {
                CopyFile(logFile, localDeployDir + logFile);

                var nupkgFile = File("Analyzers." + nugetVersion + ".nupkg");
                var nupkgFullFile = srcDir + Directory("Analyzers") + Directory("bin") + Directory(configuration) + nupkgFile;

                CopyFile(nupkgFullFile, localDeployDir + nupkgFile);
            }
        });

Task("Test")
    .IsDependentOn("Build")
    .Does(
        () =>
        {
            var testAssemblies = GetFiles(srcDir + Directory("Analyzers.UnitTests/bin/") + Directory(configuration) + Directory("net46") + File("Analyzers.UnitTests.dll"));

            XUnit2(
                testAssemblies,
                new XUnit2Settings
                {
                    HtmlReport = true,
                    XmlReport = true,
                    OutputDirectory = genDir
                }
            );

            if (deployLocally)
            {
                CopyFiles(GetFiles(genDir.ToString() + "/Analyzers.UnitTests.dll.*"), localDeployDir);
            }
        });

Task("Tag")
    .IsDependentOn("Test")
    .WithCriteria(tag)
    .Does(
        () =>
        {
            var tagName = "v" + version;
            GitTag(".", tagName);

            // GitPushRef does not support ssh
            StartProcess(
                "git",
                new ProcessSettings
                {
                    Arguments = "push origin " + tagName
                });
        });

Task("Deploy")
    .IsDependentOn("Tag")
    .WithCriteria(deployRemotely)
    .Does(
        () =>
        {
            if (string.IsNullOrEmpty(remoteDeploySource))
            {
                throw new Exception("No remote deploy source set.");
            }

            if (string.IsNullOrEmpty(remoteDeployKey))
            {
                throw new Exception("No remote deploy key set.");
            }

            var nupkgFile = File("Analyzers." + nugetVersion + ".nupkg");
            var nupkgFullFile = srcDir + Directory("Analyzers") + Directory("bin") + Directory(configuration) + nupkgFile;

            NuGetPush(
                nupkgFullFile,
                new NuGetPushSettings
                {
                    Source = remoteDeploySource,
                    ApiKey = remoteDeployKey
                });
        });

Task("Default")
    .IsDependentOn("Deploy");

CreateDirectory(genDir);

if (executingOnBitrise)
{
    // Put the current version and release notes into an environment variable so that Bitrise workflow steps can utilize them.
    StartProcess(
        "envman",
        new ProcessSettings
        {
            Arguments = "add --key VERSION --value " + version
        });
    StartProcess(
        "envman",
        new ProcessSettings
        {
            Arguments = "add --key SEMANTIC_VERSION --value " + semanticVersion
        });

    var notesFile = genDir + File("release-notes-only.txt");
    var notes = latestReleaseNote
        .Notes
        .Aggregate(
            new StringBuilder(),
            (acc, next) => acc.AppendLine(next),
            sb => sb.ToString());
    FileWriteText(notesFile, notes);
    StartProcess(
        "envman",
        new ProcessSettings
        {
            Arguments = "add --key RELEASE_NOTES --valuefile " + notesFile
        });
}

RunTarget(Argument("target", "Default"));