@echo off
echo ========================================
echo TemplateForge Build Script
echo ========================================
echo.

REM MSBuild 경로 설정 (Visual Studio 2019/2022)
set MSBUILD_PATH=""

REM Visual Studio 2022
if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    echo Found Visual Studio 2022 Community
)

if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
    echo Found Visual Studio 2022 Professional
)

if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    echo Found Visual Studio 2022 Enterprise
)

REM Visual Studio 2019
if %MSBUILD_PATH%=="" (
    if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" (
        set MSBUILD_PATH="%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
        echo Found Visual Studio 2019 Community
    )
)

if %MSBUILD_PATH%=="" (
    if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe" (
        set MSBUILD_PATH="%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
        echo Found Visual Studio 2019 Professional
    )
)

if %MSBUILD_PATH%=="" (
    echo Error: MSBuild not found. Please install Visual Studio 2019 or 2022
    pause
    exit /b 1
)

echo.
echo Building TemplateForge...
echo.

REM NuGet 패키지 복원
echo Restoring NuGet packages...
nuget restore TemplateForge.sln 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo Warning: NuGet restore failed. Trying to continue...
)

REM Release 빌드
echo Building Release configuration...
%MSBUILD_PATH% TemplateForge.sln /p:Configuration=Release /p:Platform="Any CPU" /m

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo Build completed successfully!
echo Output: TemplateForge\bin\Release\
echo ========================================
echo.
echo Run TemplateForge.exe to start the application
echo.
pause