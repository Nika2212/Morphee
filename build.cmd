@echo off
rem Builds release\Morphee.exe with the C# compiler that ships with Windows (.NET Framework 4.x).
setlocal
set CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo csc.exe not found - .NET Framework 4.x is required
  exit /b 1
)
cd /d "%~dp0"
if not exist release mkdir release
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu /win32icon:build\icon.ico /out:release\Morphee.exe src\Morphee.cs
