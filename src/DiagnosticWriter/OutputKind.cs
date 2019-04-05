namespace StyleCopAnalyzers.CLI
{
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
            switch (kind)
            {
                case OutputKind.RawText: return new ConsoleWriter();
                case OutputKind.LegacyStyleCopXml: return new XmlWriter();
                default: throw new System.ArgumentException($"Undefined outputKind [kind]");
            }
        }
    }

    public static class OutputKindHelper
    {
        public static OutputKind ToOutputKind(string kindString)
        {
            switch (kindString)
            {
                case "text": return OutputKind.RawText;
                case "xml": return OutputKind.LegacyStyleCopXml;
                default: return OutputKind.Undefined;
            }
        }
    }
}