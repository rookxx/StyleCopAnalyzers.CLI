namespace StyleCopAnalyzers.CLI;

public enum OutputKind
{
    Undefined,
    RawText,
    LegacyStyleCopXml,
}

public static class OutputKindExtensions
{
    public static IDiagnosticWriter ToWriter(this OutputKind kind)
    {
        return kind switch
        {
            OutputKind.RawText => new ConsoleWriter(),
            OutputKind.LegacyStyleCopXml => new XmlWriter(),
            _ => throw new System.ArgumentException($"Undefined outputKind [{kind}]"),
        };
    }
}

public static class OutputKindHelper
{
    public static OutputKind ToOutputKind(string kindString)
    {
        return kindString switch
        {
            "text" => OutputKind.RawText,
            "xml" => OutputKind.LegacyStyleCopXml,
            _ => OutputKind.Undefined,
        };
    }
}
