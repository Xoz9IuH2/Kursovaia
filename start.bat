@echo off
title VOENCOM Launcher

echo.
echo ========================
echo   VOENCOM - Start
echo ========================
echo.

echo Killing old processes...
taskkill /F /IM node.exe 2>nul

echo [1/4] Starting web-API...
cd /d "%~dp0App\web-api"
start "VOENCOM API" cmd /k "dotnet run"
timeout /t 12 /nobreak >nul

cd /d "%~dp0App"

echo [2/4] Starting frontend...
start "VOENCOM Frontend" cmd /k "npm run dev"
timeout /t 10 /nobreak >nul

echo [3/4] Starting tunnel...
start "VOENCOM Tunnel" cmd /k "ssh -R voencom.serveousercontent.com:80:localhost:5173 serveo.net"

echo [4/4] Open browser...
start http://localhost:5173

echo.
echo ========================
echo URL: https://voencom.serveousercontent.com
echo ========================

pause