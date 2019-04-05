namespace StyleCopAnalyzers.CLI
{
    public enum InputKind
    {
        Directory,
        Csproj,
        Sln,
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
                default: throw new System.ArgumentException($"Undefined inputKind [kind]");
            }
        }
    }
}