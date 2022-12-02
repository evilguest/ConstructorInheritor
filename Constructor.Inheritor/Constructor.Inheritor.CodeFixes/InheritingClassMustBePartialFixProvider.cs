using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Constructor.Inheritor
{
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class InheritingClassMustBePartialFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(InheritingClassMustBePartialAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                if (diagnostic.Id != InheritingClassMustBePartialAnalyzer.DiagnosticId)
                    continue;
                var action = CodeAction.Create(
                    CodeFixResources.CodeFixTitle,
                    cancellationToken => AddPartialAsync(context.Document, diagnostic, cancellationToken),
                    CodeFixResources.CodeFixTitle.ToString());
                context.RegisterCodeFix(action, diagnostic);
            }

            return Task.CompletedTask;
        }


        private static async Task<Document> AddPartialAsync(Document document, Diagnostic makePartial, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            if (root is null)
                return document;

            var classDeclaration = FindClassDeclaration(makePartial, root);

            var partial = SyntaxFactory.Token(SyntaxKind.PartialKeyword);
            var newDeclaration = classDeclaration.AddModifiers(partial);
            var newRoot = root.ReplaceNode(classDeclaration, newDeclaration);
            var newDoc = document.WithSyntaxRoot(newRoot);

            return newDoc;
        }

        private static ClassDeclarationSyntax FindClassDeclaration(Diagnostic makePartial, SyntaxNode root)
            => root.FindToken(makePartial.Location.SourceSpan.Start).Parent?.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();
    }
}