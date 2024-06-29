using Unity.Mathematics;

namespace ExtensionFunctions
{
    public static class ArrayExtensions
    {
        public static T[,,] GetSubArray<T>(this T[,,] array, int? from1 = null, int? to1 = null, int? from2 = null,
            int? to2 = null, int? from3 = null, int? to3 = null)
        {
            from1 ??= 0;
            from2 ??= 0;
            from3 ??= 0;
            to1 ??= array.GetLength(0);
            to2 ??= array.GetLength(1);
            to3 ??= array.GetLength(2);
            // Prevent overshooting
            from1 = math.max(0, (int)from1);
            from2 = math.max(0, (int)from2);
            from3 = math.max(0, (int)from3);
            to1 = math.min((int)to1, array.GetLength(0));
            to2 = math.min((int)to2, array.GetLength(1));
            to3 = math.min((int)to3, array.GetLength(2));
            var size1 = (int)to1 - (int)from1;
            var size2 = (int)to2 - (int)from2;
            var size3 = (int)to3 - (int)from3;
            var result = new T[size1, size2, size3];
            for (var i1 = 0; i1 < size1; i1++)
            for (var i2 = 0; i2 < size2; i2++)
            for (var i3 = 0; i3 < size3; i3++)
                result[i1, i2, i3] = array[(int)from1 + i1, (int)from2 + i2, (int)from3 + i3];
            return result;
        }
    }
}