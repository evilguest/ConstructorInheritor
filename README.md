# Constructor.Inheritor
Simple Source Generator that automatically duplicates all the base class constructors in the class of your choice.

The following code sample tells the most of it:

```csharp
public class Parent
{
   public Parent(string foo) => Console.WriteLine(foo);
   protected Parent(int bar) => Console.WriteLine(bar);
}

...

using Constructor.Inheritor;

  [InheritConstructors]
  internal partial class Child: Parent
  {
     // will behave as if the following is declared here:
     // public Child(string foo): base(foo) {}
     // protected Child(int bar): base(bar) {}
  }

```
