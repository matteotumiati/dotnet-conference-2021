using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using DotNetCollectorForGitHub.CodeAnalysis.Models;
using DotNetCollectorForGitHub.CodeAnalysis.Utils;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using AnalysisProject = Microsoft.CodeAnalysis.Project;
using MSBProject = Microsoft.Build.Evaluation.Project;

namespace DotNetCollectorForGitHub.CodeAnalysis;

public class ProjectWorkspace : IDisposable
{
    private BuildManager _buildManager = BuildManager.DefaultBuildManager;
    private readonly AdhocWorkspace _workspace = new();
    private readonly ProjectLoader _projectLoader;
    private readonly ILogger<ProjectWorkspace> _logger;
    private static readonly char[] s_directorySplitChars = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

    public ProjectWorkspace(ProjectLoader projectLoader, ILogger<ProjectWorkspace> logger) =>
        (_projectLoader, _logger) = (projectLoader, logger);

    public ImmutableArray<AnalysisProject> LoadProject(string path)
    {
        var projectDirectory = Path.GetDirectoryName(path)!;
        var language = LanguageNames.CSharp;
        var project = _projectLoader.LoadProject(path);
        var builder = ImmutableArray.CreateBuilder<AnalysisProject>();
        var buildProjectCollection = new ProjectCollection();
        var buildParameters = new BuildParameters(buildProjectCollection);

        _buildManager.BeginBuild(buildParameters);

        try
        {
            var projectInfos = LoadProjectInfos(project, language, projectDirectory);

            foreach (var projectInfo in projectInfos)
            {
                builder.Add(_workspace.AddProject(projectInfo));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        finally
        {
            _buildManager.EndBuild();
        }

        return builder.ToImmutable();
    }

    private ImmutableArray<ProjectInfo> LoadProjectInfos(MSBProject project, string language, string projectDirectory)
    {
        var projectInstance = project.CreateProjectInstance();
        var projectFileInfo = CreateProjectFileInfo(projectInstance, project, language, projectDirectory);
        var projectPath = projectFileInfo.FilePath;
        var projectId = ProjectId.CreateNewId(debugName: projectFileInfo.FilePath);
        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        var assemblyName = Path.GetFileNameWithoutExtension(projectPath);
        var documents = CreateDocumentInfos(projectFileInfo.Documents, projectId);

        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(FileUtilities.GetFileTimeStamp(projectPath)),
            projectName,
            assemblyName,
            language,
            projectPath,
            documents: documents);

        return ImmutableArray.Create(projectInfo);
    }

    private ProjectFileInfo CreateProjectFileInfo(ProjectInstance projectInstance, MSBProject loadedProject, string language, string projectDirectory)
    {
        var outputFilePath = projectInstance.GetPropertyValue("TargetPath");
        var outputRefFilePath = projectInstance.GetPropertyValue("TargetRefPath");
        var defaultNamespace = projectInstance.GetPropertyValue("RootNamespace") ?? string.Empty;
        var targetFramework = projectInstance.GetPropertyValue("TargetFramework");

        var docs = projectInstance.GetItems("Compile")
            .Cast<ITaskItem>()
            .Where(i => !Path.GetFileName(i.ItemSpec).StartsWith("TemporaryGeneratedFile_", StringComparison.Ordinal))
            .Select(i => MakeDocumentFileInfo(i, projectDirectory))
            .ToImmutableArray();

        var additionalDocs = projectInstance.GetItems("AdditionalFiles")
            .Cast<ITaskItem>()
            .Select(i => MakeDocumentFileInfo(i, projectDirectory))
            .ToImmutableArray();

        return ProjectFileInfo.Create(
            language,
            projectInstance.FullPath,
            outputFilePath,
            outputRefFilePath,
            defaultNamespace,
            targetFramework ?? "<unknown>",
            ImmutableArray<string>.Empty,
            docs,
            additionalDocs,
            ImmutableArray<DocumentFileInfo>.Empty,
            ImmutableArray<ProjectFileReference>.Empty
            );
    }

    private DocumentFileInfo MakeDocumentFileInfo(ITaskItem documentItem, string projectDirectory)
    {
        var logicalPath = documentItem.ItemSpec;
        var filePath = FileUtilities.TryNormalizeAbsolutePath(
            FileUtilities.ResolveRelativePath(documentItem.ItemSpec, projectDirectory) ?? documentItem.ItemSpec)
            ?? documentItem.ItemSpec;

        return new DocumentFileInfo(filePath, logicalPath, false, false, SourceCodeKind.Regular);
    }

    private static ImmutableArray<DocumentInfo> CreateDocumentInfos(IReadOnlyList<DocumentFileInfo> documentFileInfos, ProjectId projectId)
    {
        var results = ImmutableArray.CreateBuilder<DocumentInfo>();

        foreach (var info in documentFileInfos)
        {
            GetDocumentNameAndFolders(info.LogicalPath, out var name, out var folders);

            var documentInfo = DocumentInfo.Create(
                DocumentId.CreateNewId(projectId, debugName: info.FilePath),
                name,
                folders,
                info.SourceCodeKind,
                new FileTextLoader(info.FilePath, null),
                info.FilePath,
                info.IsGenerated);

            results.Add(documentInfo);
        }

        return results.ToImmutable();
    }

    private static void GetDocumentNameAndFolders(string logicalPath, out string name, out ImmutableArray<string> folders)
    {
        var pathNames = logicalPath.Split(s_directorySplitChars, StringSplitOptions.RemoveEmptyEntries);

        if (pathNames.Length > 0)
        {
            if (pathNames.Length > 1)
            {
                folders = pathNames.Take(pathNames.Length - 1).ToImmutableArray();
            }
            else
            {
                folders = ImmutableArray<string>.Empty;
            }

            name = pathNames[pathNames.Length - 1];
        }
        else
        {
            name = logicalPath;
            folders = ImmutableArray<string>.Empty;
        }
    }

    public void Dispose()
    {
        _buildManager?.Dispose();
        _buildManager = null!;
    }
}
