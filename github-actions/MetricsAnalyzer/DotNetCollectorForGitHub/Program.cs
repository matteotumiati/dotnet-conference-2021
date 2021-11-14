using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using DotNetCollectorForGitHub.Analyzers;
using DotNetCollectorForGitHub.CodeAnalysis;
using DotNetCollectorForGitHub.Extensions;
using DotNetCollectorForGitHub.Models;
using Microsoft.CodeAnalysis.CodeMetrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static CommandLine.Parser;

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) => services.AddGitHubActionServices()).Build();

static TService Get<TService>(IHost host) where TService : notnull =>
    host.Services.GetRequiredService<TService>();

static async Task StartAnalysisAsync(InputParameters inputs, IHost host)
{
    Console.WriteLine($"Analyzing source from repo {inputs.Name} ({inputs.Owner}) in branch {inputs.Branch}.", ConsoleColor.Green);

    Matcher matcher = new();
    matcher.AddIncludePatterns(new[] { "**/*.csproj" });

    using ProjectWorkspace workspace = Get<ProjectWorkspace>(host);
    var projectAnalyzer = Get<ProjectMetricDataAnalyzer>(host);
    var projects = matcher.GetResultsInFullPath(inputs.Directory);
    var average = new List<KeyValuePair<int, int>>();

    foreach (var project in projects)
    {
        var metrics = await projectAnalyzer.AnalyzeAsync(workspace, project, CancellationToken.None);

        foreach ((string path, CodeAnalysisMetricData metric) in metrics)
        {
            Console.Write($"Analyzed project: ", ConsoleColor.Green); Console.WriteLine(project);
            Console.Write($"LOC: ", ConsoleColor.Green); Console.WriteLine(metric.SourceLines);
            Console.Write($"Cyclomatic complexity: ", ConsoleColor.Green); Console.WriteLine($"{metric.CyclomaticComplexity} ({metric.ToCyclomaticComplexityEmoji()})");
            Console.Write($"Maintainability index: ", ConsoleColor.Green); Console.WriteLine(metric.MaintainabilityIndex);
            Console.WriteLine();

            average.Add(new KeyValuePair<int, int>(metric.CyclomaticComplexity, metric.MaintainabilityIndex));
        }
    }

    Console.WriteLine($"::set-output name=avg-complexity::{average.Average(x => x.Key)}");
    Console.WriteLine($"::set-output name=avg-maintainability-index::{average.Average(x => x.Value)}");

    Environment.Exit(0);
}

var parser = Default.ParseArguments<InputParameters>(() => new(), args);
parser.WithNotParsed(
    errors =>
    {
        Get<ILoggerFactory>(host)
            .CreateLogger("DotNetCollectorForGitHub.Program")
            .LogError(string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        
        Environment.Exit(2);
    });

await parser.WithParsedAsync(options => StartAnalysisAsync(options, host));
await host.RunAsync();
