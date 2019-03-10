namespace StyleCopAnalyzersCmd
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

    public class DirectoryFileReader : IProjectReader
    {
        public DirectoryFileReader()
        {
        }

        ImmutableArray<Project> IProjectReader.ReadAllSourceCodeFiles(string directory, string stylecopJsonFile)
        {
            if (!Directory.Exists(directory))
            {
                Console.Error.WriteLine($"Could not find a part of the path '{directory}'");
                return ImmutableArray<Project>.Empty;
            }

            var files = new DirectoryInfo(directory)?.GetFiles("*.cs", SearchOption.AllDirectories);
            if (files == null)
            {
                Console.Error.WriteLine($"Could not find csharp source files in '{directory}'");
                return ImmutableArray<Project>.Empty;
            }

            var syntaxTrees = new List<SyntaxTree>();

            MSBuildLocator.RegisterDefaults();
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            var solution = workspace.CurrentSolution;
            var project = solution.AddProject("Temp", "Temp", "C#");

            var lockObj = new object();
            Parallel.ForEach(files, (f) =>
            {
                var fileText = CSharpSyntaxTree.ParseText(File.ReadAllText(f.FullName), null, f.FullName);
                lock (lockObj)
                {
                    project = project.AddDocument(f.Name, fileText.GetRoot(), null, f.FullName)
                        .Project;
                }
            });

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