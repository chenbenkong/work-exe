@echo off
chcp 65001 >nul
setlocal

REM WorkExe 构建并运行脚本
REM 要求：Visual Studio 2019/2022 或 Build Tools，MSBuild 在 PATH 中

echo [1/4] 生成素材...
if exist "scripts\generate_assets.py" (
    python scripts\generate_assets.py
    if errorlevel 1 (
        echo 素材生成失败，请确保已安装 Pillow：pip install Pillow
        pause
        exit /b 1
    )
)

echo [2/4] 还原 NuGet 包...
nuget restore WorkExe.sln
if errorlevel 1 (
    echo 未找到 nuget.exe，请从 https://www.nuget.org/downloads 下载并放到 PATH。
    pause
    exit /b 1
)

echo [3/4] 编译 Release...
msbuild WorkExe.sln /p:Configuration=Release /p:Platform="Any CPU"
if errorlevel 1 (
    echo 编译失败。
    pause
    exit /b 1
)

echo [4/4] 启动程序...
start "" "WorkExe\bin\Release\WorkExe.exe"
endlocal
