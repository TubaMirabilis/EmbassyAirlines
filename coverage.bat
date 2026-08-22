@echo off

echo Cleaning old coverage files...
del /s /q coverage.cobertura*.xml 2>nul
rmdir /s /q Reports\CoverageReport 2>nul

echo Running tests and generating coverage...
dotnet test --coverlet --coverlet-output-format cobertura --coverlet-exclude-assemblies-without-sources MissingAll && reportgenerator -reports:**/coverage.cobertura*.xml -targetdir:Reports/CoverageReport -reporttypes:html

echo.
echo Done.
pause