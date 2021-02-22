using System.Collections.Generic;

namespace Connect4
{
    public static class ArrayExtensions
    {
        public static IEnumerable<T> SliceRow<T>(this T[,] array, int row)
        {
            for (var i = 0; i < array.GetLength(0); i++)
            {
                yield return array[i, row];
            }
        }
        public static IEnumerable<T> SliceColumn<T>(this T[,] array, int column)
        {
            for (var i = 0; i < array.GetLength(1); i++)
            {
                yield return array[column, i];
            }
        }
    }
}