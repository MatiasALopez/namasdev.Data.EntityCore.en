@echo off
dotnet build --configuration Release
dotnet pack --configuration Release --no-build
for %%f in (bin\Release\*.nupkg) do (
    powershell -NoProfile -Command "& { $pkg = '%%~nf'; if ($pkg -match '^(.+?)\.(\d+\.\d+\.\d+.*)$') { $id = $matches[1]; $ver = $matches[2]; $path = '\\MATUASUS\nuget\' + $id.ToLower() + '\' + $ver; if (Test-Path $path) { Remove-Item -Recurse -Force $path; Write-Host ('Removed existing: ' + $path) } } }"
    nuget add "%%f" -source \\MATUASUS\nuget
)
