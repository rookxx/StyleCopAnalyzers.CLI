namespace StyleCopAnalyzersCmd
{
    using System;
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;

    public class ConsoleWriter : IDiagnosticWriter
    {
        void IDiagnosticWriter.Write(ImmutableArray<Diagnostic> diagnostics)
        {
            foreach (var d in diagnostics)
            {
                Console.WriteLine($"{d.Id} : {d.Location.GetLineSpan().Path} : {d.Location.GetLineSpan().Span.Start.Line + 1}: {d.GetMessage()}");
            }
        }
    }
}