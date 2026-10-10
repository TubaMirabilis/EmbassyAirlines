#:package CliWrap
using CliWrap;
using CliWrap.Exceptions;

var reportDirectory = Path.Combine("Reports", "CoverageReport");

try
{
    Console.WriteLine($"Cleaning up previous coverage reports in {reportDirectory}...");

    foreach (var file in Directory.EnumerateFiles(
        Directory.GetCurrentDirectory(),
        "coverage.cobertura*.xml",
        SearchOption.AllDirectories))
    {
        File.Delete(file);
    }

    if (Directory.Exists(reportDirectory))
    {
        Directory.Delete(reportDirectory, recursive: true);
    }
    await Cli.Wrap("dotnet")
        .WithArguments([
            "test",
            "--coverlet",
            "--coverlet-output-format",
            "cobertura",
            "--coverlet-exclude-assemblies-without-sources",
            "MissingAll"
        ])
        .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
        .WithStandardErrorPipe(PipeTarget.ToDelegate(Console.Error.WriteLine))
        .ExecuteAsync();

    Console.WriteLine($"Generating coverage report in {reportDirectory}...");

    await Cli.Wrap("reportgenerator")
        .WithArguments([
            "-reports:**/coverage.cobertura*.xml",
            $"-targetdir:{reportDirectory}",
            "-reporttypes:html"
        ])
        .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
        .WithStandardErrorPipe(PipeTarget.ToDelegate(Console.Error.WriteLine))
        .ExecuteAsync();

    var reportPath = Path.GetFullPath(
        Path.Combine(reportDirectory, "index.html"));

    Console.WriteLine($"Coverage report generated at {reportPath}");
}
catch (CommandExecutionException ex)
{
    await Console.Error.WriteLineAsync(
        $"Command execution failed: {ex.Message}");

    Environment.ExitCode = 1;
}
catch (IOException ex)
{
    await Console.Error.WriteLineAsync(
        $"File operation failed: {ex.Message}");

    Environment.ExitCode = 1;
}
catch (UnauthorizedAccessException ex)
{
    await Console.Error.WriteLineAsync(
        $"Access denied: {ex.Message}");

    Environment.ExitCode = 1;
}
