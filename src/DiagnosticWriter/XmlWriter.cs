#pragma warning disable CA1051
#pragma warning disable CA1034

namespace StyleCopAnalyzers.CLI
{
    using System;
    using Microsoft.CodeAnalysis;
    using System.Xml.Serialization;
    using System.Linq;
    using System.IO;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public class XmlWriter : IDiagnosticWriter
    {
        public class StyleCopViolations
        {
            public class Violation
            {
                [XmlAttribute] public string Section;
                [XmlAttribute] public int LineNumber;
                [XmlAttribute] public string Source;
                [XmlAttribute] public string RuleNamespace;
                [XmlAttribute] public string Rule;
                [XmlAttribute] public string RuleId;
                public string Message;

                public Violation() { }

                public Violation(Diagnostic diagnostic)
                {
                    Rule = diagnostic.Id;
                    Message = diagnostic.GetMessage();
                    LineNumber = diagnostic.Location.GetLineSpan().Span.Start.Line + 1;
                    Source = diagnostic.Location.GetLineSpan().Path;
                    RuleNamespace = diagnostic.Descriptor.Category;

                    var classDeclarationSyntax = diagnostic.Location.SourceTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                    NamespaceDeclarationSyntax namespaceDeclarationSyntax = null;
                    if (!SyntaxNodeHelper.TryGetParentSyntax(classDeclarationSyntax, out namespaceDeclarationSyntax))
                    {
                        return;
                    }

                    var namespaceName = namespaceDeclarationSyntax.Name.ToString();
                    var fullClassName = namespaceName + "." + classDeclarationSyntax.Identifier.ToString();
                    Section = fullClassName;
                }
            }

            public Violation[] Violations;

            public StyleCopViolations() { }

            public StyleCopViolations(ImmutableArray<Diagnostic> diagnostics)
            {
                Violations = diagnostics.Select(d => new Violation(d)).ToArray();
            }
        }

        void IDiagnosticWriter.Write(ImmutableArray<Diagnostic> diagnostics)
        {
            var serializer = new XmlSerializer(typeof(StyleCopViolations));
            var sw = new StreamWriter(Console.OpenStandardOutput());
            sw.AutoFlush = true;
            Console.SetOut(sw);

            serializer.Serialize(sw, new StyleCopViolations(diagnostics));
        }

        static class SyntaxNodeHelper
        {
            public static bool TryGetParentSyntax<T>(SyntaxNode syntaxNode, out T result)
                where T : SyntaxNode
            {
                result = null;
                if (syntaxNode == null) { return false; }

                try
                {
                    syntaxNode = syntaxNode.Parent;
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
}
#pragma warning restore CA1051
#pragma warning disable CA1034