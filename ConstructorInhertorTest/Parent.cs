using System.Xml.Linq;

namespace ConstructorInhertorTest
{
    public class Parent
    {
        //public Parent() { }
        //protected Parent (string name = "Eve") => Console.WriteLine("Parent: " + name);
        internal Parent(ref int age) => Console.WriteLine(age++);
    }

    public class Parent<T>
    {
        public Parent(T item) { }
    }
}