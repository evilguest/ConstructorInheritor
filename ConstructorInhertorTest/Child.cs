using ConstructorInheritor;

namespace ConstructorInhertorTest
{
    [InheritConstructors]
    internal partial class Child: Parent
    {
        //public Child() { Console.WriteLine("Foo"); }

//        public Child(string name) { Console.WriteLine(name); }
    }
    [InheritConstructors]
    public partial class Child<T>: Parent<T> { }


}
