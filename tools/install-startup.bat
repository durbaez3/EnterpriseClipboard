@echo off
REM ============================================================
REM  Enterprise Clipboard Manager - Instalador de Inicio Automatico
REM  Crea un acceso directo en la carpeta Startup de Windows
REM  para que la aplicacion inicie automaticamente al encender el PC.
REM ============================================================

setlocal

REM Ruta al ejecutable (en la misma carpeta que este .bat)
set "EXE_PATH=%~dp0EnterpriseClipboard.App.exe"

REM Carpeta Startup del usuario actual
set "STARTUP_DIR=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup"

REM Nombre del acceso directo
set "SHORTCUT_NAME=Enterprise Clipboard Manager.lnk"
set "SHORTCUT_PATH=%STARTUP_DIR%\%SHORTCUT_NAME%"

REM Verificar que el ejecutable existe
if not exist "%EXE_PATH%" (
    echo ERROR: No se encontro el ejecutable en:
    echo   %EXE_PATH%
    echo.
    echo Asegurate de ejecutar este script desde la misma carpeta que el .exe
    pause
    exit /b 1
)

REM Crear el acceso directo usando VBScript
set "VBS_TEMP=%TEMP%\create_shortcut.vbs"
(
echo Set oWS = WScript.CreateObject^("WScript.Shell"^)
echo sLinkFile = "%SHORTCUT_PATH%"
echo Set oLink = oWS.CreateShortcut^(sLinkFile^)
echo oLink.TargetPath = "%EXE_PATH%"
echo oLink.WorkingDirectory = "%~dp0"
echo oLink.Description = "Enterprise Clipboard Manager"
echo oLink.WindowStyle = 7
echo oLink.Save
) > "%VBS_TEMP%"

cscript //nologo "%VBS_TEMP%"
del "%VBS_TEMP%"

if exist "%SHORTCUT_PATH%" (
    echo.
    echo [OK] Acceso directo creado exitosamente en:
    echo   %SHORTCUT_PATH%
    echo.
    echo Enterprise Clipboard Manager se iniciara automaticamente
    echo la proxima vez que inicies sesion en Windows.
) else (
    echo ERROR: No se pudo crear el acceso directo.
)

echo.
pause
endlocal
