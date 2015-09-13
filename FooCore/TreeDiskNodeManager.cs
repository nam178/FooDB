using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using log4net;

namespace FooCore
{
	public sealed class TreeDiskNodeManager<K, V> : ITreeNodeManager<K, V>
	{
		readonly IRecordStorage recordStorage;
		readonly Dictionary<uint, TreeNode<K, V>> dirtyNodes = new Dictionary<uint, TreeNode<K, V>> ();
		readonly Dictionary<uint, WeakReference<TreeNode<K, V>>> nodeWeakRefs = new Dictionary<uint, WeakReference<TreeNode<K, V>>>();
		readonly Queue<TreeNode<K, V>> nodeStrongRefs = new Queue<TreeNode<K, V>> ();
		readonly int maxStrongNodeRefs = 200;
		readonly TreeDiskNodeSerializer<K, V> serializer;
		readonly ushort minEntriesPerNode = 36;

		TreeNode<K, V> rootNode;
		int cleanupCounter = 0;

		public ushort MinEntriesPerNode {
			get {
				return minEntriesPerNode;
			}
		}

		public IComparer<Tuple<K, V>> EntryComparer {
			get;
			private set;
		}

		public IComparer<K> KeyComparer {
			get;
			private set;
		}

		public TreeNode<K, V> RootNode {
			get {
				return rootNode;
			}
		}

		//
		// Constructors
		//

		/// <summary>
		/// Construct a tree from given storage, using default comparer of key
		/// </summary>
		public TreeDiskNodeManager (ISerializer<K> keySerializer
			, ISerializer<V> valueSerializer
			, IRecordStorage nodeStorage)
			: this (keySerializer, valueSerializer, nodeStorage, Comparer<K>.Default)
		{
		}

		/// <summary>
		/// Construct a tree from given storage, using the specified comparer of key
		/// </summary>
		/// <param name="keySerializer">Tool to serialize node keys.</param>
		/// <param name="valueSerializer">Tool to serialize node values<param>
		/// <param name="recordStorage">Underlying tool for storage.</param>
		/// <param name="keyComparer">Key comparer.</param>
		public TreeDiskNodeManager (ISerializer<K> keySerializer
			, ISerializer<V> valueSerializer
			, IRecordStorage recordStorage
			, IComparer<K> keyComparer)
		{
			if (recordStorage == null)
				throw new ArgumentNullException ("nodeStorge");

			this.recordStorage = recordStorage;
			this.serializer = new TreeDiskNodeSerializer<K, V> (this, keySerializer, valueSerializer);
			this.KeyComparer = keyComparer;
			this.EntryComparer = Comparer<Tuple<K, V>>.Create ((a, b) => {
				return KeyComparer.Compare (a.Item1, b.Item1);
			});

			// The first record of nodeStorage stores id of root node,
			// if this record do not exist at the time this index instanitate,
			// then attempt to create it
			var firstBlockData = recordStorage.Find (1u);
			if (firstBlockData != null)
			{
				this.rootNode = Find (BufferHelper.ReadBufferUInt32(firstBlockData, 0));
			} else {
				this.rootNode = CreateFirstRoot ();
			}
		}

		//
		// Public Methods
		//

		public TreeNode<K, V> Create (IEnumerable<Tuple<K, V>> entries, IEnumerable<uint> childrenIds)
		{
			// Create new record
			TreeNode<K, V> node = null;

			recordStorage.Create (nodeId => {
				// Instantiate a new node
				node = new TreeNode<K, V> (this, nodeId, 0, entries, childrenIds);

				// Always keep reference to any node that we created
				OnNodeInitialized (node);

				// Return its deserialized value
				return this.serializer.Serialize (node);
			});

			if (node == null) {
				throw new Exception ("dataGenerator never called by nodeStorage");
			}

			return node;
		}

		public TreeNode<K, V> Find (uint id)
		{
			// Check if the node is being held in memory, 
			// if it does then return it
			if (nodeWeakRefs.ContainsKey(id))
			{
				TreeNode<K, V> node;
				if (nodeWeakRefs[id].TryGetTarget (out node)) {
					return node;
				} else {
					// node deallocated, remove weak reference
					nodeWeakRefs.Remove (id);
				}
			}

			// Not is not in memory, go get it
			var data = recordStorage.Find (id);
			if (data == null) {
				return  null;
			}
			var dNode = this.serializer.Deserialize (id, data);

			// Always keep reference to node we created
			OnNodeInitialized (dNode);
			return dNode;
		}

		public TreeNode<K, V> CreateNewRoot (K key, V value, uint leftNodeId, uint rightNodeId)
		{
			// Create new node as normal
			var node = Create (new Tuple<K, V>[] {
				new Tuple<K, V> (key, value)
			}, new uint[] {
				leftNodeId,
				rightNodeId
			});

			// Make it the root node
			this.rootNode = node;
			recordStorage.Update (1u, LittleEndianByteOrder.GetBytes(node.Id));

			// Then return it
			return this.rootNode;
		}

		public void MakeRoot (TreeNode<K, V> node)
		{
			this.rootNode = node;
			recordStorage.Update (1u, LittleEndianByteOrder.GetBytes(node.Id));
		}

		public void Delete (TreeNode<K, V> node)
		{
			if (node == rootNode) {
				rootNode = null;
			}
			
			recordStorage.Delete (node.Id);

			if (dirtyNodes.ContainsKey (node.Id)) {
				dirtyNodes.Remove (node.Id);
			}
		}

		public void MarkAsChanged (TreeNode<K, V> node)
		{
			if (false == dirtyNodes.ContainsKey(node.Id)) {
				dirtyNodes.Add (node.Id, node);
			}
		}

		public void SaveChanges ()
		{
			foreach (var kv in dirtyNodes)  {
				recordStorage.Update (kv.Value.Id, this.serializer.Serialize (kv.Value));
			}

			dirtyNodes.Clear ();
		}

		//
		// Private Methods
		//

		TreeNode<K, V> CreateFirstRoot ()
		{
			// Write down the id of first node into the first block
			recordStorage.Create (LittleEndianByteOrder.GetBytes((uint)2));

			// Return a new node, this node should has id of 2
			return Create (null, null);
		}

		void OnNodeInitialized (TreeNode<K, V> node)
		{
			// Keep a weak reference to it
			nodeWeakRefs.Add (node.Id, new WeakReference<TreeNode<K, V>>(node));

			// Keep a strong reference to prevent weak refs from being dellocated
			nodeStrongRefs.Enqueue (node);

			// Clean up strong refs if we been holding too many of them
			if (nodeStrongRefs.Count >= maxStrongNodeRefs) {
				while (nodeStrongRefs.Count >= (maxStrongNodeRefs/2f)) {
					nodeStrongRefs.Dequeue ();
				}
			}

			// Clean up weak refs
			if (this.cleanupCounter++ >= 1000)
			{
				this.cleanupCounter = 0;
				var tobeDeleted = new List<uint>();
				foreach (var kv in this.nodeWeakRefs)
				{
					TreeNode<K, V> target;
					if (false == kv.Value.TryGetTarget (out target)) {
						tobeDeleted.Add (kv.Key);
					}
				}

				foreach (var key in tobeDeleted)
				{
					this.nodeWeakRefs.Remove (key);
				}
			}
		}
	}
}

