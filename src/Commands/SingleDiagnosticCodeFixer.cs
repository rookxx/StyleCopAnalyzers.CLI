
namespace StyleCopAnalyzers.CLI;

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
    private readonly Document document;
    private readonly CodeFixProvider codeFixProvider;
    private readonly ImmutableArray<Diagnostic> diagnostics;

    public string DiagnosticId => diagnostics[0].Id;
    public IEnumerable<string> DiagnosticIds => new List<string> { diagnostics[0].Id };

    public struct FixedResult : IEquatable<FixedResult>
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

        public override bool Equals(object? obj)
        {
            return obj is FixedResult result && Equals(result);
        }

        public bool Equals(FixedResult other)
        {
            return RemovedDocuments.Equals(other.RemovedDocuments);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RemovedDocuments);
        }

        public static bool operator ==(FixedResult left, FixedResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FixedResult left, FixedResult right)
        {
            return !(left == right);
        }
    }

    public SingleDiagnosticCodeFixer(Document document, ImmutableArray<Diagnostic> diagnostics, CodeFixProvider codeFixProvider)
    {
        if (diagnostics.IsDefaultOrEmpty ||
            diagnostics.Any(d => d.Id != diagnostics[0].Id))
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
            return await FixCodeAllDiagnostics(fixAllProvider, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            return await FixCodeSingleDiagnostics(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<FixedResult> FixCodeAllDiagnostics(FixAllProvider fixAllProvider, CancellationToken cancellationToken)
    {
        var diagnosticsProvider = new DocumentSingleDiagnotsticProvider(diagnostics);

        var fixAllContext = new FixAllContext(
            document,
            codeFixProvider,
            FixAllScope.Document,
            await diagnosticsProvider.GetEquivalenceKeyAsync(codeFixProvider, document, cancellationToken).ConfigureAwait(false),
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
        var changedDocument = await ConvertCodeActionToChangedDocument(action, cancellationToken).ConfigureAwait(false);

        return GetFixedChanges(oldSolution, changedDocument.Project.Solution);
    }

    private async Task<FixedResult> FixCodeSingleDiagnostics(CancellationToken cancellationToken)
    {
        var codeActions = new List<CodeAction>();
        await codeFixProvider.RegisterCodeFixesAsync(new CodeFixContext(document, this.diagnostics[0], (a, _) => codeActions.Add(a), cancellationToken)).ConfigureAwait(false);

        if (codeActions.Count == 0)
        {
            return FixedResult.Empty;
        }

        var oldSolution = document.Project.Solution;
        var changedDocument = await ConvertCodeActionToChangedDocument(codeActions[0], cancellationToken).ConfigureAwait(false);

        return GetFixedChanges(oldSolution, changedDocument.Project.Solution);
    }

    private FixedResult GetFixedChanges(Solution solutionOld, Solution solutionNew)
    {
        var solutionChanges = solutionNew.GetChanges(solutionOld);
        var projectChanges = solutionChanges.GetProjectChanges();
        if (!projectChanges.Any())
        {
            return FixedResult.Empty;
        }

        Func<DocumentId, Document> getDocumentFromNewSolution = (id) =>
        {
            foreach (var project in solutionNew.Projects)
            {
                var document = project.GetDocument(id);
                if (document != null)
                {
                    return document;
                }
            }
            throw new System.ArgumentException("getDocumentFromNewSolution");
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
        if (operations.Length > 1)
        {
            Console.WriteLine($"{DiagnosticId} : Only single operation is supported. Operation count = {operations.Length} : {document.FilePath}");
            return document;
        }

        if (!(operations[0] is ApplyChangesOperation applyOperation))
        {
            return document;
        }

        var changedDocument = applyOperation.ChangedSolution?.Projects?.First()?.GetDocument(document.Id);

        return changedDocument ?? document;
    }
}