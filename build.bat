@echo off
cls
".nuget\NuGet.exe" "Install" "FAKE" "-Version" "4.61.3" "-OutputDirectory" "Src\packages" "-ExcludeVersion"
"Src\packages\FAKE\tools\Fake.exe" build.fsx %*