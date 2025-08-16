@echo off
cd /d "%~dp0"

if exist "TemplateForge\bin\Release\TemplateForge.exe" (
    echo Starting TemplateForge (Release)...
    start "" "TemplateForge\bin\Release\TemplateForge.exe"
) else if exist "TemplateForge\bin\Debug\TemplateForge.exe" (
    echo Starting TemplateForge (Debug)...
    start "" "TemplateForge\bin\Debug\TemplateForge.exe"
) else (
    echo TemplateForge.exe not found!
    echo Please build the project first using build.bat
    pause
)
