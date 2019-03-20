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
                            await style.FixCode(cancellationTokenSource.Token).ConfigureAwait(false);
                        },
                        async er =>
                        {
                            await Task.Yield();
                        })
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                Console.WriteLine(exception.StackTrace);
            }
        }
    }
}
