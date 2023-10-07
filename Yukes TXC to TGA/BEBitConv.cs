using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Yukes_TXC_to_PNG
{
    public static class BEBitConv
    {
        public unsafe static byte[] GetBytes(short value)
        {
            byte[] array = new byte[2];
            fixed (byte* ptr = array)
            {
                ptr[0] = (byte)(value >> 8);    // Store the high byte (MSB) at index 0
                ptr[1] = (byte)value;            // Store the low byte (LSB) at index 1
            }
            return array;
        }

        public unsafe static byte[] GetBytes(int value)
        {
            byte[] array = new byte[4];
            fixed (byte* ptr = array)
            {
                ptr[0] = (byte)(value >> 24);   // Store the highest byte (MSB) at index 0
                ptr[1] = (byte)(value >> 16);   // Store the second highest byte at index 1
                ptr[2] = (byte)(value >> 8);    // Store the second lowest byte at index 2
                ptr[3] = (byte)value;            // Store the lowest byte (LSB) at index 3
            }
            return array;
        }

        public unsafe static byte[] GetBytes(long value)
        {
            byte[] array = new byte[8];
            fixed (byte* ptr = array)
            {
                ptr[0] = (byte)(value >> 56);
                ptr[1] = (byte)(value >> 48);
                ptr[2] = (byte)(value >> 40);
                ptr[3] = (byte)(value >> 32);
                ptr[4] = (byte)(value >> 24);
                ptr[5] = (byte)(value >> 16);
                ptr[6] = (byte)(value >> 8);
                ptr[7] = (byte)value;
            }
           return array;
        }

        public static byte[] GetBytes(ushort value)
        {
            return GetBytes((short)value);
        }

        public static byte[] GetBytes(uint value)
        {
            return GetBytes((int)value);
        }

        public static byte[] GetBytes(ulong value)
        {
            return GetBytes((long)value);
        }

        public unsafe static byte[] GetBytes(float value)
        {
            return GetBytes(*(int*)(&value));
        }

        public unsafe static byte[] GetBytes(double value)
        {
            return GetBytes(*(long*)(&value));
        }   
    }
}
