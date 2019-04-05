namespace StyleCopAnalyzers.CLI
{
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;

    public interface IFixedContextWriter
    {
        void SetLogger(ILogger logger);
        void Write(FixedDocumentContext fixedContext);
    }
}