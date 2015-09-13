using System;
using System.Collections.Generic;

namespace FooCore
{
	public interface ITreeNodeManager<K, V>
	{
		/// <summary>
		/// Minimum number of entries per node. Maximum number of entries
		/// must be equal to MinEntriesCountPerNode*2
		/// </summary>
		ushort MinEntriesPerNode {
			get;
		}

		/// <summary>
		/// Get the comparer that used to compare keys
		/// </summary>
		IComparer<K> KeyComparer {
			get;
		}

		/// <summary>
		/// Get the comparer that used to compare entries,
		/// This must use KeyComparer declared above
		/// </summary>
		IComparer<Tuple<K, V>> EntryComparer {
			get;
		}

		/// <summary>
		/// Get the root node. Root node must be cached, because it is always get called
		/// </summary>
		TreeNode<K, V> RootNode {
			get;
		}

		/// <summary>
		/// Creates a new node that carries given entires, and keep references to given children nodes
		/// </summary>
		/// <param name="entries">Entries.</param>
		/// <param name="childrenIds">Children identifiers.</param>
		TreeNode<K, V> Create (IEnumerable<Tuple<K, V>> entries, IEnumerable<uint> childrenIds);

		/// <summary>
		/// Find a node by its numeric ID
		/// </summary>
		TreeNode<K, V> Find (uint id);

		/// <summary>
		/// Called by the tree to split a current root node to a new root node.
		/// </summary>
		/// <param name="leftNodeId">Left node identifier.</param>
		/// <param name="rightNodeId">Right node identifier.</param>
		TreeNode<K, V> CreateNewRoot (K key, V value, uint leftNodeId, uint rightNodeId);

		/// <summary>
		/// Make given node tobe root straigh away
		/// </summary>
		void MakeRoot (TreeNode<K, V> node);

		/// <summary>
		/// Mark a given node as dirty tobe saved later
		/// </summary>
		/// <param name="node">Node.</param>
		void MarkAsChanged (TreeNode<K, V> node);

		/// <summary>
		/// Delete specified node straight away
		/// </summary>
		void Delete (TreeNode<K, V> node);

		/// <summary>
		/// Write all dirty nodes to disk
		/// </summary>
		void SaveChanges ();
	}
}

