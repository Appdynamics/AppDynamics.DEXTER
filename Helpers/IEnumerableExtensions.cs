using System.Collections.Generic;
using System.Linq;

namespace AppDynamics.Dexter.Extensions
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<List<T>> BreakListIntoChunks<T>(this IEnumerable<T> sourceList, int chunkSize)
        {
            List<T> chunkReturn = new List<T>(chunkSize);
            foreach (var item in sourceList)
            {
                chunkReturn.Add(item);
                if (chunkReturn.Count == chunkSize)
                {
                    yield return chunkReturn;
                    chunkReturn = new List<T>(chunkSize);
                }
            }
            if (chunkReturn.Any())
            {
                yield return chunkReturn;
            }
        }
    }
}
