using System;
using System.Collections.Generic;
using System.Collections;

namespace FooCore
{
	public class TreeTraverser<K, V> : IEnumerable<Tuple<K, V>>
	{
		readonly TreeNode<K, V> fromNode;
		readonly int fromIndex;
		readonly TreeTraverseDirection direction;
		readonly ITreeNodeManager<K, V> nodeManager;

		/// <summary>
		/// Initializes a new instance of the <see cref="Sdb.BTree.TreeTraverser`2"/> class.
		/// </summary>
		/// <param name="nodeManager">Node manager.</param>
		/// <param name="fromNode">From node.</param>
		/// <param name="fromIndex">From index.</param>
		/// <param name="direction">Direction.</param>
		public TreeTraverser (ITreeNodeManager<K, V> nodeManager
			, TreeNode<K,V> fromNode
			, int fromIndex
			, TreeTraverseDirection direction)
		{
			if (fromNode == null)
				throw new ArgumentNullException ("fromNode");

			this.direction = direction;
			this.fromIndex = fromIndex;
			this.fromNode = fromNode;
			this.nodeManager = nodeManager;
		}

		IEnumerator<Tuple<K, V>> IEnumerable<Tuple<K, V>>.GetEnumerator ()
		{
			return new TreeEnumerator<K, V> (nodeManager, fromNode, fromIndex, direction);
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			// Use the generic version
			return ((IEnumerable<Tuple<K, V>>)this).GetEnumerator();
		}
	}
}

