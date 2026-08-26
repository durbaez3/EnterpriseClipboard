@echo off
REM ============================================================
REM  Enterprise Clipboard Manager - Desinstalador de Inicio Automatico
REM  Elimina el acceso directo de la carpeta Startup de Windows.
REM ============================================================

setlocal

set "STARTUP_DIR=%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup"
set "SHORTCUT_PATH=%STARTUP_DIR%\Enterprise Clipboard Manager.lnk"

if exist "%SHORTCUT_PATH%" (
    del /f /q "%SHORTCUT_PATH%"
    echo [OK] Acceso directo eliminado. La aplicacion ya no iniciara automaticamente.
) else (
    echo [INFO] No se encontro el acceso directo en la carpeta de Inicio.
    echo   %SHORTCUT_PATH%
)

echo.
pause
endlocal
