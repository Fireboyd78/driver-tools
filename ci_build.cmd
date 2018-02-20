@echo off

if "%1"=="" (
    echo ** missing input file! **
    goto exit
)

set PCFG=Release
set PPLT=x86

if "%IS_APPVEYOR%"=="" (
    msbuild %1 /p:Configuration=%PCFG% /p:Platform=%PPLT%
) else (
    set BUILD_LOGGER="C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll"
    msbuild %1 /p:Configuration=%PCFG% /p:Platform=%PPLT% /logger:%BUILD_LOGGER%
)

:exit