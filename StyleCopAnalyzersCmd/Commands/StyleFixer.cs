
namespace StyleCopAnalyzersCmd
{
    using CommandLine;

    [Verb("fix", HelpText = "Check C# Coding Style")]
    public class StyleFixer
    {
        [Option('r', "ruleset", Required = false, HelpText = "Ruleset file path.")]
        public string RuleSetFilePath { get; set; }
        [Option('s', "stylcopjson", Required = false, HelpText = "stylecop.json file path")]
        public string StyleCopJsonFilePath { get; set; }
        [Value(0, MetaName = "sln/csproj file path or directory path")]
        public string TargetFileOrDirectory { get; set; }


    }
}