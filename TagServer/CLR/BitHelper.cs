﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace StackOverflowTagServer.CLR
{
    /// <summary>
    /// ABOUT:
    /// Helps with operations that rely on bit marking to indicate whether an item in the
    /// collection should be added, removed, visited already, etc.
    ///
    /// BitHelper doesn't allocate the array; you must pass in an array or ints allocated on the
    /// stack or heap. ToIntArrayLength() tells you the int array size you must allocate.
    ///
    /// USAGE:
    /// Suppose you need to represent a bit array of length (i.e. logical bit array length)
    /// BIT_ARRAY_LENGTH. Then this is the suggested way to instantiate BitHelper:
    /// ***************************************************************************
    /// int intArrayLength = BitHelper.ToIntArrayLength(BIT_ARRAY_LENGTH);
    /// BitHelper bitHelper;
    /// if (intArrayLength less than stack alloc threshold)
    ///     int* m_arrayPtr = stackalloc int[intArrayLength];
    ///     bitHelper = new BitHelper(m_arrayPtr, intArrayLength);
    /// else
    ///     int[] m_arrayPtr = new int[intArrayLength];
    ///     bitHelper = new BitHelper(m_arrayPtr, intArrayLength);
    /// ***************************************************************************
    ///
    /// IMPORTANT:
    /// The second ctor args, length, should be specified as the length of the int array, not
    /// the logical bit array. Because length is used for bounds checking into the int array,
    /// it's especially important to get this correct for the stackalloc version. See the code
    /// samples above; this is the value gotten from ToIntArrayLength().
    ///
    /// The length ctor argument is the only exception; for other methods -- MarkBit and
    /// IsMarked -- pass in values as indices into the logical bit array, and it will be mapped
    /// to the position within the array of ints.
    ///

    unsafe internal class BitHelper
    {
        private const byte MarkedBitFlag = 1;
        private const byte IntSize = 32;

        // m_length of underlying int array (not logical bit array)
        private int m_length;

        // ptr to stack alloc'd array of ints
        [System.Security.SecurityCritical]
        private int* m_arrayPtr;

        // array of ints
        private int[] m_array;

        // whether to operate on stack alloc'd or heap alloc'd array
        private bool useStackAlloc;
        private int maxAllowedBit;
        private bool maxAllowedBitSet;



        /// <summary>
        /// Instantiates a BitHelper with a heap alloc'd array of ints
        /// </summary>
        /// <param name="bitArray">int array to hold bits</param>
        /// <param name="length">length of int array</param>
        [System.Security.SecurityCritical]
        internal BitHelper(int* bitArrayPtr, int length)
        {
            m_arrayPtr = bitArrayPtr;
            m_length = length;
            useStackAlloc = true;
        }

        /// <summary>
        /// Instantiates a BitHelper with a heap alloc'd array of ints
        /// </summary>
        /// <param name="bitArray">int array to hold bits</param>
        /// <param name="length">length of int array</param>
        internal BitHelper(int[] bitArray, int length)
        {
            m_array = bitArray;
            m_length = length;
        }

        /// <summary>
        /// Mark bit at specified position
        /// </summary>
        /// <param name="bitPosition"></param>
        [System.Security.SecuritySafeCritical]
        internal unsafe void MarkBit(int bitPosition)
        {
            int bitArrayIndex = bitPosition / IntSize;
            if (bitArrayIndex < m_length && bitArrayIndex >= 0)
            {
                if (useStackAlloc)
                    m_arrayPtr[bitArrayIndex] |= (MarkedBitFlag << (bitPosition % IntSize));
                else
                    m_array[bitArrayIndex] |= (MarkedBitFlag << (bitPosition % IntSize));
            }
            else
            {
                throw new ArgumentOutOfRangeException("bitPosition",
                            string.Format("Must be less than {0}, but was {1} (bitPosition:{2})", m_length, bitArrayIndex, bitPosition));
            }
        }

        /// <summary>
        /// Is bit at specified position marked?
        /// </summary>
        /// <param name="bitPosition"></param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]
        internal unsafe bool IsMarked(int bitPosition)
        {
            int bitArrayIndex = bitPosition / IntSize;
            if (bitArrayIndex < m_length && bitArrayIndex >= 0)
            {
                if (useStackAlloc)
                    return ((m_arrayPtr[bitArrayIndex] & (MarkedBitFlag << (bitPosition % IntSize))) != 0);
                else
                    return ((m_array[bitArrayIndex] & (MarkedBitFlag << (bitPosition % IntSize))) != 0);
            }
            else
            {
                return false; // This seems wierd
                //throw new ArgumentOutOfRangeException("bitPosition",
                //            string.Format("Must be less than {0}, but was {1} (bitPosition:{2})", m_length, bitArrayIndex, bitPosition));
            }
        }

        /// <summary>
        /// Inverts all the bit values. On/true bit values are converted to off/false.
        /// Off/false bit values are turned on/true. The current instance is updated and returned.
        /// Code from Not http://referencesource.microsoft.com/#mscorlib/system/collections/bitarray.cs,e71a526d814e6d57
        /// </summary>
        /// <returns></returns>
        internal BitHelper Not()
        {
            for (int i = 0; i < m_array.Length; i++)
            {
                m_array[i] = ~m_array[i];
            }

            return this;
        }

        /// <summary>
        /// How many ints must be allocated to represent n bits. Returns (n+31)/32, but
        /// avoids overflow
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        internal static int ToIntArrayLength(int n)
        {
            return n > 0 ? ((n - 1) / IntSize + 1) : 0;
        }

        internal int GetCardinality()
        {
            int counter = 0;
            for (int i = 0; i < m_array.Length; i++)
            {
                if (i == m_array.Length - 1 && maxAllowedBitSet)
                {
                    // Special case, don't count the Bits that aren't being used!!
                    var bitsToUse = maxAllowedBit % IntSize;
                    var bits = Convert.ToString(m_array[i], 2).PadLeft(IntSize, '0').Reverse().Take(bitsToUse).ToArray();
                    counter += bits.Count(c => c == '1');
                }
                else
                {
                    counter += NumberOfSetBits(m_array[i]);
                }
            }
            return counter;
        }

        /// <summary>
        /// WARNING: this could be SLOOOOW, it converts each 32-bit value into a char array (i.e. ['1', '0', '1', ...])
        /// and the gets the positions of all the '1' values from that array
        /// </summary>
        /// <returns></returns>
        internal List<int> GetPositions()
        {
            var positions = new List<int>(GetCardinality());
            for (int intCounter = 0; intCounter < m_array.Length; intCounter++)
            {
                var bits = Convert.ToString(m_array[intCounter], 2).PadLeft(IntSize, '0').Reverse().ToArray(); // .ToCharArray();
                for (int b = 0; b < bits.Length; b++)
                {
                    var bit = bits[b];
                    if (bit == '1')
                    {
                        var bitPosition = (intCounter * IntSize) + b;
                        if (maxAllowedBitSet == false)
                            positions.Add(bitPosition);
                        else if (bitPosition < maxAllowedBit)
                            positions.Add(bitPosition);
                    }
                }
            }

            if (positions.Count != GetCardinality())
                Console.WriteLine("GetPositions, got {0:N0} positions, expected {1:N0} (from GetCardinality())", positions.Count, GetCardinality());

            return positions;
        }

        internal void SetMaxAllowedBit(int maxAllowedBit)
        {
            this.maxAllowedBit = maxAllowedBit;
            this.maxAllowedBitSet = true;
        }

        // From http://stackoverflow.com/questions/12171584/what-is-the-fastest-way-to-count-set-bits-in-uint32-in-c-sharp/12175897#12175897
        private int NumberOfSetBits(int i)
        {
            i = i - ((i >> 1) & 0x55555555);
            i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
            return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }
    }
}
