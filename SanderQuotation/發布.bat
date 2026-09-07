@echo off
chcp 65001 >nul
setlocal EnableExtensions

set "ROOT=%~dp0"
set "BACKEND_OUT=%ROOT%backend\bin\Release\net8.0\WebPublisher"
set "FRONTEND_OUT=%ROOT%frontend\bin\Release\net8.0\WebPublisher"
set "ZIP_BACKEND_DIR=%ROOT%WebPublisher\backend"
set "ZIP_FRONTEND_DIR=%ROOT%WebPublisher\frontend"
set "ZIP_BACKEND_FILE=%ZIP_BACKEND_DIR%\WebPublisher.zip"
set "ZIP_FRONTEND_FILE=%ZIP_FRONTEND_DIR%\WebPublisher.zip"
set "ZIP_ROOT_DIR=%ROOT%WebPublisher"
set "ZIP_ROOT_FILE=%ROOT%WebPublisher.zip"

echo ========================================
echo  Linux x64 Publish (Release)
echo  Root: %ROOT%
echo ========================================
echo.

echo [1/5] Publishing backend...
pushd "%ROOT%backend"
dotnet publish ./backend.csproj -c Release -r linux-x64 --self-contained false -o bin\Release\net8.0\WebPublisher
set "BACKEND_ERR=%ERRORLEVEL%"
popd
if %BACKEND_ERR% neq 0 (
    echo.
    echo backend publish failed.
    pause
    exit /b 1
)
echo backend publish succeeded.
echo.

echo [2/5] Publishing frontend...
pushd "%ROOT%frontend"
dotnet publish ./frontend.csproj -c Release -r linux-x64 --self-contained false -o bin\Release\net8.0\WebPublisher
set "FRONTEND_ERR=%ERRORLEVEL%"
popd
if %FRONTEND_ERR% neq 0 (
    echo.
    echo frontend publish failed.
    pause
    exit /b 1
)
echo frontend publish succeeded.
echo.

echo [3/5] Post-process publish output...
echo.

if exist "%BACKEND_OUT%\appsettings.json" (
    del /f /q "%BACKEND_OUT%\appsettings.json"
    echo Deleted: backend\appsettings.json
) else (
    echo Skip: backend\appsettings.json not found
)

if exist "%BACKEND_OUT%\appsettings.Development.json" (
    del /f /q "%BACKEND_OUT%\appsettings.Development.json"
    echo Deleted: backend\appsettings.Development.json
) else (
    echo Skip: backend\appsettings.Development.json not found
)

if exist "%FRONTEND_OUT%\appsettings.json" (
    del /f /q "%FRONTEND_OUT%\appsettings.json"
    echo Deleted: frontend\appsettings.json
) else (
    echo Skip: frontend\appsettings.json not found
)

if exist "%FRONTEND_OUT%\appsettings.Development.json" (
    del /f /q "%FRONTEND_OUT%\appsettings.Development.json"
    echo Deleted: frontend\appsettings.Development.json
) else (
    echo Skip: frontend\appsettings.Development.json not found
)

echo.
echo [4/5] Creating zip archives (Linux-compatible paths)...
echo.

if not exist "%ZIP_BACKEND_DIR%" mkdir "%ZIP_BACKEND_DIR%"
if not exist "%ZIP_FRONTEND_DIR%" mkdir "%ZIP_FRONTEND_DIR%"

set "PUBLISH_BACKEND_OUT=%BACKEND_OUT%"
set "PUBLISH_FRONTEND_OUT=%FRONTEND_OUT%"
set "PUBLISH_ZIP_BACKEND=%ZIP_BACKEND_FILE%"
set "PUBLISH_ZIP_FRONTEND=%ZIP_FRONTEND_FILE%"
set "PUBLISH_ZIP_ROOT_DIR=%ZIP_ROOT_DIR%"
set "PUBLISH_ZIP_ROOT_FILE=%ZIP_ROOT_FILE%"

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference = 'Stop';" ^
  "Add-Type -AssemblyName System.IO.Compression;" ^
  "Add-Type -AssemblyName System.IO.Compression.FileSystem;" ^
  "function New-LinuxZip([string]$SourceDir, [string]$ZipPath, [string]$EntryPrefix) {" ^
  "  $src = (Resolve-Path -LiteralPath $SourceDir).Path.TrimEnd('\');" ^
  "  if (Test-Path -LiteralPath $ZipPath) { Remove-Item -LiteralPath $ZipPath -Force };" ^
  "  $zip = [System.IO.Compression.ZipFile]::Open($ZipPath, [System.IO.Compression.ZipArchiveMode]::Create);" ^
  "  try {" ^
  "    Get-ChildItem -LiteralPath $src -Recurse -File | ForEach-Object {" ^
  "      $relative = $_.FullName.Substring($src.Length).TrimStart('\');" ^
  "      $entryName = if ($EntryPrefix) { ($EntryPrefix + '/' + $relative).Replace('\', '/') } else { $relative.Replace('\', '/') };" ^
  "      [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $entryName, [System.IO.Compression.CompressionLevel]::Optimal);" ^
  "    };" ^
  "  } finally { $zip.Dispose() };" ^
  "};" ^
  "$backendZip = $env:PUBLISH_ZIP_BACKEND;" ^
  "$frontendZip = $env:PUBLISH_ZIP_FRONTEND;" ^
  "$backendOut = $env:PUBLISH_BACKEND_OUT;" ^
  "$frontendOut = $env:PUBLISH_FRONTEND_OUT;" ^
  "$rootDir = $env:PUBLISH_ZIP_ROOT_DIR;" ^
  "$rootZip = $env:PUBLISH_ZIP_ROOT_FILE;" ^
  "if (-not (Test-Path -LiteralPath $backendOut)) { throw 'Backend WebPublisher folder not found.' };" ^
  "if (-not (Test-Path -LiteralPath $frontendOut)) { throw 'Frontend WebPublisher folder not found.' };" ^
  "if (-not (Test-Path -LiteralPath $rootDir)) { throw 'WebPublisher folder not found.' };" ^
  "New-LinuxZip $backendOut $backendZip 'WebPublisher';" ^
  "Write-Host ('Created: ' + $backendZip);" ^
  "New-LinuxZip $frontendOut $frontendZip 'WebPublisher';" ^
  "Write-Host ('Created: ' + $frontendZip);" ^
  "New-LinuxZip $rootDir $rootZip (Split-Path -Leaf $rootDir);" ^
  "Write-Host ('Created: ' + $rootZip);"

if errorlevel 1 (
    echo.
    echo zip creation failed.
    pause
    exit /b 1
)

echo.
echo ========================================
echo  All publish completed.
echo  Output:
echo    %BACKEND_OUT%
echo    %FRONTEND_OUT%
echo  Zip:
echo    %ZIP_BACKEND_FILE%
echo    %ZIP_FRONTEND_FILE%
echo    %ZIP_ROOT_FILE%
echo ========================================

pause
endlocal
