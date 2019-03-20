
namespace StyleCopAnalyzersCmd
{
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    public static class CommandHelper
    {
        public static string GetAbsolutePath(string path)
        {
            return Path.GetFullPath(path);
        }

        public static string GetAbsoluteOrDefaultFilePath(string path, string defaultPath)
        {
            if (File.Exists(path))
            {
                return Path.GetFullPath(path);
            }
            else
            {
                return Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, defaultPath);
            }
        }

        public static async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(
                ImmutableArray<Project> projects,
                ImmutableArray<DiagnosticAnalyzer> analyzers,
                CancellationToken cancellationToken)
        {
            var diagnosticsAll = ImmutableArray.CreateBuilder<Diagnostic>();
            foreach (var project in projects)
            {
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers, project.AnalyzerOptions);
                var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken).ConfigureAwait(false);

                diagnosticsAll.AddRange(diagnostics);
            }

            return diagnosticsAll.ToImmutableArray();
        }

        public static InputKind? GetInputKindFromFileOrDirectory(string targetFileOrDirectory)
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
}