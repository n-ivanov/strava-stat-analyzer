using System;

namespace System.Extensions
{
    public static class StringExtensions
    {
        public static string PadBoth(this string source, int length, char paddingChar = ' ')
        {
            int spaces = length - source.Length;
            int padLeft = spaces/2 + source.Length;
            return source.PadLeft(padLeft, paddingChar).PadRight(length, paddingChar);
        }
    }
}