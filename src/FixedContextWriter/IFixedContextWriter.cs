namespace StyleCopAnalyzers.CLI;

public interface IFixedContextWriter
{
    void SetLogger(ILogger logger);
    void Write(FixedDocumentContext fixedContext);
}
