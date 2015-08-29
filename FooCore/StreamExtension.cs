using System;
using System.IO;
using System.Threading.Tasks;

namespace FooCore
{
	public static class StreamExtension
	{
		/// <summary>
		/// Treat given up coming data as stream, return that strea,
		/// </summary>
		public static StreamReadWrapper ExpectStream (this Stream target, long length)
		{
			return new StreamReadWrapper (target, length);
		}

		/// <summary>
		/// Write all buffer into stream
		/// </summary>
		public static void Write (this Stream stream, byte[] buffer)
		{
			stream.Write (buffer, 0, buffer.Length);
		}

		/// <summary>
		/// Read until given buffer is filled or end of stream is reached
		/// </summary>
		public static int Read (this Stream src, byte[] buffer)
		{
			var filled = 0;
			var lastRead = 0;
			while (filled < buffer.Length)
			{
				lastRead = src.Read (buffer, filled, buffer.Length - filled);
				filled += lastRead;
				if (lastRead == 0) break;
			}

			return filled;
		}

		/// <summary>
		/// Read until given buffer is filled or end of stream is reached (async version)
		/// </summary>
		public static async Task<int> ReadAsync (this Stream src, byte[] buffer)
		{
			var filled = 0;
			var lastRead = 0;
			while (filled < buffer.Length)
			{
				lastRead = await src.ReadAsync (buffer, filled, buffer.Length - filled);
				filled += lastRead;
				if (lastRead == 0) break;
			}

			return filled;
		}

		/// <summary>
		/// Expect the upcoming 4 bytes to be a float, read and return it
		/// </summary>
		public static float ExpectFloat (this Stream target)
		{
			var buff = new byte[4];
			if (target.Read (buff) == 4)
			{
				return LittleEndianByteOrder.GetSingle (buff);
			} else {
				throw new EndOfStreamException ();
			}
		}

		/// <summary>
		/// Expect the upcoming 4 bytes are int32, read it and return it;
		/// </summary>
		public static int ExpectInt32 (this Stream target)
		{
			var buff = new byte[4];
			if (target.Read (buff) == 4)
			{
				return LittleEndianByteOrder.GetInt32 (buff);
			} else {
				throw new EndOfStreamException ();
			}
		}

		/// <summary>
		/// Expect the upcoming 4 bytes are int32, read it and return it;
		/// </summary>
		public static uint ExpectUInt32 (this Stream target)
		{
			var buff = new byte[4];
			if (target.Read (buff) == 4)
			{
				return LittleEndianByteOrder.GetUInt32 (buff);
			} else {
				throw new EndOfStreamException ();
			}
		}

		/// <summary>
		/// Expect the upcoming 8 bytes are int64, read it and return it;
		/// </summary>
		public static long ExpectInt64 (this Stream target)
		{
			var buff = new byte[8];
			if (target.Read (buff) == 8)
			{
				return LittleEndianByteOrder.GetInt64 (buff);
			} else {
				throw new EndOfStreamException ();
			}
		}

		public static double ExpectDouble (this Stream target)
		{
			var buff = new byte[8];
			if (target.Read (buff) == 8)
			{
				return LittleEndianByteOrder.GetDouble (buff);
			} else {
				throw new EndOfStreamException ();
			}
		}

		/// <summary>
		/// Expect up coming byte is a bool, read and return it
		/// </summary>
		public static bool ExpectBool (this Stream target)
		{
			return Convert.ToBoolean (target.ReadByte ());
		}

		/// <summary>
		/// Expect upcoming 16 bytes is a guid, read and return it
		/// </summary>
		public static Guid ExpectGuid (this Stream target)
		{
			var buff = new byte[16];
			if (target.Read(buff) == 16) {
				return new Guid (buff);
			} else {
				throw new EndOfStreamException ();
			}
		}

		/// <summary>
		/// Similar to Stream.CopyTo, but this one allows you to inject a delegate
		/// to receive feedback, i.e. for displaying a loading bar.
		/// </summary>
		public static void CopyTo (this Stream src
			, Stream destination
			, int bufferSize = 4096
			, Func<long, bool> feedback = null
			, long maxLength = 0)
		{
			var buffer = new byte[bufferSize];
			var totalRead = 0L;

			while (totalRead < maxLength)
			{
				var bytesToRead = (int)Math.Min (maxLength - totalRead, buffer.Length);
				var thisRead = src.Read (buffer, 0, bytesToRead);

				if (thisRead == 0)
					throw new EndOfStreamException ();

				// Update total of bytes read
				totalRead += thisRead;

				// Write buff
				destination.Write (buffer, 0, thisRead);
				destination.Flush();

				// Call feedback;
				// Note: if feedback returns false then stop copying
				if ((feedback != null) && (feedback (totalRead) == false))
				{
					return;
				}
			} 
		}

		/// <summary>
		/// Async version of CopyTo() above
		/// </summary>
		public static async Task CopyToAsync (this Stream src
			, Stream destination
			, int bufferSize = 4096
			, Func<long, bool> feedback = null
			, long maxLength = 0)
		{
			var buffer = new byte[bufferSize];
			var totalRead = 0L;

			while (totalRead < maxLength)
			{
				var bytesToRead = (int)Math.Min (maxLength - totalRead, buffer.Length);
				var thisRead = await src.ReadAsync (buffer, 0, bytesToRead);

				if (thisRead == 0)
					throw new EndOfStreamException ();

				// Update total of bytes read
				totalRead += thisRead;

				// Write buff
				destination.Write (buffer, 0, thisRead);
				destination.Flush();

				// Call feedback;
				// Note: if feedback returns false then stop copying
				if ((feedback != null) && (feedback (totalRead) == false))
				{
					return;
				}
			} 
		}
	}
}

