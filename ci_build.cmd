@echo off

if "%1"=="" (
    echo ** missing input file! **
    goto exit
)

set PCFG=Release

if "%IS_APPVEYOR%"=="" (
    msbuild %1 /p:Configuration=%PCFG%
) else (
    set BUILD_LOGGER="C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll"
    msbuild %1 /p:Configuration=%PCFG% /logger:%BUILD_LOGGER%
)

:exit