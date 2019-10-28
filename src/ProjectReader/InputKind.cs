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
            switch (kind)
            {
                case InputKind.Directory: return new DirectoryFileReader();
                case InputKind.Csproj: return new CSProjectReader();
                case InputKind.Sln: return new SolutionReader();
                case InputKind.CSharpFile : return new CSharpFileReader();
                default: throw new System.ArgumentException($"Undefined inputKind [kind]");
            }
        }
    }
}