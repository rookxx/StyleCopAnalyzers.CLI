namespace StyleCopAnalyzersCmd
{
    using System;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    public class ConsoleWriter : IDiagnosticWriter
    {
        void IDiagnosticWriter.Write(List<Diagnostic> diagnostics)
        {
            foreach (var d in diagnostics)
            {
                Console.WriteLine($"{d.Id} : {d.Location.GetLineSpan().Path} : {d.Location.GetLineSpan().Span.Start.Line + 1}: {d.GetMessage()}");
            }
        }
    }
}