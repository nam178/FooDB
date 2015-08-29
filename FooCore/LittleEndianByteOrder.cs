using System;

namespace FooCore
{
	/// <summary>
	/// Helper class contains static methods to read and write
	/// numeric data in little endian byte order
	/// </summary>
	public static class LittleEndianByteOrder
	{
		public static byte[] GetBytes (int value)
		{
			var bytes = BitConverter.GetBytes (value);

			if (false == BitConverter.IsLittleEndian)
			{
				Array.Reverse (bytes);
			}

			return bytes;
		}

		public static byte[] GetBytes (long value)
		{
			var bytes = BitConverter.GetBytes (value);

			if (false == BitConverter.IsLittleEndian)
			{
				Array.Reverse (bytes);
			}

			return bytes;
		}

		public static byte[] GetBytes (uint value)
		{
			var bytes = BitConverter.GetBytes (value);

			if (false == BitConverter.IsLittleEndian)
			{
				Array.Reverse (bytes);
			}

			return bytes;
		}

		public static byte[] GetBytes (float value)
		{
			var bytes = BitConverter.GetBytes (value);

			if (false ==BitConverter.IsLittleEndian)
			{
				Array.Reverse (bytes);
			}

			return bytes;
		}

		public static byte[] GetBytes (double value)
		{
			var bytes = BitConverter.GetBytes (value);

			if (false == BitConverter.IsLittleEndian)
			{
				Array.Reverse (bytes);
			}

			return bytes;
		}

		public static float GetSingle (byte[] bytes)
		{
			// Given bytes are little endian
			// If this computer is big endian then result need to be reversed
			if (false == BitConverter.IsLittleEndian) {
				var bytesClone = new byte[bytes.Length];
				bytes.CopyTo (bytesClone, 0);
				Array.Reverse (bytesClone);
				return BitConverter.ToSingle (bytesClone, 0);
			} else {
				return BitConverter.ToSingle (bytes, 0);
			}
		}

		public static double GetDouble (byte[] bytes)
		{
			// Given bytes are little endian
			// If this computer is big endian then result need to be reversed
			if (false == BitConverter.IsLittleEndian) {
				var bytesClone = new byte[bytes.Length];
				bytes.CopyTo (bytesClone, 0);
				Array.Reverse (bytesClone);
				return BitConverter.ToDouble (bytesClone, 0);
			} else {
				return BitConverter.ToDouble (bytes, 0);
			}
		}

		public static long GetInt64 (byte[] bytes)
		{
			// Given bytes are little endian
			// If this computer is big endian then result need to be reversed
			if (false == BitConverter.IsLittleEndian) {
				var bytesClone = new byte[bytes.Length];
				bytes.CopyTo (bytesClone, 0);
				Array.Reverse (bytesClone);
				return BitConverter.ToInt64 (bytesClone, 0);
			} else {
				return BitConverter.ToInt64 (bytes, 0);
			}
		}

		public static int GetInt32 (byte[] bytes)
		{
			// Given bytes are little endian
			// If this computer is big endian then result need to be reversed
			if (false == BitConverter.IsLittleEndian) {
				var bytesClone = new byte[bytes.Length];
				bytes.CopyTo (bytesClone, 0);
				Array.Reverse (bytesClone);
				return BitConverter.ToInt32 (bytesClone, 0);
			} else {
				return BitConverter.ToInt32 (bytes, 0);
			}
		}

		public static uint GetUInt32 (byte[] bytes)
		{
			// Given bytes are little endian
			// If this computer is big endian then result need to be reversed
			if (false == BitConverter.IsLittleEndian) {
				var bytesClone = new byte[bytes.Length];
				bytes.CopyTo (bytesClone, 0);
				Array.Reverse (bytesClone);
				return BitConverter.ToUInt32 (bytesClone, 0);
			} else {
				return BitConverter.ToUInt32 (bytes, 0);
			}
		}

		public static int GetInt32 (byte[] bytes, int offset, int count)
		{
			var copied = new byte[count];
			Buffer.BlockCopy (bytes, offset, copied, 0, count);
			return GetInt32 (copied);
		}
	}
}