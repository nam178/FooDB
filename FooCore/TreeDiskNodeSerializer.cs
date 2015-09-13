using System;
using System.Diagnostics;
using System.IO;

namespace FooCore
{
	public sealed class TreeDiskNodeSerializer<K, V>
	{
		ISerializer<K> keySerializer;
		ISerializer<V> valueSerializer;
		ITreeNodeManager<K, V> nodeManager;

		/// <summary>
		/// Construct a tree serializer that uses given key/value serializers 
		/// </summary>
		public TreeDiskNodeSerializer (ITreeNodeManager<K, V> nodeManager
			, ISerializer<K> keySerializer
			, ISerializer<V> valueSerializer)
		{
			if (nodeManager == null)
				throw new ArgumentNullException ("nodeManager");
			if (valueSerializer == null)
				throw new ArgumentNullException ("valueSerializer");
			if (keySerializer == null)
				throw new ArgumentNullException ("keySerializer");

			this.nodeManager = nodeManager;
			this.keySerializer = keySerializer;
			this.valueSerializer = valueSerializer;
		}
		
		/// <summary>
		/// Serialize given node into byte array that will then be written down to RecordStorage
		/// </summary>
		public byte[] Serialize (TreeNode<K, V> node)
		{
			if (keySerializer.IsFixedSize && valueSerializer.IsFixedSize) {
				return FixedLengthSerialize (node);				
			} else if (valueSerializer.IsFixedSize) {
				return VariableKeyLengthSerialize (node);
			} else {
				// It's rare to have varaible-length serialization of value,
				// as value always point to a record ID
				// so I dont implement it for now.
				throw new NotSupportedException ();
			}
		}

		/// <summary>
		/// Deserialize given node from data which is read from RecordStorage
		/// </summary>
		public TreeNode<K, V> Deserialize (uint assignId, byte[] record)
		{
			if (keySerializer.IsFixedSize && valueSerializer.IsFixedSize) {
				return FixedLengthDeserialize (assignId, record);
			} else if (valueSerializer.IsFixedSize) {
				return VariableKeyLengthDeserialize (assignId, record);
			} else {
				// It's rare to have varaible-length serialization of value,
				// as value always point to a record ID
				// so I dont implement it for now.
				throw new NotSupportedException ();
			}
		}

		byte[] FixedLengthSerialize (TreeNode<K, V> node)
		{
			var entrySize = this.keySerializer.Length + this.valueSerializer.Length;
			var size = 16 
				+ node.Entries.Length * entrySize
				+ node.ChildrenIds.Length * 4;
			if (size >= (1024*64)) {
				throw new Exception ("Serialized node size too large: " + size);
			}
			var buffer = new byte[size];

			// First 4 bytes of the buffer is parent id of this node
			BufferHelper.WriteBuffer (node.ParentId, buffer, 0);

			// Followed by 4 bytes of how many entries
			BufferHelper.WriteBuffer ((uint)node.EntriesCount, buffer, 4);

			// Followed by 4 bytes of how manu child references
			BufferHelper.WriteBuffer ((uint)node.ChildrenNodeCount, buffer, 8);

			// Start writing entries && child refs
			for (var i = 0; i < node.EntriesCount; i++)
			{
				// Write entry
				var entry = node.GetEntry (i);
				Buffer.BlockCopy (this.keySerializer.Serialize(entry.Item1), 0, buffer, 12 + i*entrySize, this.keySerializer.Length);
				Buffer.BlockCopy (this.valueSerializer.Serialize(entry.Item2), 0, buffer, 12 + i*entrySize + this.keySerializer.Length, this.valueSerializer.Length);
			}

			// Start writing child references
			var childrenIds = node.ChildrenIds;
			for (var i = 0; i < node.ChildrenNodeCount; i++)
			{
				// Write child refs
				BufferHelper.WriteBuffer (childrenIds [i], buffer, 12 + entrySize*node.EntriesCount + (i*4));
			}

			// Return generated buffer
			return buffer;
		}

		TreeNode<K, V> FixedLengthDeserialize (uint assignId, byte[] buffer)
		{
			var entrySize = this.keySerializer.Length + this.valueSerializer.Length;

			// First 4 bytes uint32 is parent id 
			var parentId = BufferHelper.ReadBufferUInt32 (buffer, 0);

			// Followed by 4 bytes uint32 of by how many entries this node has
			var entriesCount = BufferHelper.ReadBufferUInt32 (buffer, 4);

			// Followed by 4 bytes uint32 of how many child reference this node has
			var childrenCount  = BufferHelper.ReadBufferUInt32 (buffer, 8);

			// Deserialize entries
			var entries = new Tuple<K, V>[entriesCount];
			for (var i = 0; i < entriesCount; i++)
			{
				var key = this.keySerializer.Deserialize (buffer
					, 12 + i*entrySize
					, this.keySerializer.Length);
				var value = this.valueSerializer.Deserialize (buffer
					, 12 + i*entrySize + this.keySerializer.Length
					, this.valueSerializer.Length);
				entries[i] = new Tuple<K, V> (key, value);
			}

			// Decode child refs..
			var children = new uint[childrenCount];
			for (var i = 0; i < childrenCount; i++)
			{
				// Decode child refs..
				children[i] = BufferHelper.ReadBufferUInt32 (buffer, (int)(12 + entrySize*entriesCount + (i*4)));
			}

			// Reconstuct the node
			return new TreeNode<K, V> (nodeManager, assignId, parentId, entries, children);
		}

		TreeNode<K, V> VariableKeyLengthDeserialize (uint assignId, byte[] buffer)
		{
			// First 4 bytes uint32 is parent id 
			var parentId = BufferHelper.ReadBufferUInt32 (buffer, 0);

			// Followed by 4 bytes uint32 of by how many entries this node has
			var entriesCount = BufferHelper.ReadBufferUInt32 (buffer, 4);

			// Followed by 4 bytes uint32 of how many child reference this node has
			var childrenCount  = BufferHelper.ReadBufferUInt32 (buffer, 8);

			// Deserialize entries
			var entries = new Tuple<K, V>[entriesCount];
			var p = 12;
			for (var i = 0; i < entriesCount; i++)
			{
				var keyLength = BufferHelper.ReadBufferInt32 (buffer, p);
				var key = this.keySerializer.Deserialize (buffer
					, p + 4
					, keyLength);
				var value = this.valueSerializer.Deserialize (buffer
					, p + 4 + keyLength
					, this.valueSerializer.Length);

				entries[i] = new Tuple<K, V> (key, value);

				p += 4 + keyLength + valueSerializer.Length;
			}

			// Decode child refs..
			var children = new uint[childrenCount];
			for (var i = 0; i < childrenCount; i++)
			{
				// Decode child refs..
				children[i] = BufferHelper.ReadBufferUInt32 (buffer, (int)(p + (i*4)));
			}

			// Reconstuct the node
			return new TreeNode<K, V> (nodeManager, assignId, parentId, entries, children);
		}

		byte[] VariableKeyLengthSerialize (TreeNode<K, V> node)
		{
			using (var m = new MemoryStream())
			{
				// 4 bytes uint parent id
				m.Write (LittleEndianByteOrder.GetBytes((uint)node.ParentId), 0, 4);
				// Followed by 4 bytes of how many entries
				m.Write (LittleEndianByteOrder.GetBytes((uint)node.EntriesCount), 0, 4);
				// Followed by 4 bytes of how manu child references
				m.Write (LittleEndianByteOrder.GetBytes((uint)node.ChildrenNodeCount), 0, 4);

				// Write entries..
				for (var i = 0; i < node.EntriesCount; i++)
				{
					// Write entry
					var entry = node.GetEntry (i);
					var key = this.keySerializer.Serialize(entry.Item1);
					var value = this.valueSerializer.Serialize(entry.Item2);

					m.Write (LittleEndianByteOrder.GetBytes((int)key.Length), 0, 4);
					m.Write (key, 0, key.Length);
					m.Write (value, 0, value.Length);
				}

				// Write child refs..
				var childrenIds = node.ChildrenIds;
				for (var i = 0; i < node.ChildrenNodeCount; i++)
				{
					m.Write (LittleEndianByteOrder.GetBytes((uint)childrenIds[i]), 0, 4);
				}

				return m.ToArray();
			}
		}
	}
}

