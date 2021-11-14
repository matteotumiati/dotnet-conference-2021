using DotNetCollectorForGitHub.Analyzers;
using DotNetCollectorForGitHub.CodeAnalysis;
using DotNetCollectorForGitHub.CodeAnalysis.Utils;
using Microsoft.Build.Locator;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCollectorForGitHub.Extensions;

static class ServiceCollectionExtensions
{
    internal static IServiceCollection AddGitHubActionServices(this IServiceCollection services)
    {
        MSBuildLocator.RegisterDefaults();

        services.AddSingleton<ProjectLoader>()
                .AddSingleton<ProjectWorkspace>()
                .AddSingleton<ProjectMetricDataAnalyzer>();

        return services;
    }
}
