using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DotNetCollectorForGitHub.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeMetrics;
using Microsoft.Extensions.Logging;

namespace DotNetCollectorForGitHub.Analyzers;

sealed class ProjectMetricDataAnalyzer
{
    private readonly ILogger<ProjectMetricDataAnalyzer> _logger;

    public ProjectMetricDataAnalyzer(ILogger<ProjectMetricDataAnalyzer> logger) => _logger = logger;

    internal async Task<ImmutableArray<(string, CodeAnalysisMetricData)>> AnalyzeAsync(ProjectWorkspace workspace, string path, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();

        if (File.Exists(path))
        {
            _logger.LogInformation($"Computing analytics on {path}.");
        }
        else
        {
            _logger.LogWarning($"{path} doesn't exist.");
            return ImmutableArray<(string, CodeAnalysisMetricData)>.Empty;
        }

        var projects = workspace.LoadProject(path);
        var builder = ImmutableArray.CreateBuilder<(string, CodeAnalysisMetricData)>();

        foreach (var project in projects)
        {
            var compilation = await project.GetCompilationAsync(cancellation);
            var metricData = await CodeAnalysisMetricData.ComputeAsync(compilation!.Assembly, new CodeMetricsAnalysisContext(compilation, cancellation));

            builder.Add((project.FilePath!, metricData));
        }

        return builder.ToImmutable();
    }
}
