using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Constructor.Inheritor
{
    [Generator]
    public class InheritedConstructorGenerator : ISourceGenerator
    {
        private const string AttributeName = "InheritConstructorsAttribute";
        private static string AttributeText => @$"using System;
namespace {typeof(InheritedConstructorGenerator).Namespace}
{{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    sealed class {AttributeName} : Attribute {{}}
}}
";
        private static SourceText AttributeSource => SourceText.From(AttributeText, Encoding.UTF8);

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource("InheritConstructorsAttribute", AttributeSource);

            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
                return;

            var options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(AttributeSource, options));

            INamedTypeSymbol attributeSymbol = compilation.GetTypeByMetadataName(QualifiedAttributeName);

            foreach (var classDeclaration in receiver.CandidateClasses)
            {
                var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                var symbol = semanticModel.GetDeclaredSymbol(classDeclaration);
                if (symbol is ITypeSymbol classSymbol
                    && classSymbol.IsReferenceType
                    && classSymbol.IsMarkedWithAttribute(attributeSymbol))

                {
                    if (classDeclaration.IsPartial())
                    {
                        var source = GenerateInheritedConstructors(classSymbol);
                        if (source != null)
                            context.AddSource($"{ToFileName(classSymbol)}.Constructors.cs", SourceText.From(source, Encoding.UTF8));
                    }
                    //else
                    //    context.ReportDiagnostic(Diagnostic.Create(Rule, classSymbol.Locations.First(), classSymbol.Locations.Skip(1), classSymbol.Name));
                }
            }
        }


        public static string QualifiedAttributeName => typeof(InheritedConstructorGenerator).Namespace + "." + AttributeName;

        private static readonly SymbolDisplayFormat format = new SymbolDisplayFormat(
                            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);
        private static string ToFileName(ITypeSymbol classSymbol) => classSymbol.ToDisplayString(format).Replace('<', '(').Replace('>', ')');


        private string GenerateInheritedConstructors(ITypeSymbol typeSymbol)
        {
            var implementedConstructorSignatures
                = (from m in typeSymbol.GetMembers().OfType<IMethodSymbol>()
                  where m.MethodKind == MethodKind.Constructor
                  select (from p in m.Parameters select (p.Type, p.RefKind)).ToList()).ToImmutableHashSet(new SequenceComparer<(ITypeSymbol, RefKind)>());

            var inheritableConstructorSymbols
                = from m in typeSymbol.BaseType.GetMembers().OfType<IMethodSymbol>()
                  where m.MethodKind == MethodKind.Constructor
                    && m.DeclaredAccessibility != Accessibility.Private // anything else would work
                    && !implementedConstructorSignatures.Contains((from p in m.Parameters select (p.Type, p.RefKind)).ToList())
                  select m;

            if (inheritableConstructorSymbols.Count() == 0)
                return null;

            return $@"
namespace {typeSymbol.ContainingNamespace}
{{
  partial class {typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}
  {{
    {GenerateInheritedConstructors(typeSymbol.Name, inheritableConstructorSymbols)}
  }}
}}";
        }

        private string GenerateInheritedConstructors(string className, IEnumerable<IMethodSymbol> baseConstructors)
        {
            var sb = new StringBuilder();
                  
            foreach (var constructor in baseConstructors)
                sb.Append(GenerateInheritedConstructor(className, constructor));;

            return sb.ToString();
        }

        private string GenerateInheritedConstructor(string className, IMethodSymbol constructor)
        {
            var parameters = constructor.Parameters;
            return $@"
      {GenerateAccessibility(constructor.DeclaredAccessibility)} {className}({GenerateParameterDeclarations(parameters)})
        : base({GenerateParameterReferences(parameters)}) {{}}
";
        }

        private string GenerateParameterReferences(ImmutableArray<IParameterSymbol> parameters)
            => string.Join(", ", from p in parameters select GenerateRefKind(p.RefKind) + p.Name);

        private string GenerateRefKind(RefKind refKind) => refKind switch
        {
            RefKind.None => "",
            RefKind.Ref => "ref ",
            RefKind.Out => "out ",
            RefKind.RefReadOnly => "ref readonly",
            _ => throw new ArgumentException("Unknown RefKind value", nameof(refKind)),
        };

        private string GenerateParameterDeclarations(IEnumerable<IParameterSymbol> parameters)
            => string.Join(", ", from p in parameters
                                 select p.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

        private string GenerateAccessibility(Accessibility declaredAccessibility) => declaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedOrInternal => "protected internal",
            _ => throw new InvalidOperationException($"Unknown accessibility type: {declaredAccessibility}"),
        };

        public const string DiagnosticId = "Constructor.Inheritor";

        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";
        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public void Initialize(GeneratorInitializationContext context) 
            => context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());

        class SyntaxReceiver : ISyntaxReceiver
        {
            public List<ClassDeclarationSyntax> CandidateClasses { get; } = new();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // any field with at least one attribute is a candidate for property generation
                if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax
                    && classDeclarationSyntax.AttributeLists.Count > 0)
                    CandidateClasses.Add(classDeclarationSyntax);
            }
        }
    }
}
