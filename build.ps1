param(
    [string]$version
	)
dotnet restore llc_newVer_Updater.sln
nuget restore llc_newVer_Updater.sln
dotnet build llc_newVer_Updater.sln
set-location bin\Debug
$Path = "Release"
if (Test-Path $Path)
	{
	Remove-Item -Path "$Path" -Recurse
	} else {
    New-Item -Path $Path -ItemType "directory" -Force
    }
..\..\7z.exe a -t7z "..\..\Release\LLC_newver_$version.7z" "llc_newVer_Updater.exe" "llc_newVer_Updater.exe.config" -mx=9 -ms
set-location "..\..\"
