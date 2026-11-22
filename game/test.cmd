@echo off
setlocal
cd /d "%~dp0"
dotnet run --project tests\Arena.Core.Tests -c Release
exit /b %errorlevel%
