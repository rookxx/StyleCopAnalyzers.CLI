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

    public class CSharpFileReader : IProjectReader
    {
        public CSharpFileReader()
        {
        }

        ImmutableArray<Project> IProjectReader.ReadAllSourceCodeFiles(string file, string stylecopJsonFile)
        {
            if (!File.Exists(file))
            {
                Console.Error.WriteLine($"Could not find a part of the path '{file}'");
                return ImmutableArray<Project>.Empty;
            }

            var fileInfo = new FileInfo(file);
            if (fileInfo == null)
            {
                Console.Error.WriteLine($"Could not find csharp source files in '{file}'");
                return ImmutableArray<Project>.Empty;
            }

            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }

            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            var solution = workspace.CurrentSolution;
            var project = solution.AddProject("Temp", "Temp", "C#");

            var fileText = CSharpSyntaxTree.ParseText(File.ReadAllText(fileInfo.FullName), null, fileInfo.FullName);

            project = project.AddDocument(fileInfo.Name, fileText.GetRoot(), null, fileInfo.FullName)
                    .Project;

            if (!string.IsNullOrEmpty(stylecopJsonFile) && File.Exists(stylecopJsonFile))
            {
                project = project.AddAdditionalDocument("stylecop.json", SourceText.From(File.ReadAllText(stylecopJsonFile)))
                    .Project;
            }
            workspace.Dispose();

            return ImmutableArray.Create(project);
        }
    }
}