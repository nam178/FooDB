using System;

namespace FooCore
{
	public interface IBlockStorage
	{
		/// <summary>
		/// Number of bytes of custom data per block that this storage can handle.
		/// </summary>
		int BlockContentSize {
			get;
		}

		/// <summary>
		/// Total number of bytes in header
		/// </summary>
		int BlockHeaderSize {
			get;
		}

		/// <summary>
		/// Total block size, equal to content size + header size, should be a multiple of 4KB
		/// </summary>
		int BlockSize {
			get;
		}

		/// <summary>
		/// Find a block by its id
		/// </summary>
		IBlock Find (uint blockId);

		/// <summary>
		/// Allocate new block, extend the length of underlying storage
		/// </summary>
		IBlock CreateNew ();
	}
}

