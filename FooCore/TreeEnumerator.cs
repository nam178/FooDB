using System;
using System.Collections.Generic;
using System.Collections;

namespace FooCore
{
	public class TreeEnumerator<K, V> : IEnumerator<Tuple<K, V>>
	{
		readonly ITreeNodeManager<K, V> nodeManager;
		readonly TreeTraverseDirection direction;

		bool doneIterating = false;
		int currentEntry = 0;
		TreeNode<K, V> currentNode;

		Tuple<K, V> current;

		public TreeNode<K, V> CurrentNode {
			get {
				return currentNode;
			}
		}

		public int CurrentEntry {
			get {
				return currentEntry;
			}
		}

		object IEnumerator.Current {
			get {
				return (object)Current;
			}
		}

		public  Tuple<K, V> Current {
			get {
				return current;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Sdb.BTree.TreeEnumerator`2"/> class.
		/// </summary>
		/// <param name="nodeManager">Node manager.</param>
		/// <param name="node">Node.</param>
		/// <param name="fromIndex">From index.</param>
		/// <param name="direction">Direction.</param>
		public TreeEnumerator (ITreeNodeManager<K, V> nodeManager
			, TreeNode<K, V> node
			, int fromIndex
			, TreeTraverseDirection direction)
		{
			this.nodeManager = nodeManager;
			this.currentNode = node;
			this.currentEntry = fromIndex;
			this.direction = direction;
		}

		public bool MoveNext ()
		{
			if (doneIterating) {
				return false;
			}

			switch (this.direction) {
				case TreeTraverseDirection.Ascending:
					return MoveForward ();
				case TreeTraverseDirection.Decending:
					return MoveBackward ();
				default:
					throw new ArgumentOutOfRangeException ();
			}
		}

		bool MoveForward ()
		{
			// Leaf node, either move right or up
			if (currentNode.IsLeaf)
			{
				// First, move right
				currentEntry++;

				while (true)
				{
					// If currentEntry is valid
					// then we are done here.
					if (currentEntry < currentNode.EntriesCount) {
						current = currentNode.GetEntry (currentEntry);
						return true;
					}
					// If can't move right then move up
					else if (currentNode.ParentId != 0){
						currentEntry = currentNode.IndexInParent ();
						currentNode = nodeManager.Find (currentNode.ParentId);

						// Validate move up result
						if ((currentEntry < 0) || (currentNode == null)) {
							throw new Exception ("Something gone wrong with the BTree");
						}
					}
					// If can't move up when we are done iterating
					else {
						current = null;
						doneIterating = true;
						return false;
					}
				}
			}
			// Parent node, always move right down
			else {
				currentEntry++; // Increase currentEntry, this make firstCall to nodeManager.Find 
				                // to return the right node, but does not affect subsequence calls

				do {
					currentNode = currentNode.GetChildNode(currentEntry);
					currentEntry = 0;
				} while (false == currentNode.IsLeaf);

				current = currentNode.GetEntry (currentEntry);
				return true;
			}
		}

		bool MoveBackward ()
		{
			// Leaf node, either move right or up
			if (currentNode.IsLeaf)
			{
				// First, move left
				currentEntry--;

				while (true)
				{
					// If currentEntry is valid
					// then we are done here.
					if (currentEntry >= 0) {
						current = currentNode.GetEntry (currentEntry);
						return true;
					}
					// If can't move left then move up
					else if (currentNode.ParentId != 0){
						currentEntry = currentNode.IndexInParent () -1;
						currentNode = nodeManager.Find (currentNode.ParentId);

						// Validate move result
						if (currentNode == null) {
							throw new Exception ("Something gone wrong with the BTree");
						}
					}
					// If can't move up when we are done here
					else {
						doneIterating = true;
						current = null;
						return false;
					}
				}
			}
			// Parent node, always move left down
			else {
				do {
					currentNode = currentNode.GetChildNode(currentEntry);
					currentEntry = currentNode.EntriesCount;

					// Validate move result
					if ((currentEntry < 0) || (currentNode == null)) {
						throw new Exception ("Something gone wrong with the BTree");
					}
				} while (false == currentNode.IsLeaf);

				currentEntry -= 1;
				current = currentNode.GetEntry (currentEntry);
				return true;
			}
		}

		public void Reset ()
		{
			throw new NotSupportedException ();
		}

		public void Dispose ()
		{
			// dispose my ass
		}
	}
}

