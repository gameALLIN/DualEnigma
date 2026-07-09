@echo off
echo ====================================
echo  DualEnigma Excel Export Tool v1.0
echo ====================================
echo.

cd /d "%~dp0\.."

if "%1"=="" (
    echo Exporting all tables...
    python src/main.py export
) else (
    echo Exporting table: %1
    python src/main.py export --table %1
)

echo.
echo Done.
pause
