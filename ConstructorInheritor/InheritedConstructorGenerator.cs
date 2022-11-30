using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConstructorInheritor
{
    [Generator]
    public class InheritedConstructorGenerator : ISourceGenerator
    {
        private const string attributeText = @"
using System;
namespace ConstructorInheritor
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    sealed class InheritConstructorsAttribute : Attribute
    {
    }
}
";
        private static SourceText AttributeSource => SourceText.From(attributeText, Encoding.UTF8);

        public void Execute(GeneratorExecutionContext context)
        {
            context.AddSource("InheritConstructorsAttribute", AttributeSource);

            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
                return;

            var options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(AttributeSource, options));

            INamedTypeSymbol attributeSymbol = compilation.GetTypeByMetadataName("ConstructorInheritor.InheritConstructorsAttribute");

            foreach(var classDeclaration in receiver.CandidateClasses)
            {
                var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                var symbol = semanticModel.GetDeclaredSymbol(classDeclaration);
                if (symbol is ITypeSymbol typeSymbol && typeSymbol.IsReferenceType
                                    && typeSymbol.GetAttributes().Any(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default)))
                {
                    if (!classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(CreateMissingPartialDescriptor(), classDeclaration. GetLocation(), typeSymbol.Name));
                    }
                    else
                    {
                        var classToHandle = typeSymbol;

                        var source = GenerateInheritedConstructors(classToHandle);
                        if (source != null)
                            context.AddSource($"{classToHandle.Name}.Constructors.cs", SourceText.From(source, Encoding.UTF8));
                    }
                }
            }
        }
        /*
        private static async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root =
              await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticTree = diagnostic.Location.SourceTree;
            var classToFix = diagnosticTree.GetRoot().DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>().First();

            const string title = "Make Partial";
            context.RegisterCodeFix(CodeAction.Create(title, c => FixPartialAsync(context.Document, classToFix, c), equivalenceKey: title), diagnostic);
        }

        private static Task<Document> FixPartialAsync(Document document, ClassDeclarationSyntax classToFix, CancellationToken c)
        {
            throw new NotImplementedException();
        }*/

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
                return null; // may I return null? 

            return $@"
namespace {typeSymbol.ContainingNamespace}
{{
  partial class {typeSymbol.Name}
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
            => string.Join(", ", from p in parameters select p.Name);

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

        public void Initialize(GeneratorInitializationContext context) 
            => context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        private static DiagnosticDescriptor CreateMissingPartialDescriptor() 
            => new DiagnosticDescriptor(
                "CACI0001",
                "The class marked with IheritConstructors attribute must be marked as partial",
                "Add 'partial' modifier to {0} to allow automatic parent constructors inheritance",
                "Constructor.Inheritance", DiagnosticSeverity.Error, true);

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
