namespace StyleCopAnalyzers.CLI
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Loader;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.Diagnostics;

    public class AnalyzerLoader
    {
        private const string StyleCopAnalyzersDll = "StyleCop.Analyzers";
        private const string StyleCopAnalyzersCodeFixesDll = "StyleCop.Analyzers.CodeFixes";

        private readonly Dictionary<string, ReportDiagnostic> rulesets = new Dictionary<string, ReportDiagnostic>();

        public AnalyzerLoader(string ruleSetFilePath)
        {
            if (File.Exists(ruleSetFilePath))
            {
                RuleSet.GetDiagnosticOptionsFromRulesetFile(ruleSetFilePath, out rulesets);
            }

            rulesets.Add("AD0001", ReportDiagnostic.Error);
        }

        public ImmutableArray<DiagnosticAnalyzer> GetAnalyzers()
        {
            var name = new AssemblyName(StyleCopAnalyzersDll);
            var stylecop = AssemblyLoadContext.Default.LoadFromAssemblyName(name);
            var assembly = stylecop.GetType("StyleCop.Analyzers.NoCodeFixAttribute")?.Assembly;
            if (assembly == null) { return default; }

            var diagnosticAnalyzerType = typeof(DiagnosticAnalyzer);

            var analyzers = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();

            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsSubclassOf(diagnosticAnalyzerType) || type.IsAbstract)
                {
                    continue;
                }
                if (!(Activator.CreateInstance(type) is DiagnosticAnalyzer analyzer))
                {
                    continue;
                }
                if (!IsValidAnalyzer(analyzer, rulesets))
                {
                    continue;
                }

                analyzers.Add(analyzer!);
            }

            return analyzers.ToImmutable();
        }

        public ImmutableArray<CodeFixProvider> GetCodeFixProviders()
        {
            var name = new AssemblyName(StyleCopAnalyzersCodeFixesDll);
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(name);

            var codeFixProviderType = typeof(CodeFixProvider);

            var providers = ImmutableArray.CreateBuilder<CodeFixProvider>();

            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsSubclassOf(codeFixProviderType) || type.IsAbstract)
                {
                    continue;
                }

                if (!(Activator.CreateInstance(type) is CodeFixProvider codeFixProvider))
                {
                    continue;
                }

                if (!IsValidCodeFixProvider(codeFixProvider, rulesets))
                {
                    continue;
                }

                providers.Add(codeFixProvider);
            }

            return providers.ToImmutableArray();
        }

        private bool IsValidAnalyzer(DiagnosticAnalyzer analyzer, Dictionary<string, ReportDiagnostic> rulesets)
        {
            foreach (var diagnostic in analyzer.SupportedDiagnostics)
            {
                if (!IsValidRule(diagnostic.Id, rulesets))
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsValidCodeFixProvider(CodeFixProvider codeFixProvider, Dictionary<string, ReportDiagnostic> rulesets)
        {
            foreach (var diagnosticId in codeFixProvider.FixableDiagnosticIds)
            {
                if (!IsValidRule(diagnosticId, rulesets))
                {
                    return false;
                }
            }
            return true;
        }

        private bool IsValidRule(string diagnosticId, Dictionary<string, ReportDiagnostic> rulesets)
        {
            if (rulesets.ContainsKey(diagnosticId))
            {
                if (rulesets[diagnosticId] == ReportDiagnostic.Suppress ||
                    rulesets[diagnosticId] == ReportDiagnostic.Hidden)
                {
                    return false;
                }
            }
            return true;
        }
    }
}