@echo off
SETLOCAL ENABLEDELAYEDEXPANSION

REM Define a configuração para Release
SET BUILD_CONFIG=Release

REM Caminho para o diretório de saída dos pacotes NuGet
SET NUGET_OUTPUT_DIR=packages

REM Limpa a solução
dotnet clean -c %BUILD_CONFIG%
IF %ERRORLEVEL% NEQ 0 (
    echo Falha ao limpar a solução.
    exit /b %ERRORLEVEL%
)

REM Constrói a solução
dotnet build -c %BUILD_CONFIG% --no-restore
IF %ERRORLEVEL% NEQ 0 (
    echo Falha ao construir a solução.
    exit /b %ERRORLEVEL%
)

REM Empacota os projetos
dotnet pack -c %BUILD_CONFIG% -o %NUGET_OUTPUT_DIR% --no-build
IF %ERRORLEVEL% NEQ 0 (
    echo Falha ao empacotar os projetos.
    exit /b %ERRORLEVEL%
)

echo Pacotes NuGet gerados com sucesso.
exit /b 0
