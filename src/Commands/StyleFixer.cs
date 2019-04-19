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
        [Option('v', "verbose", Required = false)]
        public bool LogLevelIsVerbose { get; set; }
        [Option('i', "id", Required = false)]
        public string RuleSetId { get; set; }
        [Value(0, MetaName = "sln/csproj file path or directory path")]
        public string TargetFileOrDirectory { get; set; }

        private ImmutableArray<CodeFixProvider> allCodeFixProviders;
        private ImmutableArray<DiagnosticAnalyzer> allAnalyzers;

        public StyleFixer() { }

        private ILogger logger = new SilentLogger();

        public void SetLogger(ILogger logger)
        {
            this.logger = logger;
        }

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

            this.logger.LogDebug($"Arguments ============================");
            this.logger.LogDebug($"Verbose Log : {LogLevelIsVerbose}");
            this.logger.LogDebug($"ruleset : {RuleSetFilePath}");
            this.logger.LogDebug($"stylecop.json : {RuleSetFilePath}");
            this.logger.LogDebug($"fix : {TargetFileOrDirectory}");
            this.logger.LogDebug($"======================================");

            if (LogLevelIsVerbose)
            {
                this.logger.SetLogLevel(LogLevel.Verbose);
            }

            var isSpecifiedRuleSetId = !string.IsNullOrEmpty(RuleSetId);
            if (isSpecifiedRuleSetId)
            {
                RuleSetFilePath = null;
                StyleCopJsonFilePath = null;
            }

            Initialize();

            var inputKind = CommandHelper.GetInputKindFromFileOrDirectory(TargetFileOrDirectory);
            if (!inputKind.HasValue) { return; }

            if (isSpecifiedRuleSetId)
            {
                await FixSingleDiagnostics(RuleSetId, inputKind.Value, cancellationToken);
            }
            else
            {
                await FixAllDiagnostics(inputKind.Value, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task FixAllDiagnostics(InputKind inputKind, CancellationToken cancellationToken)
        {
            foreach (var analyzer in this.allAnalyzers)
            {
                await FixDiagnostic(analyzer, inputKind, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task FixSingleDiagnostics(string rulesetId, InputKind inputKind, CancellationToken cancellationToken)
        {
            var analyzer = this.allAnalyzers.FirstOrDefault(
                    a => a.SupportedDiagnostics.FirstOrDefault(d => { Console.WriteLine(d.Id); return d.Id == rulesetId; }) != null);
            if (analyzer == null)
            {
                Console.WriteLine($"Not found or Not Supported rulesetId : {rulesetId}");
                return;
            }

            await FixDiagnostic(analyzer, inputKind, cancellationToken).ConfigureAwait(false);
        }

        private async Task FixDiagnostic(DiagnosticAnalyzer analyzer, InputKind inputKind, CancellationToken cancellationToken)
        {
            this.logger.LogVerbose("Analyze :" + string.Join(",", analyzer.SupportedDiagnostics.Select(d => d.Id)));
            foreach (var descriptor in analyzer.SupportedDiagnostics)
            {
                this.logger.LogVerbose(" " + descriptor.Description);
            }

            var projects = LoadProject(inputKind);
            if (projects.Length <= 0) { return; }

            var diagnostics = await CommandHelper.GetAnalyzerDiagnosticsAsync(
                    projects,
                    ImmutableArray.Create(analyzer),
                    cancellationToken)
                .ConfigureAwait(false);

            if (diagnostics.Length <= 0)
            {
                return;
            }

            var fixableCodeFixProviders = GetFixableCodeFixProviders(diagnostics.Select(d => d.Id).ToImmutableArray());
            if (fixableCodeFixProviders.Count() <= 0)
            {
                this.logger.LogVerbose($"Not Fixed : {diagnostics[0].Location.SourceTree.FilePath}\n    {diagnostics[0].Id} {diagnostics[0].GetMessage()}\n    NotFound codeFixProvider");
                return;
            }

            try
            {
                this.logger.LogVerbose($"Try Fix : {diagnostics[0].Id} {diagnostics[0].GetMessage()}");
                var fixedContexts = await FixDiagnosticsAsync(projects, diagnostics, fixableCodeFixProviders, cancellationToken);
                var documentWriter = new FixedDocumentContextWriter() as IFixedContextWriter;
                documentWriter.SetLogger(this.logger);
                foreach (var context in fixedContexts)
                {
                    documentWriter.Write(context);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
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
                    foreach (var groupingDiagnostics in group)
                    {
                        var fixDiagnostics = groupingDiagnostics.ToImmutableArray();
                        var syntaxTree = fixDiagnostics.First().Location.SourceTree;

                        var oldDocument = getDocument(syntaxTree);
                        var codeFixer = new SingleDiagnosticCodeFixer(oldDocument, fixDiagnostics, codeFixProvider);
                        var fixedResult = await codeFixer.FixCode(cancellationToken);

                        fixedContexts.Add(new FixedDocumentContext(oldDocument, fixedResult));
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