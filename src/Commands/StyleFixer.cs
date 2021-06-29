
namespace StyleCopAnalyzers.CLI
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using CommandLine;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;

    [Verb("fix", HelpText = "Fix C# Coding Style")]
    public class StyleFixer
    {
        [Option('r', "ruleset", Required = false, HelpText = "Ruleset file path.")]
        public string RuleSetFilePath { get; set; } = string.Empty;
        [Option('j', "json", Required = false, HelpText = "stylecop.json file path")]
        public string StyleCopJsonFilePath { get; set; } = string.Empty;
        [Option('v', "verbose", Required = false)]
        public bool LogLevelIsVerbose { get; set; }
        [Value(0, MetaName = "sln/csproj file path, directory path or file path")]
        public IEnumerable<string> Targets { get; set; }

        private ImmutableArray<CodeFixProvider> allCodeFixProviders;
        private ImmutableArray<DiagnosticAnalyzer> allAnalyzers;
        private AnalyzerLoader analyzerLoader;

        public StyleFixer() { }

        private ILogger logger = new SilentLogger();

        public void SetLogger(ILogger logger)
        {
            this.logger = logger;
        }

        public void Initialize()
        {
            this.analyzerLoader = new AnalyzerLoader(RuleSetFilePath);
            this.allAnalyzers = analyzerLoader.GetAnalyzers();
            this.allCodeFixProviders = analyzerLoader.GetCodeFixProviders();
        }

        public async Task FixCode(CancellationToken cancellationToken)
        {
            RuleSetFilePath = CommandHelper.GetAbsoluteOrDefaultFilePath(RuleSetFilePath, "./stylecop.ruleset");
            StyleCopJsonFilePath = CommandHelper.GetAbsoluteOrDefaultFilePath(StyleCopJsonFilePath, "./stylecop.json");

            if (!Targets.Any())
            {
                return;
            }

            this.logger.LogDebug("Arguments ============================");
            this.logger.LogDebug($"Verbose Log : {LogLevelIsVerbose}");
            this.logger.LogDebug($"ruleset : {RuleSetFilePath}");
            this.logger.LogDebug($"stylecop.json : {RuleSetFilePath}");
            this.logger.LogDebug($"fix : \n{string.Join("\n", Targets)}");
            this.logger.LogDebug("======================================");

            if (LogLevelIsVerbose)
            {
                this.logger.SetLogLevel(LogLevel.Verbose);
            }

            Initialize();

            foreach (var target in Targets)
            {
                var targetFileOrDirectory = CommandHelper.GetAbsolutePath(target);
                var inputKind = CommandHelper.GetInputKindFromFileOrDirectory(targetFileOrDirectory);
                if (!inputKind.HasValue) { continue; }

                foreach (var analyzer in this.allAnalyzers)
                {
                    this.logger.LogVerbose("Analyze :" + string.Join(",", analyzer.SupportedDiagnostics.Select(d => d.Id)));
                    foreach (var descriptor in analyzer.SupportedDiagnostics)
                    {
                        this.logger.LogVerbose(" " + descriptor.Description);
                    }

                    var projects = inputKind.Value.ToReader().ReadAllSourceCodeFiles(target, StyleCopJsonFilePath);
                    if (projects.IsDefaultOrEmpty) { return; }

                    var diagnostics = await CommandHelper.GetAnalyzerDiagnosticsAsync(
                            projects,
                            ImmutableArray.Create(analyzer),
                            analyzerLoader.RuleSets,
                            cancellationToken)
                        .ConfigureAwait(false);

                    if (diagnostics.IsDefaultOrEmpty)
                    {
                        continue;
                    }

                    var fixableCodeFixProviders = GetFixableCodeFixProviders(diagnostics.Select(d => d.Id).ToImmutableArray());
                    if (fixableCodeFixProviders.IsDefaultOrEmpty)
                    {
                        this.logger.LogVerbose($"Not Fixed : {diagnostics[0].Location.SourceTree?.FilePath}\n    {diagnostics[0].Id} {diagnostics[0].GetMessage()}\n    NotFound codeFixProvider");
                        continue;
                    }

                    try
                    {
                        this.logger.LogVerbose($"Try Fix : {diagnostics[0].Id} {diagnostics[0].GetMessage()}");
                        var fixedContexts = await FixDiagnosticsAsync(projects, diagnostics, fixableCodeFixProviders, cancellationToken).ConfigureAwait(false);
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
                throw new ArgumentException();
            };

            var group = diagnostics.GroupBy(d => d.Location.SourceTree?.FilePath);

            var fixedContexts = ImmutableArray.CreateBuilder<FixedDocumentContext>();
            try
            {
                foreach (var codeFixProvider in codeFixProviders)
                {
                    foreach (var groupingDiagnostics in group)
                    {
                        var fixDiagnostics = groupingDiagnostics.ToImmutableArray();
                        var syntaxTree = fixDiagnostics[0].Location.SourceTree;
                        if (syntaxTree == null) { continue; }

                        var oldDocument = getDocument(syntaxTree);
                        var codeFixer = new SingleDiagnosticCodeFixer(oldDocument, fixDiagnostics, codeFixProvider);
                        var fixedResult = await codeFixer.FixCode(cancellationToken).ConfigureAwait(false);

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
    }
}