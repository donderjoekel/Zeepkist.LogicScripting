using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TNRD.Zeepkist.LogicScripting.Analyzers;

/// <summary>
/// Reports a nested type that isn't in its own partial file of the containing type, named
/// <c>&lt;Outer&gt;.&lt;Nested&gt;.cs</c> (<c>GraphBuilder.Resolved.cs</c>), so every type has a file of its own. A partial
/// nested type may also be split across <c>&lt;Outer&gt;.&lt;Nested&gt;.&lt;Part&gt;.cs</c> (<c>ScriptWindow.Driver.Orphans.cs</c>).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NestedTypeFileAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "LS0001";

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        "Nested type belongs in its own partial file",
        "Move nested type '{0}' to a partial file named '{1}'",
        "Maintainability",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeType,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.InterfaceDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration,
            SyntaxKind.EnumDeclaration,
            SyntaxKind.DelegateDeclaration);
    }

    private static void AnalyzeType(SyntaxNodeAnalysisContext context)
    {
        if (context.Node.Parent is not TypeDeclarationSyntax)
            return;

        var expected = GetFileName(context.Node);
        var actual = Path.GetFileName(context.Node.SyntaxTree.FilePath);
        if (actual == expected || IsPartOfPartialType(context.Node, actual) || IsPartialContainerOfFileType(context.Node, actual))
            return;

        var identifier = GetIdentifier(context.Node);
        context.ReportDiagnostic(Diagnostic.Create(_rule, identifier.GetLocation(), identifier.ValueText, expected));
    }

    /// <summary>
    /// Whether the type is a partial declaration in one of its own part files, named
    /// <c>&lt;Outer&gt;.&lt;Nested&gt;.&lt;Part&gt;.cs</c>: <c>partial class Driver</c> in <c>ScriptWindow.Driver.Orphans.cs</c>.
    /// </summary>
    private static bool IsPartOfPartialType(SyntaxNode type, string fileName)
    {
        if (type is not TypeDeclarationSyntax declaration || !declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
            return false;

        var prefix = GetQualifiedName(type) + ".";
        if (!fileName.StartsWith(prefix, StringComparison.Ordinal) || !fileName.EndsWith(".cs", StringComparison.Ordinal))
            return false;

        var part = fileName.Substring(prefix.Length, fileName.Length - prefix.Length - ".cs".Length);
        return part.Length > 0 && part.IndexOf('.') < 0;
    }

    /// <summary>
    /// Whether the type is a partial declaration containing the type the file is named for:
    /// <c>partial class Middle</c> in <c>Outer.Middle.Inner.cs</c>.
    /// </summary>
    private static bool IsPartialContainerOfFileType(SyntaxNode type, string fileName)
    {
        if (type is not TypeDeclarationSyntax declaration || !declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
            return false;

        return declaration.DescendantNodes(node => node is TypeDeclarationSyntax)
            .Where(IsTypeDeclaration)
            .Any(nested => GetFileName(nested) == fileName);
    }

    private static string GetFileName(SyntaxNode type)
    {
        return GetQualifiedName(type) + ".cs";
    }

    /// <summary>
    /// The type's name prefixed with each containing type's name, outermost first: <c>Outer.Inner.Deeper</c>.
    /// </summary>
    private static string GetQualifiedName(SyntaxNode type)
    {
        var name = GetIdentifier(type).ValueText;
        for (var parent = type.Parent; parent is TypeDeclarationSyntax containing; parent = parent.Parent)
        {
            name = containing.Identifier.ValueText + "." + name;
        }

        return name;
    }

    private static bool IsTypeDeclaration(SyntaxNode node)
    {
        return node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax;
    }

    private static SyntaxToken GetIdentifier(SyntaxNode type)
    {
        return type switch
        {
            BaseTypeDeclarationSyntax declaration => declaration.Identifier,
            DelegateDeclarationSyntax declaration => declaration.Identifier,
            _ => default
        };
    }
}
