# StyleCopAnalyzers.CLI

## Overview
StyleCop.Analyzers CLI tool

## Description
`StyleCopAnalyzers.CLI` is a tool to check C# coding style with CLI, commit-hook, etc.

## Requirement
- .NET Core SDK or Runtime ver3.1 or higher.
[.NET Core](https://dotnet.microsoft.com/download)

## Usage
Execute by specifying solution file, csproj file and directory.

- Check in the default ruleset of StyleCop.Analyzers
```
dotnet ./StyleCopAnalyzers.CLI.dll check [./ExampleDir or ./Example.sln or ./Example.csproj]
```

- Specify a StyleCop.Analyzers ruleset
```
dotnet ./StyleCopAnalyzers.CLI.dll check -r stylecop.ruleset  [./ExampleDir or ./Example.sln or ./Example.csproj]
```

- Specify a stylecop.json
```
dotnet ./StyleCopAnalyzers.CLI.dll check -j stylecop.json  [./ExampleDir or ./Example.sln or ./Example.csproj]
```

- Both ruleset and json can be specified
```
dotnet ./StyleCopAnalyzers.CLI.dll check -j stylecop.json -r stylecop.ruleset [./ExampleDir or ./Example.sln or ./Example.csproj]
```
- Fix Style
```
dotnet ./StyleCopAnalyzers.CLI.dll fix -j stylecop.json -r stylecop.ruleset [./ExampleDir or ./Example.sln or ./Example.csproj]
```

## StyleCheck Running Example
- Text
```
# dotnet ./StyleCopAnalyzers.CLI.dll check ./TestCSharpCodes/
SA1402 : StyleCopAnalyzers.CLI/DiagnosticWriter/OutputKind.cs : 23: File may only contain a single type
SA1633 : StyleCopAnalyzers.CLI/DiagnosticWriter/OutputKind.cs : 1: The file header is missing or not located at the top of the file.
SA1649 : StyleCopAnalyzers.CLI/DiagnosticWriter/OutputKind.cs : 10: File name should match first type name.
SA1633 : StyleCopAnalyzers.CLI/DiagnosticWriter/IDiagnosticWriter.cs : 1: The file header is missing or not located at the top of the file.
SA1633 : StyleCopAnalyzers.CLI/Extensions.cs : 1: The file header XML is invalid.
SA1633 : StyleCopAnalyzers.CLI/DiagnosticWriter/ConsoleWriter.cs : 1: The file header is missing or not located at the top of the file.
SA1633 : StyleCopAnalyzers.CLI/DiagnosticWriter/XmlWriter.cs : 1: The file header is missing or not located at the top of the file.
SA1101 : StyleCopAnalyzers.CLI/DiagnosticWriter/XmlWriter.cs : 32: Prefix local calls with this
SA1101 : StyleCopAnalyzers.CLI/DiagnosticWriter/XmlWriter.cs : 33: Prefix local calls with this
```

- StyleCop Xml format
```
# dotnet ./StyleCopAnalyzers.CLI.dll check ./TestCSharpCodes/ -f xml
<?xml version="1.0" encoding="utf-8"?>
<StyleCopViolations xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <Violations>
    <Violation Section="StyleCopAnalyzers.CLI.OutputKindExtensions" LineNumber="23" Source=StyleCopAnalyzers.CLI/DiagnosticWriter/OutputKind.cs" RuleNamespace="StyleCop.CSharp.MaintainabilityRules" Rule="SA1402">
      <Message>File may only contain a single type</Message>
    </Violation>
    <Violation Section="StyleCopAnalyzers.CLI.OutputKindExtensions" LineNumber="1" Source=StyleCopAnalyzers.CLI/DiagnosticWriter/OutputKind.cs" RuleNamespace="StyleCop.CSharp.DocumentationRules" Rule="SA1633">
      <Message>The file header is missing or not located at the top of the file.</Message>
    </Violation>
    <Violation Section="StyleCopAnalyzers.CLI.OutputKindExtensions" LineNumber="10" Source=StyleCopAnalyzers.CLI/DiagnosticWriter/OutputKind.cs" RuleNamespace="StyleCop.CSharp.DocumentationRules" Rule="SA1649">
      <Message>File name should match first type name.</Message>
    </Violation>
    <Violation LineNumber="1" Source=StyleCopAnalyzers.CLI/DiagnosticWriter/IDiagnosticWriter.cs" RuleNamespace="StyleCop.CSharp.DocumentationRules" Rule="SA1633">
      <Message>The file header is missing or not located at the top of the file.</Message>
    </Violation>
    <Violation Section="StyleCopTester.Extensions" LineNumber="1" Source=StyleCopAnalyzers.CLI/Extensions.cs" RuleNamespace="StyleCop.CSharp.DocumentationRules" Rule="SA1633">
      <Message>The file header XML is invalid.</Message>
    </Violation>
```

## Style Fix Running Example
### CLI
![CLI Example](https://github.com/rookxx/StyleCopAnalyzers.CLI/wiki/fix.gif)

### Source Code difference
![Diff1](https://github.com/rookxx/StyleCopAnalyzers.CLI/wiki/fixexample1.png)
![Diff2](https://github.com/rookxx/StyleCopAnalyzers.CLI/wiki/fixexample2.png)
![Diff3](https://github.com/rookxx/StyleCopAnalyzers.CLI/wiki/fixexample3.png)

## License

[Apache License](https://github.com/rookxx/StyleCopAnalyzers.CLI/blob/master/LICENSE)

## Author

[rookxx](https://github.com/rookxx)

