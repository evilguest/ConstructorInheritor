using ConstructorInheritor;

namespace ConstructorInhertorTest
{
    [InheritConstructors]
    internal partial class Child: Parent
    {
        //public Child() { Console.WriteLine("Foo"); }

        public Child(ref string name) { Console.WriteLine(name); }
    }
}
