namespace ExtensionFunctions
{
    public static class ByteExtensions
    {
        public static bool Bool(this byte mask, int bit) => (mask & (1 << bit)) != 0;
    }
}