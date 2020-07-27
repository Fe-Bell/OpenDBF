using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDBF.Shared.Generic
{
    public static class NumericUtils
    {
        public static IEnumerable<long> Range(long start, long count)
        {
            var arr = new long[count];
            for (long i = 0; i < count; i++)
            {
                arr[i] = start;
                start++;
            }

            return arr;
        }

        public static IEnumerable<uint> Range(uint start, uint count)
        {
            count = count == 0 ? 1 : count;

            var arr = new uint[count];
            for (uint i = 0; i < count; i++)
            {
                arr[i] = start;
                start++;
            }

            return arr;
        }

        public static IEnumerable<ulong> Range(ulong start, ulong count)
        {
            var arr = new ulong[count];
            for (ulong i = 0; i < count; i++)
            {
                arr[i] = start;
                start++;
            }

            return arr;
        }
    }
}
