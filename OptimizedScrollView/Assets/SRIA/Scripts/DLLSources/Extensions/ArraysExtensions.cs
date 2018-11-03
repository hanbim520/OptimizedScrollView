
using System;

namespace UnityEngine.UI.Extension
{
    public static class ArraysExtensions
    {
        public static bool IsBetween(
            this float[] arr,
            float[] minInclusive,
            float[] maxInclusive)
        {
            for (int i = 0; i < arr.Length; ++i)
                if (arr[i] < minInclusive[i] || arr[i] > maxInclusive[i])
                    return false;

            return true;
        }

        public static T[] GetRotatedArray<T>(this T[] arr, int rotateAmount)
        {
            var result = new T[arr.Length];
            GetRotatedArray(arr, result, rotateAmount);

            return result;
        }

        public static void GetRotatedArray<T>(this T[] arr, T[] result, int rotateAmount)
        {
            int length = arr.Length;

            if (rotateAmount == 0)
            {
                Array.Copy(arr, result, length);

                return;
            }

            if (rotateAmount < 0)
                rotateAmount += length;

            if (Math.Abs(rotateAmount) >= length)
                throw new InvalidOperationException();

            Array.Copy(arr, 0, result, rotateAmount, length - rotateAmount);
            Array.Copy(arr, length - rotateAmount, result, 0, rotateAmount);
        }
    }
}

