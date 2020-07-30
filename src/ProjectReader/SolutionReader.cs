namespace StyleCopAnalyzers.CLI
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Threading.Tasks;
    using System.Linq;
    using Microsoft.Build.Locator;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.MSBuild;
    using Microsoft.CodeAnalysis.Text;
    using System;

    public class SolutionReader : IProjectReader
    {
        public SolutionReader()
        {
        }

        ImmutableArray<Project> IProjectReader.ReadAllSourceCodeFiles(string solutionFilePath, string stylecopJsonFile)
        {
            if (!File.Exists(solutionFilePath))
            {
                Console.Error.WriteLine($"Could not find a solution file '{solutionFilePath}'");
                return ImmutableArray<Project>.Empty;
            }

            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }

            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            var solution = workspace.OpenSolutionAsync(solutionFilePath).Result;

            var projects = ImmutableArray.CreateBuilder<Project>(solution.Projects.Count());

            foreach (var project in solution.Projects)
            {
                if (!string.IsNullOrEmpty(stylecopJsonFile) && File.Exists(stylecopJsonFile))
                {
                    projects.Add(project.AddAdditionalDocument("stylecop.json", SourceText.From(File.ReadAllText(stylecopJsonFile))).Project);
                }
            }

            workspace.Dispose();

            return projects.ToImmutable();
        }
    }
}