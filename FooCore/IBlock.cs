using System;

namespace FooCore
{
	public interface IBlock : IDisposable
	{
		/// <summary>
		/// Id of the block, must be unique
		/// </summary>
		uint Id {
			get;
		}

		/// <summary>
		/// A block may contain one ore more header metadata, 
		/// each header identified by a number and 8 bytes value.
		/// </summary>
		long GetHeader (int field);

		/// <summary>
		/// Change a value for given header.
		/// Data must not be written to disk until the block is disposed.
		/// </summary>
		void SetHeader (int field, long value);

		/// <summary>
		/// Read content of this block (src) into given buffer (dst)
		/// </summary>
		void Read (byte[] dst, int dstOffset, int srcOffset, int count);

		/// <summary>
		/// Write content of given buffer (src) into this (dst)
		/// </summary>
		void Write (byte[] src, int srcOffset, int dstOffset, int count);
	}
}