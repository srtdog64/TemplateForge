param(
    [string]$PythonExe = "python",
    [string]$Entry = "fastmcp_server.py",
    [string]$OutDir = "../TemplateForge/Runtime/fastmcp"
)

$ErrorActionPreference = "Stop"

Write-Host "Building TemplateForge FastMCP Server..."

# Install dependencies
& $PythonExe -m pip install --upgrade pip
& $PythonExe -m pip install -r requirements.txt
& $PythonExe -m pip install pyinstaller

# Build with PyInstaller
& $PythonExe -m PyInstaller `
    --name fastmcp `
    --onefile `
    --noconfirm `
    --clean `
    --add-data "requirements.txt;." `
    $Entry

# Copy to WPF project
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
Copy-Item -Force ".\dist\fastmcp.exe" $OutDir

Write-Host "fastmcp.exe copied to $OutDir"
Write-Host "Build completed successfully!"
