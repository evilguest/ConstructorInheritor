using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Constructor.Inheritor
{
    internal static class CodeAnalysisExtensions
    {
        public static bool IsPartial(this ClassDeclarationSyntax classDeclaration) 
            => classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
        public static bool IsMarkedWithAttribute(this ITypeSymbol classSymbol, INamedTypeSymbol attributeSymbol) 
            => classSymbol.GetAttributes().Any(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
    }
}
