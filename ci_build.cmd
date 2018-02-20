@echo off

if "%1"=="" (
    echo ** missing input file! **
    goto exit
)

set PCFG=Release
set PPLT=x86

if "%BUILD_LOGGER%"=="" (
    msbuild %1 /p:Configuration=%PCFG% /p:Platform=%PPLT%
) else (
    msbuild %1 /p:Configuration=%PCFG% /p:Platform=%PPLT% /logger:%BUILD_LOGGER%
)

:exit