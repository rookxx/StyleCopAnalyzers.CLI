namespace StyleCopAnalyzersCmd
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using CommandLine;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    [Verb("check", HelpText = "Check C# Coding Style")]
    public class StyleChecker
    {
        [Option('r', "ruleset", Required = false, HelpText = "Ruleset file path.")]
        public string RuleSetFilePath { get; set; }
        [Option('s', "stylcopjson", Required = false, HelpText = "stylecop.json file path")]
        public string StyleCopJsonFilePath { get; set; }
        [Option('f', "format", Required = false, Default = "text", HelpText = "output format\n    text raw text\n    xml  legacy stylecop xml format")]
        public string OutputFormat { get; set; }
        [Value(0, MetaName = "sln/csproj file path or directory path")]
        public string TargetFileOrDirectory { get; set; }

        public StyleChecker()
        {
        }

        private Stopwatch stopwatch;

        [Conditional("DEBUG")]
        private void DebugTimeLog(string message)
        {
            Console.WriteLine($"{message} in {stopwatch.ElapsedMilliseconds}ms");
        }

        public async Task Check(CancellationToken cancellationToken)
        {
            RuleSetFilePath = CommandHelper.GetAbsoluteOrDefaultFilePath(RuleSetFilePath, "./stylecop.ruleset");
            StyleCopJsonFilePath = CommandHelper.GetAbsoluteOrDefaultFilePath(StyleCopJsonFilePath, "./stylecop.json");
            TargetFileOrDirectory = CommandHelper.GetAbsolutePath(TargetFileOrDirectory);

            stopwatch = Stopwatch.StartNew();

            var inputKind = CommandHelper.GetInputKindFromFileOrDirectory(TargetFileOrDirectory);
            if (!inputKind.HasValue) { return; }

            var projects = inputKind.Value.ToReader().ReadAllSourceCodeFiles(TargetFileOrDirectory, StyleCopJsonFilePath);
            if (projects.Length <= 0) { return; }

            var outputKind = OutputKindHelper.ToOutputKind(OutputFormat);
            if (outputKind == OutputKind.Undefined)
            {
                Console.Error.WriteLine($"output format is undefined. -f {OutputFormat}");
                return;
            }

            var analyzerLoader = new AnalyzerLoader(RuleSetFilePath);
            var analyzers = analyzerLoader.GetAnalyzers();
            var diagnostics = await CommandHelper.GetAnalyzerDiagnosticsAsync(projects, analyzers, cancellationToken).ConfigureAwait(false);

            var writer = outputKind.ToWriter();
            writer.Write(diagnostics);

            DebugTimeLog("Check Style Completed");
        }
    }
}