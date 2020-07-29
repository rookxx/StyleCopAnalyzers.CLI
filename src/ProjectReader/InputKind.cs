namespace StyleCopAnalyzers.CLI
{
    public enum InputKind
    {
        Directory,
        Csproj,
        Sln,
        CSharpFile,
    }

    public static class InputKindExtensions
    {
        public static IProjectReader ToReader(this InputKind kind)
        {
            return kind switch
            {
                InputKind.Directory => new DirectoryFileReader(),
                InputKind.Csproj => new CSProjectReader(),
                InputKind.Sln => new SolutionReader(),
                InputKind.CSharpFile => new CSharpFileReader(),
                _ => throw new System.ArgumentException($"Undefined inputKind [{kind}]"),
            };
        }
    }
}