using System.Collections.Immutable;

namespace DotNetCollectorForGitHub.CodeAnalysis.Models;

internal sealed record ProjectFileReference(string Path, ImmutableArray<string> Aliases);
