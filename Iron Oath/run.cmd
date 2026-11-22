@echo off
setlocal
cd /d "%~dp0"
dotnet run --project src\Arena.Wpf -c Release
exit /b %errorlevel%
