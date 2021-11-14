using Microsoft.CodeAnalysis.CodeMetrics;

namespace DotNetCollectorForGitHub.Extensions;

static class CodeAnalysisMetricDataExtensions
{
    internal static string ToCyclomaticComplexityEmoji(this CodeAnalysisMetricData metric) =>
        metric.CyclomaticComplexity switch
        {
            >= 0 and <= 7 => "✔️",
            8 or 9 => "👀",
            10 or 11 => "⚠️",
            12 or 14 => "❌",
            _ => "🔥"
        };
}
