using System;
using System.Diagnostics;
using System.IO;

namespace FooCore
{
	public class Block : IBlock
	{
		readonly byte[] firstSector;
		readonly long?[] cachedHeaderValue = new long?[5];
		readonly Stream stream;
		readonly BlockStorage storage;
		readonly uint id;

		bool isFirstSectorDirty = false;
		bool isDisposed = false;

		public event EventHandler Disposed;

		public uint Id {
			get {
				return id;
			}
		}

		//
		// Constructors
		//

		public Block (BlockStorage storage, uint id, byte[] firstSector, Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException ("stream");

			if (firstSector == null)
				throw new ArgumentNullException ("firstSector");

			if (firstSector.Length != BlockStorage.DiskSectorSize)
				throw new ArgumentException ("firstSector length must be " + BlockStorage.DiskSectorSize);

			this.storage = storage;
			this.id = id;
			this.stream = stream;
			this.firstSector = firstSector;
		}

		//
		// Public Methods
		//

		public long GetHeader (int field)
		{
			if (isDisposed) {
				throw new ObjectDisposedException ("Block");
			}

			// Validate field number
			if (field < 0) {
				throw new IndexOutOfRangeException ();
			}
			if (field >= (storage.BlockHeaderSize/8)) {
				throw new ArgumentException ("Invalid field: " + field);
			}

			// Check from cache, if it is there then return it
			if (field < cachedHeaderValue.Length)
			{
				if (cachedHeaderValue[field] == null) {
					cachedHeaderValue[field] = BufferHelper.ReadBufferInt64 (firstSector, field * 8);
				}
				return (long)cachedHeaderValue[field];
			}
			// Otherwise return straight away
			else {
				return BufferHelper.ReadBufferInt64 (firstSector, field * 8);
			}
		}

		public void SetHeader (int field, long value)
		{
			if (isDisposed) {
				throw new ObjectDisposedException ("Block");
			}

			if (field < 0) {
				throw new IndexOutOfRangeException ();
			}

			// Update cache if this field is cached
			if (field < cachedHeaderValue.Length) {
				cachedHeaderValue[field] = value;
			}

			// Write in cached buffer
			BufferHelper.WriteBuffer ((long)value, firstSector, field * 8);
			isFirstSectorDirty = true;
		}

		public void Read (byte[] dest, int destOffset, int srcOffset, int count)
		{
			if (isDisposed) {
				throw new ObjectDisposedException ("Block");
			}

			// Validate argument
			if (false == ((count >= 0) && ((count + srcOffset) <= storage.BlockContentSize))) {
				throw new ArgumentOutOfRangeException ("Requested count is outside of src bounds: Count=" + count, "count");
			}

			if (false == ((count + destOffset) <= dest.Length)) {
				throw new ArgumentOutOfRangeException ("Requested count is outside of dest bounds: Count=" + count);
			}


			// If part of remain data belongs to the firstSector buffer
			// then copy from the firstSector first
			var dataCopied = 0;
			var copyFromFirstSector = (storage.BlockHeaderSize + srcOffset) < BlockStorage.DiskSectorSize;
			if (copyFromFirstSector)
			{
				var tobeCopied = Math.Min(BlockStorage.DiskSectorSize -storage.BlockHeaderSize -srcOffset, count);

				Buffer.BlockCopy (src: firstSector
					, srcOffset: storage.BlockHeaderSize +srcOffset
					, dst: dest
					, dstOffset: destOffset
					, count: tobeCopied);

				dataCopied += tobeCopied;
			}

			// Move the stream to correct position,
			// if there is still some data tobe copied
			if (dataCopied < count) {
				if (copyFromFirstSector) {
					stream.Position = (Id * storage.BlockSize) + BlockStorage.DiskSectorSize ;
				} else {
					stream.Position = (Id * storage.BlockSize) + storage.BlockHeaderSize + srcOffset;
				}
			}

			// Start copying until all data required is copied
			while (dataCopied < count)
			{
				var bytesToRead = Math.Min (BlockStorage.DiskSectorSize, count -dataCopied);
				var thisRead = stream.Read (dest, destOffset + dataCopied, bytesToRead);
				if (thisRead == 0) {
					throw new EndOfStreamException ();
				}
				dataCopied += thisRead;
			}
		}

		public void Write (byte[] src, int srcOffset, int dstOffset, int count)
		{
			if (isDisposed) {
				throw new ObjectDisposedException ("Block");
			}

			// Validate argument
			if (false == ((dstOffset >= 0) && ((dstOffset + count) <= storage.BlockContentSize))) {
				throw new ArgumentOutOfRangeException ("Count argument is outside of dest bounds: Count=" + count
					, "count");
			}

			if (false == ((srcOffset >= 0) && ((srcOffset + count) <= src.Length))) {
				throw new ArgumentOutOfRangeException ("Count argument is outside of src bounds: Count=" + count
					, "count");
			}

			// Write bytes that belong to the firstSector
			if ((storage.BlockHeaderSize + dstOffset) < BlockStorage.DiskSectorSize) {
				var thisWrite = Math.Min (count, BlockStorage.DiskSectorSize -storage.BlockHeaderSize -dstOffset);
				Buffer.BlockCopy (src: src
					, srcOffset: srcOffset
					, dst: firstSector
					, dstOffset: storage.BlockHeaderSize + dstOffset
					, count: thisWrite);
				isFirstSectorDirty = true;
			}

			// Write bytes that do not belong to the firstSector
			if ((storage.BlockHeaderSize + dstOffset + count) > BlockStorage.DiskSectorSize)
			{
				// Move underlying stream to correct position ready for writting
				this.stream.Position = (Id * storage.BlockSize) 
					+ Math.Max (BlockStorage.DiskSectorSize, storage.BlockHeaderSize + dstOffset);

				// Exclude bytes that have been written to the first sector
				var d = BlockStorage.DiskSectorSize -(storage.BlockHeaderSize + dstOffset);
				if (d > 0) {
					dstOffset += d;
					srcOffset += d;
					count -= d;
				}

				// Keep writing until all data is written
				var written = 0;
				while (written < count)
				{
					var bytesToWrite = (int)Math.Min (4096, count -written);
					this.stream.Write (src, srcOffset + written, bytesToWrite);
					this.stream.Flush ();
					written += bytesToWrite;
				}
			}
		}

		public override string ToString ()
		{
			return string.Format ("[Block: Id={0}, ContentLength={1}, Prev={2}, Next={3}]"
				, Id
				, GetHeader(2)
				, GetHeader(3)
				, GetHeader(0));
		}

		//
		// Protected Methods
		//

		protected virtual void OnDisposed (EventArgs e)
		{
			if (Disposed != null)
			{
				Disposed (this, e);
			}
		}

		//
		// Dispose
		//
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (disposing && !isDisposed)
			{
				isDisposed = true;

				if (isFirstSectorDirty)
				{
					this.stream.Position = (Id * storage.BlockSize);
					this.stream.Write (firstSector, 0, 4096);
					this.stream.Flush ();
					isFirstSectorDirty = false;
				}

				OnDisposed (EventArgs.Empty);
			}
		}

		~Block ()
		{
			Dispose (false);
		}
	}
}

