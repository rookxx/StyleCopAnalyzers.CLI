
namespace StyleCopAnalyzers.CLI
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using CommandLine;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Text;

    [Verb("fix", HelpText = "Fix C# Coding Style")]
    public class StyleFixer
    {
        [Option('r', "ruleset", Required = false, HelpText = "Ruleset file path.")]
        public string RuleSetFilePath { get; set; }
        [Option('j', "json", Required = false, HelpText = "stylecop.json file path")]
        public string StyleCopJsonFilePath { get; set; }
        [Value(0, MetaName = "sln/csproj file path or directory path")]
        public string TargetFileOrDirectory { get; set; }

        private ImmutableArray<CodeFixProvider> allCodeFixProviders;
        private ImmutableArray<DiagnosticAnalyzer> allAnalyzers;

        public StyleFixer() { }

        public void Initialize()
        {
            var analyzerLoader = new AnalyzerLoader(RuleSetFilePath);
            this.allAnalyzers = analyzerLoader.GetAnalyzers();
            this.allCodeFixProviders = analyzerLoader.GetCodeFixProviders();
        }

        public async Task FixCode(CancellationToken cancellationToken)
        {
            RuleSetFilePath = CommandHelper.GetAbsoluteOrDefaultFilePath(RuleSetFilePath, "./stylecop.ruleset");
            StyleCopJsonFilePath = CommandHelper.GetAbsoluteOrDefaultFilePath(StyleCopJsonFilePath, "./stylecop.json");
            TargetFileOrDirectory = CommandHelper.GetAbsolutePath(TargetFileOrDirectory);

            Initialize();

            var inputKind = CommandHelper.GetInputKindFromFileOrDirectory(TargetFileOrDirectory);
            if (!inputKind.HasValue) { return; }

            foreach (var analyzer in this.allAnalyzers)
            {
                var projects = LoadProject(inputKind.Value);
                if (projects.Length <= 0) { return; }

                var diagnostics = await CommandHelper.GetAnalyzerDiagnosticsAsync(
                        projects,
                        ImmutableArray.Create(analyzer),
                        cancellationToken)
                    .ConfigureAwait(false);

                if (diagnostics.Length <= 0)
                {
                    continue;
                }

                var fixableCodeFixProviders = GetFixableCodeFixProviders(diagnostics.Select(d => d.Id).ToImmutableArray());
                if (fixableCodeFixProviders.Count() <= 0)
                {
                    Debug.WriteLine($"{diagnostics[0].Id} {diagnostics[0].GetMessage()} NotFound codeFixProvider");
                    continue;
                }

                try
                {
                    var fixedContexts = await FixDiagnosticsAsync(projects, diagnostics, fixableCodeFixProviders, cancellationToken);
                    var documentWriter = new FixedDocumentContextWriter() as IFixedContextWriter;
                    foreach(var context in fixedContexts)
                    {
                        documentWriter.Write(context);
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
        }

        private async Task<ImmutableArray<FixedDocumentContext>> FixDiagnosticsAsync(
            ImmutableArray<Project> projects,
            ImmutableArray<Diagnostic> diagnostics,
            ImmutableArray<CodeFixProvider> codeFixProviders,
            CancellationToken cancellationToken)
        {
            Func<SyntaxTree, Document> getDocument = (syntaxTree) =>
            {
                foreach (var project in projects)
                {
                    var doc = project.Solution.GetDocument(syntaxTree);
                    if (doc != null) { return doc; }
                }
                return null;
            };

            var group = diagnostics.GroupBy(d => d.Location.SourceTree.FilePath);

            var fixedContexts = ImmutableArray.CreateBuilder<FixedDocumentContext>();
            try
            {
                foreach (var codeFixProvider in codeFixProviders)
                {
                    Console.WriteLine($"Try Fix {string.Join(",", codeFixProvider.FixableDiagnosticIds)}");
                    foreach (var groupingDiagnostics in group)
                    {
                        var fixDiagnostics = groupingDiagnostics.ToImmutableArray();
                        var syntaxTree = fixDiagnostics.First().Location.SourceTree;

                        var oldDocument = getDocument(syntaxTree);
                        var codeFixer = new SingleDiagnosticCodeFixer(oldDocument, fixDiagnostics, codeFixProvider);
                        var fixedResult = await codeFixer.FixCode(cancellationToken);

                        fixedContexts.Add(new FixedDocumentContext(oldDocument, fixedResult));

                        Console.WriteLine($"    Fixed {oldDocument.FilePath}");
                    }
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
            }

            return fixedContexts.ToImmutableArray();
        }

        private ImmutableArray<Project> LoadProject(InputKind inputKind)
        {
            return inputKind.ToReader().ReadAllSourceCodeFiles(TargetFileOrDirectory, StyleCopJsonFilePath);
        }

        private ImmutableArray<CodeFixProvider> GetFixableCodeFixProviders(ImmutableArray<string> diagnosticIds)
        {
            return this.allCodeFixProviders.Where(fixer =>
            {
                foreach (var diagnosticId in diagnosticIds)
                {
                    if (fixer.FixableDiagnosticIds.Contains(diagnosticId))
                    {
                        return true;
                    }
                }
                return false;
            })
            .ToImmutableArray();
        }

        private async Task<string> WriteCodeTextAsync(Document document)
        {
            var path = document.FilePath;
            var text = await document.GetTextAsync();

            File.WriteAllText(path, text.ToString());

            return text.ToString();
        }
    }
}