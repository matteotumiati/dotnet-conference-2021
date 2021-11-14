using System;
using System.IO;
using DotNetCollectorForGitHub.CodeAnalysis.Models;

namespace DotNetCollectorForGitHub.CodeAnalysis.Utils;

internal static class FileUtilities
{
    internal static string? ResolveRelativePath(string? path, string? baseDirectory)
        => ResolveRelativePath(path, null, baseDirectory);

    internal static string? ResolveRelativePath(string? path, string? basePath, string? baseDirectory) 
        => ResolveRelativePath(PathUtilities.GetPathKind(path), path, basePath, baseDirectory);

    private static string? ResolveRelativePath(PathKind kind, string? path, string? basePath, string? baseDirectory)
    {
        switch (kind)
        {
            case PathKind.Empty:
                return null;

            case PathKind.Relative:
                baseDirectory = GetBaseDirectory(basePath, baseDirectory);
                return PathUtilities.CombinePathsUnchecked(baseDirectory!, path);

            default:
                throw new InvalidOperationException($"Unexpected value '{kind}' of type '{kind.GetType().FullName}'");
        }
    }

    private static string? GetBaseDirectory(string? basePath, string? baseDirectory)
    {
        // relative base paths are relative to the base directory:
        string? resolvedBasePath = ResolveRelativePath(basePath, baseDirectory);
        if (resolvedBasePath == null)
        {
            return baseDirectory;
        }

        // Note: Path.GetDirectoryName doesn't normalize the path and so it doesn't depend on the process state.
        try
        {
            return Path.GetDirectoryName(resolvedBasePath);
        }
        catch (Exception)
        {
            return null;
        }
    }

    internal static string? TryNormalizeAbsolutePath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return null;
        }
    }

    /// <exception cref="IOException"/>
    internal static DateTime GetFileTimeStamp(string fullPath)
    {
        try
        {
            return File.GetLastWriteTimeUtc(fullPath);
        }
        catch (IOException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new IOException(e.Message, e);
        }
    }
}
