using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Constructor.Inheritor.Test
{
    using VerifyCS = CSharpCodeFixVerifier<InheritingClassMustBePartialAnalyzer, InheritingClassMustBePartialFixProvider>;

    [TestClass]
    public class ConstructorInhertorUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        /*
        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestMethod2()
        {
            var test = @"
    using Constructor.Inheritor;

    namespace ConsoleApplication1
    {
        [InheritConstructors]
        class {|#0:TypeName|}
        {   
        }
    }";

            var fixtest = @"
    using Constructor.Inheritor;

    namespace ConsoleApplication1
    {
        [InheritConstructors]
        partial class TypeName
        {   
        }
    }";

            var expected = VerifyCS.Diagnostic("Constructor.Inheritor").WithLocation(0).WithArguments("TypeName");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
        */
    }
}
