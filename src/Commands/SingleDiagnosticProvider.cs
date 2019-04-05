namespace StyleCopAnalyzers.CLI
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;

    /// <summary>
    /// Supports only one Diagnostic Id
    /// </summary>
    public class DocumentSingleDiagnotsticProvider : FixAllContext.DiagnosticProvider
    {
        private ImmutableArray<Diagnostic> diagnostics;
        private List<CodeAction> codeActions;

        public DocumentSingleDiagnotsticProvider(ImmutableArray<Diagnostic> diagnostics)
        {
            if (diagnostics.Length <= 0 ||
                diagnostics.Where(d => d.Id != diagnostics[0].Id).Count() != 0)
            {
                throw new System.ArgumentException();
            }

            this.diagnostics = diagnostics.Where(d => d.Location.IsInSource).ToImmutableArray();
        }

        public async Task<string> GetEquivalenceKeyAsync(CodeFixProvider codeFixProvider, Document document, CancellationToken cancellationToken)
        {
            this.codeActions = new List<CodeAction>();
            await codeFixProvider.RegisterCodeFixesAsync(new CodeFixContext(document, this.diagnostics[0], (a, d) => codeActions.Add(a), cancellationToken)).ConfigureAwait(false);

            return codeActions.Count > 0 ? codeActions[0].EquivalenceKey : string.Empty;
        }

        public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            return Task.FromResult(diagnostics.Where(d => d.Location.SourceTree.FilePath == document.FilePath));
        }

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            return Task.FromResult(Enumerable.Empty<Diagnostic>());
        }

        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            return Task.FromResult(this.diagnostics.AsEnumerable());
        }
    }
}