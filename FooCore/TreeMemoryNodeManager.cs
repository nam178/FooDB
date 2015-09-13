using System;
using System.Collections.Generic;

namespace FooCore
{
	public class TreeMemoryNodeManager<K, V> : ITreeNodeManager<K, V>
	{
		readonly Dictionary<uint, TreeNode<K,V>> nodes = new Dictionary<uint, TreeNode<K, V>>();
		readonly ushort minEntriesCountPerNode;
		readonly IComparer<K> keyComparer;
		readonly IComparer<Tuple<K, V>> entryComparer;
		int idCounter = 1;
		TreeNode<K, V> rootNode;

		public IComparer<Tuple<K, V>> EntryComparer {
			get {
				return entryComparer;
			}
		}

		public ushort MinEntriesPerNode {
			get {
				return minEntriesCountPerNode;
			}
		}

		public IComparer<K> KeyComparer {
			get {
				return keyComparer;
			}
		}

		public TreeNode<K, V> RootNode {
			get {
				return rootNode;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Sdb.BTree.MemoryNodeManager`2"/> class.
		/// </summary>
		/// <param name="minEntriesCountPerNode">This multiply by 2 is the degree of the tree</param>
		/// <param name="keyComparer">Key comparer.</param>
		public TreeMemoryNodeManager (ushort minEntriesCountPerNode, IComparer<K> keyComparer)
		{
			this.keyComparer = keyComparer;
			this.entryComparer = Comparer<Tuple<K, V>>.Create((t1, t2) => {
				return this.keyComparer.Compare (t1.Item1, t2.Item1);
			});
			this.minEntriesCountPerNode = minEntriesCountPerNode;
			this.rootNode = Create (null, null);
		}

		public TreeNode<K, V> Create (IEnumerable<Tuple<K, V>> entries, IEnumerable<uint> childrenIds)
		{
			var newNode = new TreeNode<K, V>(this
				, (uint)(this.idCounter++)
				, 0
				, entries
				, childrenIds);

			nodes[newNode.Id] = newNode;

			return newNode;
		}

		public TreeNode<K, V> Find (uint id)
		{
			if (false == nodes.ContainsKey(id)) {
				throw new ArgumentException ("Node not found by id: " + id);
			}

			return nodes[id];
		}

		public TreeNode<K, V> CreateNewRoot (K key, V value, uint leftNodeId, uint rightNodeId)
		{
			var newNode = Create (new Tuple<K, V>[] { new Tuple<K, V>(key, value) }
				, new uint[] { leftNodeId, rightNodeId }
			);
			this.rootNode = newNode;
			return newNode;
		}

		public void Delete (TreeNode<K, V> target)
		{
			if (target == rootNode) {
				rootNode = null;
			}
			if (nodes.ContainsKey(target.Id)) {
				nodes.Remove (target.Id);
			}
		}

		public void MakeRoot (TreeNode<K, V> target)
		{
			this.rootNode = target;
		}

		public void MarkAsChanged (TreeNode<K, V> node)
		{
			// does nothing
		}

		public void SaveChanges ()
		{
			// does nothing
		}
	}
}

