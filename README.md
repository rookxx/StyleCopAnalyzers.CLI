# StyleCopAnalyzersCmd

## Overview
StyleCop.Analyzers CLI tool

## Description
`StyleCopAnalyzersCmd` is a tool to check C# coding style with CLI, commit-hook, etc.

## WIP
Automatic code fix.

## Usage
Execute by specifying solution file, csproj file and directory.

- Check in the default ruleset of StyleCop.Analyzers  
```
dotnet ./StyleCopAnalyzersCmd.dll check [./ExampleDir or ./Example.sln or ./Example.csproj]
```

- Specify a StyleCop.Analyzers ruleset 
```
dotnet ./StyleCopAnalyzersCmd.dll check -r stylecop.ruleset  [./ExampleDir or ./Example.sln or ./Example.csproj]
```

- Specify a stylecop.json
```
dotnet ./StyleCopAnalyzersCmd.dll check -s stylecop.json  [./ExampleDir or ./Example.sln or ./Example.csproj]
```

- Both ruleset and json can be specified  
```
dotnet ./StyleCopAnalyzersCmd.dll check -s stylecop.json -r stylecop.ruleset [./ExampleDir or ./Example.sln or ./Example.csproj]
```
- fix command is WIP

## Running Example
- Text
```
# dotnet ./StyleCopAnalyzersCmd.dll check ./TestCSharpCodes/
SA1402 : StyleCopAnalyzersCmd/DiagnosticWriter/OutputKind.cs : 23: File may only contain a single type
SA1633 : StyleCopAnalyzersCmd/DiagnosticWriter/OutputKind.cs : 1: The file header is missing or not located at the top of the file.
SA1649 : StyleCopAnalyzersCmd/DiagnosticWriter/OutputKind.cs : 10: File name should match first type name.
SA1633 : StyleCopAnalyzersCmd/DiagnosticWriter/IDiagnosticWriter.cs : 1: The file header is missing or not located at the top of the file.
SA1633 : StyleCopAnalyzersCmd/Extensions.cs : 1: The file header XML is invalid.
SA1633 : StyleCopAnalyzersCmd/DiagnosticWriter/ConsoleWriter.cs : 1: The file header is missing or not located at the top of the file.
SA1633 : StyleCopAnalyzersCmd/DiagnosticWriter/XmlWriter.cs : 1: The file header is missing or not located at the top of the file.
SA1101 : StyleCopAnalyzersCmd/DiagnosticWriter/XmlWriter.cs : 32: Prefix local calls with this
SA1101 : StyleCopAnalyzersCmd/DiagnosticWriter/XmlWriter.cs : 33: Prefix local calls with this
```

- StyleCop Xml format
```
# dotnet ./StyleCopAnalyzersCmd.dll check ./TestCSharpCodes/ -f xml
<?xml version="1.0" encoding="utf-8"?>
<StyleCopViolations xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <Violations>
    <Violation Section="StyleCopAnalyzersCmd.OutputKindExtensions" LineNumber="23" Source=StyleCopAnalyzersCmd/DiagnosticWriter/OutputKind.cs" RuleNamespace="StyleCop.CSharp.MaintainabilityRules" Rule="SA1402">
      <Message>File may only contain a single type</Message>
    </Violation>
    <Violation Section="StyleCopAnalyzersCmd.OutputKindExtensions" LineNumber="1" Source=StyleCopAnalyzersCmd/DiagnosticWriter/OutputKind.cs" RuleNamespace="StyleCop.CSharp.DocumentationRules" Rule="SA1633">
      <Message>The file header is missing or not located at the top of the file.</Message>
    </Violation>
    <Violation Section="StyleCopAnalyzersCmd.OutputKindExtensions" LineNumber="10" Source=StyleCopAnalyzersCmd/DiagnosticWriter/OutputKind.cs" RuleNamespace="StyleCop.CSharp.DocumentationRules" Rule="SA1649">
      <Message>File name should match first type name.</Message>
    </Violation>
    <Violation LineNumber="1" Source=StyleCopAnalyzersCmd/DiagnosticWriter/IDiagnosticWriter.cs" RuleNamespace="StyleCop.CSharp.DocumentationRules" Rule="SA1633">
      <Message>The file header is missing or not located at the top of the file.</Message>
    </Violation>
    <Violation Section="StyleCopTester.Extensions" LineNumber="1" Source=StyleCopAnalyzersCmd/Extensions.cs" RuleNamespace="StyleCop.CSharp.DocumentationRules" Rule="SA1633">
      <Message>The file header XML is invalid.</Message>
    </Violation>
```

## Requirement
- .NET Core SDK or Runtime ver2.1 or higher.  
[.NET Core](https://dotnet.microsoft.com/download)

## License

[Apache License](https://github.com/rookxx/StyleCopAnalyzersCmd/blob/master/LICENSE)

## Author

[rookxx](https://github.com/rookxx)

