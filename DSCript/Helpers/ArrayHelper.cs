namespace System
{
    public static class ArrayHelper
    {
        public static bool IsNullOrEmpty(Array array)
        {
            return (array == null || array.Length == 0);
        }
    }
}
