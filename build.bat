@echo off
setlocal
cd /d "%~dp0"

echo ========================================
echo Edge Workspace Manager - Build
echo ========================================

where dotnet >nul 2>nul
if errorlevel 1 goto :dotnet_missing

dotnet --list-sdks | findstr /b /c:"8." >nul
if errorlevel 1 goto :dotnet8_missing

dotnet restore
if errorlevel 1 goto :error

set "APP_VERSION=1.7.4"
set "RELEASE_DIR=%~dp0releases\v%APP_VERSION%"

dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o "%RELEASE_DIR%"
if errorlevel 1 goto :error

dotnet publish Updater\EdgeWorkspaceManager.Updater.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o "%~dp0obj\UpdaterPublish\v%APP_VERSION%"
if errorlevel 1 goto :error
copy /y "%~dp0obj\UpdaterPublish\v%APP_VERSION%\EdgeWorkspaceManager.Updater.exe" "%RELEASE_DIR%\EdgeWorkspaceManager.Updater.exe" >nul

echo.
echo Build completed successfully.
explorer "%RELEASE_DIR%"
pause
exit /b 0

:error
echo.
echo [ERROR] Build failed. Please review the error message above.
pause
exit /b 1

:dotnet_missing
echo.
echo [ERROR] .NET SDK was not found.
echo Install the .NET 8 SDK, then reopen this file.
echo https://dotnet.microsoft.com/download/dotnet/8.0
pause
exit /b 1

:dotnet8_missing
echo.
echo [ERROR] .NET 8 SDK was not found.
echo Install the .NET 8 SDK, then reopen this file.
echo https://dotnet.microsoft.com/download/dotnet/8.0
pause
exit /b 1
