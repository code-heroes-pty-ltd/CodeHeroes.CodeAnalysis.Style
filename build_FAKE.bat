@echo off
cls
".nuget\NuGet.exe" "Install" "FAKE" "-Version" "5.0.0-beta005" "-Prerelease"  "-OutputDirectory" "Src\packages" "-ExcludeVersion"
"Src\packages\FAKE\tools\Fake.exe" build.fsx %*