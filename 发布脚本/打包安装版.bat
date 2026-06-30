@echo off
chcp 65001 >nul
title 陈叔叔工具箱 - 安装版打包

set "ISCC=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
set "DIR=%~dp0"

echo ========================================
echo   陈叔叔工具箱 - 安装版打包
echo ========================================
echo.

if not exist "%ISCC%" (
    echo [错误] 未找到 Inno Setup 6
    echo 请先安装: https://jrsoftware.org/isinfo.php
    pause
    exit /b 1
)

echo [1/2] 编译基础版（Lite）...
"%ISCC%" "%DIR%陈叔叔工具箱.iss"
if %errorlevel% neq 0 (
    echo [错误] 基础版编译失败
    pause
    exit /b 1
)
echo.

echo [2/2] 编译完整版（Full）...
"%ISCC%" "%DIR%陈叔叔工具箱-Full.iss"
if %errorlevel% neq 0 (
    echo [错误] 完整版编译失败
    pause
    exit /b 1
)
echo.

echo ========================================
echo   完成！文件在 发布 目录：
echo ========================================
dir /b "%DIR%..\发布\*.exe" 2>nul
echo.
pause
