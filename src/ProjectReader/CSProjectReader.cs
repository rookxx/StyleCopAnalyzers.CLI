namespace StyleCopAnalyzers.CLI
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Build.Locator;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.MSBuild;
    using Microsoft.CodeAnalysis.Text;

    public class CSProjectReader : IProjectReader
    {
        public CSProjectReader()
        {
        }

        ImmutableArray<Project> IProjectReader.ReadAllSourceCodeFiles(string csprojFilePath, string stylecopJsonFile)
        {
            if (!File.Exists(csprojFilePath))
            {
                Console.Error.WriteLine($"Could not find a csproj file '{csprojFilePath}'");
                return ImmutableArray<Project>.Empty;
            }

            var syntaxTrees = new List<SyntaxTree>();

            MSBuildLocator.RegisterDefaults();
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            var project = workspace.OpenProjectAsync(csprojFilePath).Result;

            if (!string.IsNullOrEmpty(stylecopJsonFile) && File.Exists(stylecopJsonFile))
            {
                project = project.AddAdditionalDocument("stylecop.json", SourceText.From(File.ReadAllText(stylecopJsonFile)))
                    .Project;
            }

            var projects = ImmutableArray.Create(project);

            return projects;
        }
    }
}