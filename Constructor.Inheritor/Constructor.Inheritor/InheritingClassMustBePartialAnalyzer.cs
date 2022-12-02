using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Constructor.Inheritor
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InheritingClassMustBePartialAnalyzer: DiagnosticAnalyzer
    {
        public const string DiagnosticId = "COIN1001";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Other";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
        }
        private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not ClassDeclarationSyntax classDeclaration)
                return;

            INamedTypeSymbol attributeSymbol = context.Compilation.GetTypeByMetadataName(InheritedConstructorGenerator.QualifiedAttributeName);

            var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

            if (symbol is ITypeSymbol classSymbol
                && classSymbol.IsReferenceType
                && classSymbol.IsMarkedWithAttribute(attributeSymbol) && !classDeclaration.IsPartial())
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Rule,
                        classSymbol.Locations.First(),
                        classSymbol.Locations.Skip(1), 
                        classSymbol.Name
                    )
                );
            }
        }
    }
}
