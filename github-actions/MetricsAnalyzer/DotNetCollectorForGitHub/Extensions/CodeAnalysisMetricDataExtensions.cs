using Microsoft.CodeAnalysis.CodeMetrics;

namespace DotNetCollectorForGitHub.Extensions;

static class CodeAnalysisMetricDataExtensions
{
    internal static string ToCyclomaticComplexityEmoji(this CodeAnalysisMetricData metric) =>
        metric.CyclomaticComplexity switch
        {
            >= 0 and <= 7 => ":heavy_check_mark:",
            8 or 9 => ":warning:",
            10 or 11 => ":radioactive:",
            12 or 14 => ":x:",
            _ => ":feelsgood:"
        };
}
