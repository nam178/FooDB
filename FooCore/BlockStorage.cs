using System;
using System.Collections.Generic;
using System.IO;

namespace FooCore
{
	public class BlockStorage : IBlockStorage
	{
		readonly Stream stream;
		readonly int blockSize;
		readonly int blockHeaderSize;
		readonly int blockContentSize;
		readonly int unitOfWork;
		readonly Dictionary<uint, Block> blocks = new Dictionary<uint, Block> ();

		public int DiskSectorSize {
			get {
				return unitOfWork;
			}
		}

		public int BlockSize {
			get {
				return blockSize;
			}
		}

		public int BlockHeaderSize {
			get {
				return blockHeaderSize;
			}
		}

		public int BlockContentSize {
			get {
				return blockContentSize;
			}
		}

		//
		// Constructors
		//

		public BlockStorage (Stream storage, int blockSize = 40960, int blockHeaderSize = 48)
		{
			if (storage == null)
				throw new ArgumentNullException ("storage");

			if (blockHeaderSize >= blockSize) {
				throw new ArgumentException ("blockHeaderSize cannot be " +
					"larger than or equal " +
					"to " + "blockSize");
			}

			if (blockSize < 128) {
				throw new ArgumentException ("blockSize too small");
			}

			this.unitOfWork = ((blockSize >= 4096) ? 4096 : 128);
			this.blockSize = blockSize;
			this.blockHeaderSize = blockHeaderSize;
			this.blockContentSize = blockSize - blockHeaderSize;
			this.stream = storage;
		}

		//
		// Public Methods
		//

		public IBlock Find (uint blockId)
		{
			// Check from initialized blocks
			if (true == blocks.ContainsKey(blockId))
			{
				return blocks[blockId];
			}

			// First, move to that block.
			// If there is no such block return NULL
			var blockPosition = blockId * blockSize;
			if ((blockPosition + blockSize) > this.stream.Length)
			{
				return null;
			}

			// Read the first 4KB of the block to construct a block from it
			var firstSector = new byte[DiskSectorSize];
			stream.Position = blockId * blockSize;
			stream.Read (firstSector, 0, DiskSectorSize);

			var block = new Block (this, blockId, firstSector, this.stream);
			OnBlockInitialized (block);
			return block;
		}

		public IBlock CreateNew ()
		{
			if ((this.stream.Length % blockSize) != 0) {
				throw new DataMisalignedException ("Unexpected length of the stream: " + this.stream.Length);
			}

			// Calculate new block id
			var blockId = (uint)Math.Ceiling ((double)this.stream.Length / (double)blockSize);

			// Extend length of underlying stream
			this.stream.SetLength ((long)((blockId * blockSize) + blockSize));
			this.stream.Flush ();

			// Return desired block
			var block = new Block (this, blockId, new byte[DiskSectorSize], this.stream);
			OnBlockInitialized (block);
			return block;
		}

		//
		// Protected Methods
		//

		protected virtual void OnBlockInitialized (Block block)
		{
			// Keep reference to it
			blocks[block.Id] = block;

			// When block disposed, remove it from memory
			block.Disposed += HandleBlockDisposed;
		}

		protected virtual void HandleBlockDisposed (object sender, EventArgs e)
		{
			// Stop listening to it
			var block = (Block)sender;
			block.Disposed -= HandleBlockDisposed;

			// Remove it from memory
			blocks.Remove (block.Id);
		}
	}
}
