@echo off
setlocal
cd /d "%~dp0"
chcp 65001 >nul
set "DOTNET_CLI_HOME=%~dp0.dotnet"
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"
set "DOTNET_GENERATE_ASPNET_CERTIFICATE=false"
dotnet run --project src\LibraryCatalog.Console -c Release --verbosity quiet
exit /b %errorlevel%
