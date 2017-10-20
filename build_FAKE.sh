#!/bin/bash
mono .nuget/nuget.exe install FAKE -Version 5.0.0-beta005 -Prerelease -OutputDirectory "Src/packages" -ExcludeVersion
mono Src/packages/FAKE/tools/FAKE.exe build.fsx $@