namespace StyleCopAnalyzersCmd
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    public interface IDiagnosticWriter
    {
        void Write(List<Diagnostic> diagnostics);
    }
}