@echo off
setlocal enabledelayedexpansion

:: CONFIG
set "REPO_URL=https://github.com/manglesh1/games"
set "BRANCH=deploy"
set "INSTALL_DIR=C:\Users\AeroSports\Desktop\test"
set "TEMP_DIR=%TEMP%\repo_clone_%RANDOM%"
set "BACKUP_DIR=%TEMP%\config_backup_%RANDOM%"
set "GIT_PATH=git"

:: Protected config files (relative to install dir)
set "PROTECTED_FILES=scorecard.exe.config GameRoomScoreboard.dll.config"

echo 🔐 Backing up protected config files...
mkdir "%BACKUP_DIR%"
for %%F in (%PROTECTED_FILES%) do (
    if exist "%INSTALL_DIR%\%%F" (
        echo    Backing up %%F
        xcopy /Y /Q "%INSTALL_DIR%\%%F" "%BACKUP_DIR%\%%F" >nul
    )
)

echo 🧹 Deleting old install directory...
rmdir /S /Q "%INSTALL_DIR%"

echo 📥 Cloning repo branch '%BRANCH%'...
"%GIT_PATH%" clone --branch %BRANCH% "%REPO_URL%" "%INSTALL_DIR%"
if errorlevel 1 (
    echo ❌ Git clone failed!
    exit /b 1
)

echo ♻️ Restoring protected config files...
for %%F in (%PROTECTED_FILES%) do (
    if exist "%BACKUP_DIR%\%%F" (
        echo    Restoring %%F
        xcopy /Y /Q "%BACKUP_DIR%\%%F" "%INSTALL_DIR%\%%F" >nul
    )
)

echo ✅ Update complete.
exit /b 0
