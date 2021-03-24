using System;
using System.Collections.Generic;
using System.Linq;

namespace WolfGoatCabbage
{
    public static class UtExt
    {
        public static IEnumerable<IEnumerable<T>> Combinations<T>(this IEnumerable<T> elements, int k)
        {
            return k == 0 ? new[] { new T[0] } : elements.SelectMany((e, i) => elements.Skip(i + 1).Combinations(k - 1).Select(c => (new[] { e }).Concat(c)));
        }

        public static string JoinWithCommasOrAnd<T>(this IEnumerable<T> elements)
        {
            if (elements.Count() == 0)
                throw new Exception("Error: The collection is empty.");
            var el = elements.Select(e => e.ToString().Trim()).ToArray();
            if (el.Length == 2)
                return el[0] + " and " + el[1];
            else
            {
                string parsedString = el[0];
                for (int i = 1; i < el.Length; i++)
                    parsedString += ", " + (i == el.Length - 1 ? "and " : "") + el[i];
                return parsedString;
            }
        }
    }
}
