namespace StyleCopAnalyzersCmd
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;

    public interface IProjectReader
    {
        ImmutableArray<Project> ReadAllSourceCodeFiles(string path, string stylecopJsonFile);
    }
}