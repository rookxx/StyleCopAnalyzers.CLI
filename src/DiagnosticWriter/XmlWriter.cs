#nullable disable
#pragma warning disable CA1051, CA1034

namespace StyleCopAnalyzers.CLI;

using System;
using Microsoft.CodeAnalysis;
using System.Xml.Serialization;
using System.Linq;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

public class XmlWriter : IDiagnosticWriter
{
    public class StyleCopViolations
    {
        public class Violation
        {
            [XmlAttribute] public string Section = string.Empty;
            [XmlAttribute] public int LineNumber;
            [XmlAttribute] public string Source = string.Empty;
            [XmlAttribute] public string RuleNamespace = string.Empty;
            [XmlAttribute] public string Rule = string.Empty;
            [XmlAttribute] public string RuleId = string.Empty;
            public string Message = string.Empty;

            public Violation() { }

            public Violation(Diagnostic diagnostic)
            {
                if (diagnostic == null) { throw new ArgumentException(); }
                Rule = diagnostic.Id;
                Message = diagnostic.GetMessage();
                LineNumber = diagnostic.Location.GetLineSpan().Span.Start.Line + 1;
                Source = diagnostic.Location.GetLineSpan().Path;
                RuleNamespace = diagnostic.Descriptor.Category;

                var classDeclarationSyntax = diagnostic.Location.SourceTree!.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (!SyntaxNodeHelper.TryGetParentSyntax(classDeclarationSyntax, out NamespaceDeclarationSyntax namespaceDeclarationSyntax))
                {
                    return;
                }

                var namespaceName = namespaceDeclarationSyntax.Name.ToString();
                Section = namespaceName + "." + classDeclarationSyntax.Identifier.ToString();
            }
        }

        public Violation[] Violations = Array.Empty<Violation>();

        public StyleCopViolations() { }

        public StyleCopViolations(ImmutableArray<Diagnostic> diagnostics)
        {
            Violations = diagnostics.Select(d => new Violation(d)).ToArray();
        }
    }

    void IDiagnosticWriter.Write(ImmutableArray<Diagnostic> diagnostics)
    {
        var serializer = new XmlSerializer(typeof(StyleCopViolations));
        using var sw = new StreamWriter(Console.OpenStandardOutput())
        {
            AutoFlush = true
        };
        Console.SetOut(sw);

        serializer.Serialize(sw, new StyleCopViolations(diagnostics));
    }

    private static class SyntaxNodeHelper
    {
        public static bool TryGetParentSyntax<T>(SyntaxNode syntaxNode, out T result)
            where T : SyntaxNode
        {
            result = null;
            if (syntaxNode == null) { return false; }

            try
            {
                syntaxNode = syntaxNode.Parent!;
                if (syntaxNode == null) { return false; }

                if (syntaxNode.GetType() == typeof(T))
                {
                    result = syntaxNode as T;
                    return true;
                }

                return TryGetParentSyntax<T>(syntaxNode, out result);
            }
            catch
            {
                return false;
            }
        }
    }
}

#pragma warning restore CA1051, CA1034
#nullable enable