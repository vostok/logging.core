﻿using System;

namespace Vostok.Logging.Core.Helpers
{
    internal static class CharArrayCache
    {
        private const int MaximumSize = 384;

        [ThreadStatic]
        private static char[] CachedArray;

        public static char[] Acquire(int capacity)
        {
            if (capacity <= MaximumSize)
            {
                var array = CachedArray;

                if (capacity <= array?.Length)
                {
                    CachedArray = null;
                    return array;
                }
            }

            return new char[capacity];
        }

        public static void Return(char[] array)
        {
            if (array.Length <= MaximumSize)
            {
                CachedArray = array;
            }
        }
    }
}