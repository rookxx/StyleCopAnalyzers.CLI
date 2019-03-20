

namespace StyleCopAnalyzersCmd
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

    public class SingleDiagnosticCodeFixer
    {
        private Document document;
        private ImmutableArray<Diagnostic> diagnostics;
        private CodeFixProvider codeFixProvider;

        public string DiagnosticId => diagnostics[0].Id;
        public IEnumerable<string> DiagnosticIds => new List<string> { diagnostics[0].Id };

        public struct FixedResult
        {
            public readonly ImmutableArray<Document> AddedDocuments;
            public readonly ImmutableArray<Document> ChangedDocuments;
            public readonly ImmutableArray<Document> RemovedDocuments;

            public static FixedResult Empty
            {
                get
                {
                    return new FixedResult(ImmutableArray.Create<Document>(), ImmutableArray.Create<Document>(), ImmutableArray.Create<Document>());
                }
            }

            public FixedResult(
                ImmutableArray<Document> addedDocuments,
                ImmutableArray<Document> changedDocuments,
                ImmutableArray<Document> removedDocuments)
            {
                AddedDocuments = addedDocuments;
                ChangedDocuments = changedDocuments;
                RemovedDocuments = removedDocuments;
            }
        }

        public SingleDiagnosticCodeFixer(Document document, ImmutableArray<Diagnostic> diagnostics, CodeFixProvider codeFixProvider)
        {
            if (diagnostics.Length <= 0 ||
                diagnostics.Where(d => d.Id != diagnostics[0].Id).Count() != 0)
            {
                throw new System.ArgumentException();
            }

            this.document = document;
            this.diagnostics = diagnostics;
            this.codeFixProvider = codeFixProvider;
        }

        public async Task<FixedResult> FixCode(CancellationToken cancellationToken)
        {
            var fixAllProvider = this.codeFixProvider.GetFixAllProvider();
            if (fixAllProvider != null)
            {
                return await FixCodeAllDiagnostics(this.codeFixProvider.GetFixAllProvider(), cancellationToken);
            }
            else
            {
                return await FixCodeSingleDiagnostics(cancellationToken);
            }
        }

        private async Task<FixedResult> FixCodeAllDiagnostics(FixAllProvider fixAllProvider, CancellationToken cancellationToken)
        {
            var diagnosticsProvider = new DocumentSingleDiagnotsticProvider(diagnostics);

            var fixAllContext = new FixAllContext(
                document,
                codeFixProvider,
                FixAllScope.Document,
                await diagnosticsProvider.GetEquivalenceKeyAsync(codeFixProvider, document, cancellationToken),
                DiagnosticIds,
                diagnosticsProvider,
                cancellationToken);

            var action = await fixAllProvider.GetFixAsync(fixAllContext).ConfigureAwait(false);
            if (action == null)
            {
                Console.WriteLine($"{DiagnosticId} : Code fix action did not exists : {document.FilePath}");
                return FixedResult.Empty;
            }

            var oldSolution = document.Project.Solution;
            var changedDocument = await ConvertCodeActionToChangedDocument(action, cancellationToken);

            return GetFixedChanges(oldSolution, changedDocument.Project.Solution);
        }

        private async Task<FixedResult> FixCodeSingleDiagnostics(CancellationToken cancellationToken)
        {
            var codeActions = new List<CodeAction>();
            await codeFixProvider.RegisterCodeFixesAsync(new CodeFixContext(document, this.diagnostics[0], (a, d) => codeActions.Add(a), cancellationToken)).ConfigureAwait(false);

            if (codeActions.Count <= 0)
            {
                return FixedResult.Empty;
            }

            var oldSolution = document.Project.Solution;
            var changedDocument = await ConvertCodeActionToChangedDocument(codeActions.First(), cancellationToken);

            return GetFixedChanges(oldSolution, changedDocument.Project.Solution);
        }

        private FixedResult GetFixedChanges(Solution solutionOld, Solution solutionNew)
        {
            var solutionChanges = solutionNew.GetChanges(solutionOld);
            var projectChanges = solutionChanges.GetProjectChanges();
            if (projectChanges.Count() <= 0)
            {
                return FixedResult.Empty;
            }

            Func<DocumentId, Document> getDocumentFromNewSolution = (id) =>
            {
                foreach(var project in solutionNew.Projects)
                {
                    var document = project.GetDocument(id);
                    if (document != null)
                    {
                        return document;
                    }
                }
                return null;
            };

            var changedDocuments = projectChanges
                .SelectMany(p => p.GetChangedDocuments())
                .Select(documentId => getDocumentFromNewSolution(documentId))
                .ToImmutableArray();

            var addedDocuments = projectChanges
                .SelectMany(p => p.GetAddedDocuments())
                .Select(documentId => getDocumentFromNewSolution(documentId))
                .ToImmutableArray();

            var removedDocuments = projectChanges
                .SelectMany(p => p.GetRemovedDocuments())
                .Select(documentId => getDocumentFromNewSolution(documentId))
                .ToImmutableArray();

            return new FixedResult(addedDocuments, changedDocuments, removedDocuments);
        }

        private async Task<Document> ConvertCodeActionToChangedDocument(CodeAction action, CancellationToken cancellationToken)
        {
            var operations = await action.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
            if (operations == null)
            {
                return document;
            }
            if (operations.Count() > 1)
            {
                Console.WriteLine($"{DiagnosticId} : Only single operation is supported. Operation count = {operations.Count()} : {document.FilePath}");
                return document;
            }

            var applyOperation = operations.First() as ApplyChangesOperation;
            if (applyOperation == null)
            {
                return document;
            }

            var changedDocument = applyOperation.ChangedSolution?.Projects?.First()?.GetDocument(document.Id);

            return changedDocument != null ? changedDocument : document;
        }
    }
}