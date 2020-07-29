namespace StyleCopAnalyzers.CLI
{
    using System;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;

    public struct FixedDocumentContext : IEquatable<FixedDocumentContext>
    {
        public readonly Document Document;
        public readonly SingleDiagnosticCodeFixer.FixedResult FixedResult;

        public FixedDocumentContext(Document document, SingleDiagnosticCodeFixer.FixedResult fixedResult)
        {
            Document = document;
            FixedResult = fixedResult;
        }

        public override bool Equals(object obj)
        {
            return obj is FixedDocumentContext context &&
                   EqualityComparer<Document>.Default.Equals(Document, context.Document) &&
                   FixedResult.Equals(context.FixedResult);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Document, FixedResult);
        }

        public static bool operator ==(FixedDocumentContext left, FixedDocumentContext right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FixedDocumentContext left, FixedDocumentContext right)
        {
            return !(left == right);
        }

        public bool Equals(FixedDocumentContext other)
        {
            return this.Equals(other);
        }
    }
}