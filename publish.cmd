SET nugetversion=0.2.0
dotnet pack ./src/DeadManSwitch/DeadManSwitch.csproj --configuration Release --include-source
dotnet pack ./src/DeadManSwitch.AspNetCore/DeadManSwitch.AspNetCore.csproj --configuration Release --include-source
dotnet nuget push ./src/DeadManSwitch/bin/Release/DeadManSwitch.%nugetversion%.nupkg --source nuget.org
dotnet nuget push ./src/DeadManSwitch.AspNetCore/bin/Release/DeadManSwitch.AspNetCore.%nugetversion%.nupkg --source nuget.org
dotnet nuget push ./src/DeadManSwitch/bin/Release/DeadManSwitch.%nugetversion%.nupkg --source Github
dotnet nuget push ./src/DeadManSwitch.AspNetCore/bin/Release/DeadManSwitch.AspNetCore.%nugetversion%.nupkg --source Github