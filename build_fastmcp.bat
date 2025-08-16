@echo off
echo ================================
echo  TemplateForge FastMCP Builder
echo ================================

cd /d E:\TemplateForge\FastMcp

echo Current directory: %CD%
echo.

if not exist "fastmcp_server.py" (
    echo ERROR: fastmcp_server.py not found!
    pause
    exit /b 1
)

if not exist "requirements.txt" (
    echo ERROR: requirements.txt not found!
    pause
    exit /b 1
)

echo Installing Python dependencies...
python -m pip install --upgrade pip
python -m pip install -r requirements.txt
python -m pip install pyinstaller

echo.
echo Building with PyInstaller...
python -m PyInstaller --name fastmcp --onefile --noconfirm --clean fastmcp_server.py

echo.
echo Copying to WPF project...
if not exist "..\TemplateForge\Runtime\fastmcp" mkdir "..\TemplateForge\Runtime\fastmcp"
copy "dist\fastmcp.exe" "..\TemplateForge\Runtime\fastmcp\"

if exist "..\TemplateForge\Runtime\fastmcp\fastmcp.exe" (
    echo.
    echo ✅ Build completed successfully!
    echo FastMCP server is ready at: ..\TemplateForge\Runtime\fastmcp\fastmcp.exe
) else (
    echo.
    echo ❌ Build failed!
)

echo.
pause
