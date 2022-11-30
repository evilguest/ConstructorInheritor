using Xunit;

namespace ConstructorInhertorTest
{
    public class Test
    {
        [Fact]
        public void TestSimple()
        {
            var a = "Adam";
            var c = new Child(ref a);
        }
    }
}
