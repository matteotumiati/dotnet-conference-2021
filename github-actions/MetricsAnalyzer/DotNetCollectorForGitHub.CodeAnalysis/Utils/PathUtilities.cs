using System.Diagnostics.CodeAnalysis;
using DotNetCollectorForGitHub.CodeAnalysis.Models;

namespace DotNetCollectorForGitHub.CodeAnalysis.Utils;

internal static class PathUtilities
{
    internal static readonly char DirectorySeparatorChar = PlatformInformation.IsUnix ? '/' : '\\';
    internal const char AltDirectorySeparatorChar = '/';
    internal const string ParentRelativeDirectory = "..";
    internal const string ThisDirectory = ".";
    internal static readonly string DirectorySeparatorStr = new(DirectorySeparatorChar, 1);
    internal const char VolumeSeparatorChar = ':';
    internal static bool IsUnixLikePlatform => PlatformInformation.IsUnix;

    /// <summary>
    /// True if the character is the platform directory separator character or the alternate directory separator.
    /// </summary>
    public static bool IsDirectorySeparator(char c) => c == DirectorySeparatorChar || c == AltDirectorySeparatorChar;

    /// <summary>
    /// Gets the specific kind of relative or absolute path.
    /// </summary>
    public static PathKind GetPathKind(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return PathKind.Empty;
        }

        // "C:\"
        // "\\machine" (UNC)
        // "/etc"      (Unix)
        if (IsAbsolute(path))
        {
            return PathKind.Absolute;
        }

        // "."
        // ".."
        // ".\"
        // "..\"
        if (path.Length > 0 && path[0] == '.')
        {
            if (path.Length == 1 || IsDirectorySeparator(path[1]))
            {
                return PathKind.RelativeToCurrentDirectory;
            }

            if (path[1] == '.')
            {
                if (path.Length == 2 || IsDirectorySeparator(path[2]))
                {
                    return PathKind.RelativeToCurrentParent;
                }
            }
        }

        if (!IsUnixLikePlatform)
        {
            // "\"
            // "\goo"
            if (path.Length >= 1 && IsDirectorySeparator(path[0]))
            {
                return PathKind.RelativeToCurrentRoot;
            }

            // "C:goo"

            if (path.Length >= 2 && path[1] == VolumeSeparatorChar && (path.Length <= 2 || !IsDirectorySeparator(path[2])))
            {
                return PathKind.RelativeToDriveDirectory;
            }
        }

        // "goo.dll"
        return PathKind.Relative;
    }

    /// <summary>
    /// True if the path is an absolute path (rooted to drive or network share)
    /// </summary>
    public static bool IsAbsolute([NotNullWhen(true)] string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        if (IsUnixLikePlatform)
        {
            return path[0] == DirectorySeparatorChar;
        }

        // "C:\"
        if (IsDriveRootedAbsolutePath(path))
        {
            // Including invalid paths (e.g. "*:\")
            return true;
        }

        // "\\machine\share"
        // Including invalid/incomplete UNC paths (e.g. "\\goo")
        return path.Length >= 2 &&
            IsDirectorySeparator(path[0]) &&
            IsDirectorySeparator(path[1]);
    }

    /// <summary>
    /// Returns true if given path is absolute and starts with a drive specification ("C:\").
    /// </summary>
    private static bool IsDriveRootedAbsolutePath(string path)
    {
        return path.Length >= 3 && path[1] == VolumeSeparatorChar && IsDirectorySeparator(path[2]);
    }

    public static string CombinePathsUnchecked(string root, string? relativePath)
    {
        char c = root[root.Length - 1];
        if (!IsDirectorySeparator(c) && c != VolumeSeparatorChar)
        {
            return root + DirectorySeparatorStr + relativePath;
        }

        return root + relativePath;
    }
}
