@echo off

echo Cleaning old coverage files...
del /s /q coverage.cobertura*.xml 2>nul
rmdir /s /q Reports\CoverageReport 2>nul

echo Running tests and generating coverage...

dotnet test --coverlet --coverlet-output-format cobertura --coverlet-exclude-assemblies-without-sources MissingAll

if errorlevel 1 (
    echo Tests failed!
    pause
    exit /b 1
)

echo Generating HTML coverage report...

reportgenerator -reports:**/coverage.cobertura*.xml -targetdir:Reports/CoverageReport -reporttypes:html

if errorlevel 1 (
    echo Report generation failed!
    pause
    exit /b 1
)

echo.
echo Coverage report generated successfully!
echo Open Reports\CoverageReport\index.html
pause