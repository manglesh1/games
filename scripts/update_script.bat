@echo off
setlocal enabledelayedexpansion

:: Set repository information
set "REPO_URL=https://github.com/manglesh1/games"
set "BRANCH=deploy"

:: Get Desktop path directly from system
echo Step 1: Getting Desktop path...
for /f "tokens=2*" %%a in ('reg query "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders" /v "Desktop"') do set "DESKTOP_ROOT=%%b"
echo DESKTOP_ROOT=%DESKTOP_ROOT%

:: Set paths
echo Step 2: Setting up paths...
set "INSTALL_DIR=%DESKTOP_ROOT%\Pixelgames"
set "TEMP_DIR=%TEMP%\repo_clone_%RANDOM%"
set "BACKUP_DIR=%TEMP%\config_backup_%RANDOM%"
echo INSTALL_DIR=%INSTALL_DIR%
echo BACKUP_DIR=%BACKUP_DIR%

:: Protected config files
set "PROTECTED_FILES=GameEngine\scorecard.exe.config GameSelection\GameRoomScoreboard.dll.config"

:: Step 1: Backup existing config files if they exist
echo Step 3: Checking for existing config files to backup...
mkdir "%BACKUP_DIR%" 2>nul

for %%F in (%PROTECTED_FILES%) do (
    echo Checking file: %%F
    set "SRC_FILE=%INSTALL_DIR%\%%F"
    set "DST_FILE=%BACKUP_DIR%\%%F"
    
    if exist "!SRC_FILE!" (
        :: Create the destination folder structure
        for %%D in ("!DST_FILE!") do (
            set "DST_FOLDER=%%~dpD"
            mkdir "!DST_FOLDER!" 2>nul
        )
        
        echo Backing up: !SRC_FILE!
        copy /Y "!SRC_FILE!" "!DST_FILE!" >nul
        if errorlevel 1 (
            echo Failed to backup file
            goto :error
        )
        echo Backup successful: %%F
    ) else (
        echo No existing config found for: %%F
    )
)

:: Step 2: Clone new version to temp directory first
echo Step 4: Cloning repository branch '%BRANCH%' to temp location...
git clone --branch %BRANCH% "%REPO_URL%" "%TEMP_DIR%\repo"
if errorlevel 1 (
    echo Failed to clone repository
    goto :error
)

:: Step 3: Delete old installation
echo Step 5: Removing old installation...
if exist "%INSTALL_DIR%" (
    rmdir /S /Q "%INSTALL_DIR%"
    if errorlevel 1 (
        echo Failed to remove old installation
        goto :error
    )
)

:: Step 4: Move new version to install directory
echo Step 6: Installing new version...
move "%TEMP_DIR%\repo" "%INSTALL_DIR%"
if errorlevel 1 (
    echo Failed to move new version to install directory
    goto :error
)

:: Step 5: Restore backed up configs if they exist
echo Step 7: Restoring config files...

for %%F in (%PROTECTED_FILES%) do (
    set "SRC_FILE=%BACKUP_DIR%\%%F"
    set "DST_FILE=%INSTALL_DIR%\%%F"
    
    if exist "!SRC_FILE!" (
        :: Create the destination folder structure
        for %%D in ("!DST_FILE!") do (
            set "DST_FOLDER=%%~dpD"
            mkdir "!DST_FOLDER!" 2>nul
        )
        
        echo Restoring: %%F
        copy /Y "!SRC_FILE!" "!DST_FILE!" >nul
        if errorlevel 1 (
            echo Failed to restore config file
            goto :error
        )
        echo Restored: %%F
    ) else (
        echo Using repository version of: %%F
    )
)

:: Cleanup
echo Step 8: Cleaning up temporary files...
if exist "%BACKUP_DIR%" rmdir /S /Q "%BACKUP_DIR%"
if exist "%TEMP_DIR%" rmdir /S /Q "%TEMP_DIR%"

goto :success

:error
echo.
echo Update failed!
echo.
pause
exit /b 1

:success
echo.
echo Update completed successfully!
echo    New version installed and config files handled.
echo.
echo Installed config files:
dir "%INSTALL_DIR%\GameEngine\scorecard.exe.config" 2>nul
dir "%INSTALL_DIR%\GameSelection\GameRoomScoreboard.dll.config" 2>nul
exit /b 0