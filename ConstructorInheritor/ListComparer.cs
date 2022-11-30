using System.Collections.Generic;
using System.Linq;

namespace ConstructorInheritor
{
    internal class SequenceComparer<T> : IEqualityComparer<IEnumerable<T>>
    {
        public bool Equals(IEnumerable<T> x, IEnumerable<T> y) => Enumerable.SequenceEqual(x, y);

        public int GetHashCode(IEnumerable<T> obj) => Enumerable.Aggregate(obj, 0, (hash, item) => hash * 31 + item.GetHashCode());
    }
}