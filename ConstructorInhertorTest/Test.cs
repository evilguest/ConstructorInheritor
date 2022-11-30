using Xunit;

namespace ConstructorInhertorTest
{
    public class Test
    {
        [Fact]
        public void TestSimple()
        {
            var a = 42;
            var c = new Child(ref a);
        }
    }
}
