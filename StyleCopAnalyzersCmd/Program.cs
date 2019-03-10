namespace StyleCopAnalyzersCmd
{
    using Microsoft.Build.Locator;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.MSBuild;
    using Microsoft.CodeAnalysis.Text;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Threading;
    using CommandLine;
    using File = System.IO.File;

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
            RuleSetFilePath = ArgumentPathHelper.GetAbsoluteOrDefaultFilePath(RuleSetFilePath, "./stylecop.ruleset");
            StyleCopJsonFilePath = ArgumentPathHelper.GetAbsoluteOrDefaultFilePath(StyleCopJsonFilePath, "./stylecop.json");
            TargetFileOrDirectory = ArgumentPathHelper.GetAbsolutePath(TargetFileOrDirectory);

            stopwatch = Stopwatch.StartNew();

            var inputKind = GetInputKindFromFileOrDirectory(TargetFileOrDirectory);
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

            var diagnosticsAll = new List<Diagnostic>();
            foreach (var project in projects)
            {
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers, project.AnalyzerOptions);
                var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken).ConfigureAwait(false);

                diagnosticsAll.AddRange(diagnostics);
            }

            var writer = outputKind.ToWriter();
            writer.Write(diagnosticsAll);

            DebugTimeLog("Check Style Completed");
        }

        private InputKind? GetInputKindFromFileOrDirectory(string targetFileOrDirectory)
        {
            if (File.Exists(targetFileOrDirectory))
            {
                var fileinfo = new FileInfo(targetFileOrDirectory);
                switch (fileinfo.Extension)
                {
                    case ".csproj": return InputKind.Csproj;
                    case ".sln": return InputKind.Sln;
                    default:
                        Console.Error.WriteLine($"Supported File Extension is .sln or .csproj only. {fileinfo.Extension}");
                        return null;
                }
            }

            if (Directory.Exists(targetFileOrDirectory))
            {
                return InputKind.Directory;
            }

            Console.Error.WriteLine($"Could not find {targetFileOrDirectory}");
            return null;
        }
    }

    [Verb("fix", HelpText = "WIP:Fix Code Style")]
    public class StyleFixer
    {
    }

    internal static class Program
    {
        static async Task Main(string[] args)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress +=
                (sender, e) =>
                {
                    e.Cancel = true;
                    cancellationTokenSource.Cancel();
                };

            try
            {
                await Parser.Default.ParseArguments<StyleChecker, StyleFixer>(args)
                    .MapResult(
                        async (StyleChecker style) =>
                        {
                            await style.Check(cancellationTokenSource.Token).ConfigureAwait(false);
                        },
                        async (StyleFixer style) =>
                        {
                            // WIP
                            await Task.Yield();
                        },
                        async er =>
                        {
                            await Task.Yield();
                        })
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(exception);
            }
        }
    }
}
