namespace DotNetCollectorForGitHub.CodeAnalysis.Models;

internal enum PathKind
{
    Empty,
    Relative,
    RelativeToCurrentDirectory,
    RelativeToCurrentParent,
    RelativeToCurrentRoot,
    RelativeToDriveDirectory,
    Absolute
}
