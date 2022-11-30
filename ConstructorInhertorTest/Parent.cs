namespace ConstructorInhertorTest
{
    public partial class Parent
    {
        public Parent() { }
        public Parent(string name = "Eve") { Console.WriteLine("Parent: " + name); }
        internal Parent(string name, int age) { }
    }
}