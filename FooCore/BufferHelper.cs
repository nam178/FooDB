using System;

namespace FooCore
{
	/// <summary>
	/// Helper class contains static methods that read and write numeric values
	/// into and from a byte array in little endian byte order.
	/// </summary>
	public static class BufferHelper
	{
		public static Guid ReadBufferGuid (byte[] buffer, int bufferOffset)
		{
			var guidBuffer = new byte[16];
			Buffer.BlockCopy (buffer, bufferOffset, guidBuffer, 0, 16);
			return new Guid (guidBuffer);
		}

		public static uint ReadBufferUInt32 (byte[] buffer, int bufferOffset)
		{
			var uintBuffer = new byte[4];
			Buffer.BlockCopy (buffer, bufferOffset, uintBuffer, 0, 4);
			return LittleEndianByteOrder.GetUInt32 (uintBuffer);
		}

		public static int ReadBufferInt32 (byte[] buffer, int bufferOffset)
		{
			var intBuffer = new byte[4];
			Buffer.BlockCopy (buffer, bufferOffset, intBuffer, 0, 4);
			return LittleEndianByteOrder.GetInt32 (intBuffer);
		}

		public static long ReadBufferInt64 (byte[] buffer, int bufferOffset)
		{
			var longBuffer = new byte[8];
			Buffer.BlockCopy (buffer, bufferOffset, longBuffer, 0, 8);
			return LittleEndianByteOrder.GetInt64 (longBuffer);
		}

		public static double ReadBufferDouble (byte[] buffer, int bufferOffset)
		{
			var doubleBuffer = new byte[8];
			Buffer.BlockCopy (buffer, bufferOffset, doubleBuffer, 0, 8);
			return LittleEndianByteOrder.GetDouble (doubleBuffer);
		}

		public static void WriteBuffer (double value, byte[] buffer, int bufferOffset)
		{
			Buffer.BlockCopy (LittleEndianByteOrder.GetBytes(value), 0, buffer, bufferOffset, 8);
		}

		public static void WriteBuffer (uint value, byte[] buffer, int bufferOffset)
		{
			Buffer.BlockCopy (LittleEndianByteOrder.GetBytes(value), 0, buffer, bufferOffset, 4);
		}

		public static void WriteBuffer (long value, byte[] buffer, int bufferOffset)
		{
			Buffer.BlockCopy (LittleEndianByteOrder.GetBytes(value), 0, buffer, bufferOffset, 8);
		}

		public static void WriteBuffer (int value, byte[] buffer, int bufferOffset)
		{
			Buffer.BlockCopy (LittleEndianByteOrder.GetBytes((int)value), 0, buffer, bufferOffset, 4);
		}

		public static void WriteBuffer (Guid value, byte[] buffer, int bufferOffset)
		{
			Buffer.BlockCopy (value.ToByteArray(), 0, buffer, bufferOffset, 16);
		}
	}
}

