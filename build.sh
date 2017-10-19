#!/bin/bash
mono .nuget/nuget.exe install FAKE -Version 4.61.3 -OutputDirectory "Src/packages" -ExcludeVersion
mono Src/packages/FAKE/tools/FAKE.exe build.fsx $@