using System;

namespace TempProServer
{
    public static class CompatApi
    {
        public static Array CopyArray(Array src, Array dest)
        {
            Array.Copy(src, dest, src.Length);
            return dest;
        }
    }
}