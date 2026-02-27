@echo off
REM ============================================================================
REM Build script for Minkio.dll
REM Automatically detects Visual Studio 2022 and compiles for x64 Release
REM ============================================================================

setlocal enabledelayedexpansion

REM Colors (Windows 10+)
color 0A
echo.
echo ============================================================================
echo                    MINKIO DLL BUILD SCRIPT
echo ============================================================================
echo.

REM Get current directory
set "SCRIPT_DIR=%cd%"
echo Build Directory: %SCRIPT_DIR%
echo.

REM ============================================================================
REM STEP 1: Find Visual Studio 2022 using vswhere
REM ============================================================================
echo [STEP 1] Searching for Visual Studio 2022...

REM vswhere path (usually in Program Files)
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

if not exist "!VSWHERE!" (
    echo Error: vswhere.exe not found. Visual Studio 2022 may not be installed.
    echo.
    echo Trying alternative search methods...
    
    REM Fallback: Check common installation paths
    if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe" (
        set "DEVENV=C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe"
        echo Found: Community Edition
        goto :VS_FOUND
    )
    
    if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\devenv.exe" (
        set "DEVENV=C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\devenv.exe"
        echo Found: Professional Edition
        goto :VS_FOUND
    )
    
    if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\devenv.exe" (
        set "DEVENV=C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\devenv.exe"
        echo Found: Enterprise Edition
        goto :VS_FOUND
    )
    
    echo.
    echo ERROR: Visual Studio 2022 not found!
    echo Please install Visual Studio 2022 with "Desktop Development with C++"
    echo Download from: https://visualstudio.microsoft.com/vs/community/
    echo.
    pause
    exit /b 1
)

REM Use vswhere to find VS2022
echo Using vswhere to locate VS2022...
for /f "usebackq tokens=*" %%i in (`"!VSWHERE!" -latest -products * -version "[17.0,18.0)" -property installationPath`) do (
    set "VS_PATH=%%i"
)

if not defined VS_PATH (
    echo Error: Visual Studio 2022 not found via vswhere
    pause
    exit /b 1
)

set "DEVENV=!VS_PATH!\Common7\IDE\devenv.exe"

:VS_FOUND
if not exist "!DEVENV!" (
    echo Error: devenv.exe not found at: !DEVENV!
    pause
    exit /b 1
)

echo [OK] Visual Studio found at:
echo      !DEVENV!
echo.

REM ============================================================================
REM STEP 2: Find MSBuild
REM ============================================================================
echo [STEP 2] Searching for MSBuild...

set "MSBUILD=!VS_PATH!\MSBuild\Current\Bin\MSBuild.exe"

if not exist "!MSBUILD!" (
    echo Warning: MSBuild not found at standard location
    echo Attempting to use devenv.exe instead
    set "USE_DEVENV=1"
) else (
    echo [OK] MSBuild found at:
    echo      !MSBUILD!
    set "USE_DEVENV=0"
)
echo.

REM ============================================================================
REM STEP 3: Verify project file exists
REM ============================================================================
echo [STEP 3] Verifying Minkio.vcxproj exists...

if not exist "%SCRIPT_DIR%\Minkio.vcxproj" (
    echo Error: Minkio.vcxproj not found in %SCRIPT_DIR%
    echo Expected: %SCRIPT_DIR%\Minkio.vcxproj
    pause
    exit /b 1
)

echo [OK] Project file found
echo.

REM ============================================================================
REM STEP 4: Clean previous build (optional)
REM ============================================================================
echo [STEP 4] Cleaning previous build artifacts...

if exist "%SCRIPT_DIR%\x64\Release" (
    echo Removing old build: x64\Release
    rmdir /s /q "%SCRIPT_DIR%\x64\Release" 2>nul
)

if exist "%SCRIPT_DIR%\x64\Debug" (
    echo Removing old build: x64\Debug
    rmdir /s /q "%SCRIPT_DIR%\x64\Debug" 2>nul
)

echo [OK] Clean complete
echo.

REM ============================================================================
REM STEP 5: Compile the DLL
REM ============================================================================
echo [STEP 5] Compiling Minkio.dll for x64 Release...
echo.

if !USE_DEVENV! EQU 1 (
    echo Using: devenv.exe
    "!DEVENV!" "%SCRIPT_DIR%\Minkio.vcxproj" /Build "Release|x64" /Out "%SCRIPT_DIR%\build.log"
) else (
    echo Using: MSBuild.exe
    "!MSBUILD!" "%SCRIPT_DIR%\Minkio.vcxproj" /p:Configuration=Release /p:Platform=x64 /p:PlatformToolset=v143 /verbosity:normal /m
)

if %ERRORLEVEL% EQU 0 (
    goto :BUILD_SUCCESS
) else (
    goto :BUILD_FAILED
)

:BUILD_SUCCESS
echo.
echo ============================================================================
echo                        BUILD SUCCESSFUL!
echo ============================================================================
echo.

REM Verify output DLL exists
if not exist "%SCRIPT_DIR%\x64\Release\Minkio.dll" (
    echo Error: DLL file not found at expected location!
    echo Expected: %SCRIPT_DIR%\x64\Release\Minkio.dll
    echo.
    if exist "%SCRIPT_DIR%\build.log" (
        echo Build log contents:
        type "%SCRIPT_DIR%\build.log"
    )
    pause
    exit /b 1
)

REM Get DLL file size
for %%F in ("%SCRIPT_DIR%\x64\Release\Minkio.dll") do (
    set "FILESIZE=%%~zF"
)

echo Output DLL:    %SCRIPT_DIR%\x64\Release\Minkio.dll
echo DLL Size:      !FILESIZE! bytes
echo Architecture:  x64 (64-bit)
echo Configuration: Release
echo.
echo ============================================================================
echo Ready for injection into RobloxPlayerBeta.exe!
echo ============================================================================
echo.
echo Next steps:
echo   1. Place Minkio.dll in the executor's output directory
echo   2. Run the MinkioExecutor application
echo   3. Click the "Attach" button to inject
echo.
pause
exit /b 0

:BUILD_FAILED
echo.
echo ============================================================================
echo                         BUILD FAILED!
echo ============================================================================
echo.
echo Please check the error messages above and:
echo   1. Ensure Visual Studio 2022 is properly installed
echo   2. Verify you have C++ development tools installed
echo   3. Check that no files are in use by other programs
echo.

if exist "%SCRIPT_DIR%\build.log" (
    echo Build log:
    type "%SCRIPT_DIR%\build.log"
    echo.
)

pause
exit /b 1

endlocal
