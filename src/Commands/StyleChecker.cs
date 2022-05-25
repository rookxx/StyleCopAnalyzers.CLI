namespace StyleCopAnalyzers.CLI;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using CommandLine;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

[Verb("check", HelpText = "Check C# Coding Style")]
public class StyleChecker
{
    [Option('r', "ruleset", Required = false, HelpText = "Ruleset file path.")]
    public string RuleSetFilePath { get; set; } = string.Empty;
    [Option('j', "json", Required = false, HelpText = "stylecop.json file path")]
    public string StyleCopJsonFilePath { get; set; } = string.Empty;
    [Option('f', "format", Required = false, Default = "text", HelpText = "output format\n    text raw text\n    xml  legacy stylecop xml format")]
    public string OutputFormat { get; set; } = string.Empty;
    [Value(0, MetaName = "sln/csproj file path, directory path or file path")]
    public IEnumerable<string> Targets { get; set; }

    private ILogger logger = new SilentLogger();

    public StyleChecker()
    {
        Targets = Array.Empty<string>();
    }

    public void SetLogger(ILogger logger)
    {
        this.logger = logger;
    }

    public async Task Check(CancellationToken cancellationToken)
    {
        RuleSetFilePath = CommandHelper.GetAbsoluteOrDefaultFilePath(RuleSetFilePath, "./stylecop.ruleset");
        StyleCopJsonFilePath = CommandHelper.GetAbsoluteOrDefaultFilePath(StyleCopJsonFilePath, "./stylecop.json");

        if (!Targets.Any())
        {
            return;
        }

        this.logger.LogDebug("Arguments ============================");
        this.logger.LogDebug($"ruleset : {RuleSetFilePath}");
        this.logger.LogDebug($"stylecop.json : {RuleSetFilePath}");
        this.logger.LogDebug($"format : {OutputFormat}");
        this.logger.LogDebug($"check : \n{string.Join("\n", Targets)}");
        this.logger.LogDebug("======================================");

        var projects = ImmutableArray.CreateBuilder<Project>();
        foreach (var target in Targets)
        {
            var targetFileOrDirectory = CommandHelper.GetAbsolutePath(target);

            var inputKind = CommandHelper.GetInputKindFromFileOrDirectory(targetFileOrDirectory);
            if (!inputKind.HasValue) { return; }

            var readableProjects = inputKind.Value.ToReader().ReadAllSourceCodeFiles(targetFileOrDirectory, StyleCopJsonFilePath);
            if (readableProjects.Length == 0) { return; }

            projects.AddRange(readableProjects);
        }

        var outputKind = OutputKindHelper.ToOutputKind(OutputFormat);
        if (outputKind == OutputKind.Undefined)
        {
            Console.Error.WriteLine($"output format is undefined. -f {OutputFormat}");
            return;
        }

        var analyzerLoader = new AnalyzerLoader(RuleSetFilePath);
        var analyzers = analyzerLoader.GetAnalyzers();
        var diagnostics = await CommandHelper.GetAnalyzerDiagnosticsAsync(
            projects.ToImmutable(),
            analyzers,
            analyzerLoader.RuleSets,
            cancellationToken).ConfigureAwait(false);

        var writer = outputKind.ToWriter();
        writer.Write(diagnostics);
    }
}
