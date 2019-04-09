namespace StyleCopAnalyzers.CLI
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using CommandLine;

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

            var logger = new SimpleConsoleLogger() as ILogger;
            try
            {
                await Parser.Default.ParseArguments<StyleChecker, StyleFixer>(args)
                    .MapResult(
                        async (StyleChecker style) =>
                        {
                            style.SetLogger(logger);
                            await style.Check(cancellationTokenSource.Token).ConfigureAwait(false);
                        },
                        async (StyleFixer style) =>
                        {
                            style.SetLogger(logger);
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
                logger.LogError(exception.Message);
                logger.LogError(exception.StackTrace);
            }
        }
    }
}
