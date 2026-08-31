@echo off
chcp 65001 >nul
setlocal

REM WorkExe 直接运行脚本（需先 Build 或从 Release 下载）
set EXE=WorkExe\bin\Release\WorkExe.exe
if not exist "%EXE%" (
    echo 未找到 %EXE%，请先运行 Build-and-Run.bat 或从 GitHub Actions 下载 Release 包。
    pause
    exit /b 1
)

echo 正在启动 WorkExe...
start "" "%EXE%"
endlocal
