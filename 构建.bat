@echo off
chcp 65001 >nul
rem PCL3 一键构建入口：直接双击为内测构建；带「正式版」参数为正式版本构建
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0构建.ps1" %*
pause
