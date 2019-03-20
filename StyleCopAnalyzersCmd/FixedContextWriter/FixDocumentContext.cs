namespace StyleCopAnalyzersCmd
{
    using Microsoft.CodeAnalysis;

    public struct FixedDocumentContext
    {
        public readonly Document Document;
        public readonly SingleDiagnosticCodeFixer.FixedResult FixedResult;

        public FixedDocumentContext(Document document, SingleDiagnosticCodeFixer.FixedResult fixedResult)
        {
            Document = document;
            FixedResult = fixedResult;
        }
    }
}