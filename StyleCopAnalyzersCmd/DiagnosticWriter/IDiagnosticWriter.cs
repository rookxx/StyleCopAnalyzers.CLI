namespace StyleCopAnalyzersCmd
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;

    public interface IDiagnosticWriter
    {
        void Write(ImmutableArray<Diagnostic> diagnostics);
    }
}