using Microsoft.CodeAnalysis;

namespace DotNetCollectorForGitHub.CodeAnalysis.Models;

internal sealed record DocumentFileInfo(string FilePath, string LogicalPath, bool IsLinked, bool IsGenerated, SourceCodeKind SourceCodeKind);
