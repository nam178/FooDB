using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FooCore
{
	public class TreeNode<K, V>
	{
		protected uint id = 0;
		protected uint parentId;
		protected readonly ITreeNodeManager<K, V> nodeManager;
		protected readonly List<uint> childrenIds;
		protected readonly List<Tuple<K, V>> entries;

		//
		// Properties
		//

		public K MaxKey {
			get {
				return entries[entries.Count-1].Item1;
			}
		}

		public K MinKey {
			get {
				return entries[0].Item1;
			}
		}

		public bool IsEmpty {
			get {
				return entries.Count == 0;
			}
		}

		public bool IsLeaf {
			get {
				return childrenIds.Count == 0;
			}
		}

		public bool IsOverflow {
			get {
				return entries.Count > (nodeManager.MinEntriesPerNode*2);
			}
		}

		public int EntriesCount {
			get {
				return entries.Count;
			}
		}

		public int ChildrenNodeCount {
			get {
				return childrenIds.Count;
			}
		}

		public uint ParentId {
			get {
				return parentId;
			}
			private set {
				parentId = value;
				nodeManager.MarkAsChanged (this);
			}
		}

		public uint[] ChildrenIds {
			get {
				return childrenIds.ToArray();
			}
		}

		public Tuple<K, V>[] Entries {
			get {
				return entries.ToArray();
			}
		}

		/// <summary>
		/// Id of this node, assigned by node manager. Node never change its id itself
		/// </summary>
		public uint Id {
			get {
				return id;
			}
		}

		//
		// Constructors
		//

		/// <summary>
		/// Initializes a new instance of the <see cref="Sdb.BTree.Node`2"/> class.
		/// </summary>
		/// <param name="branchingFactor">Branching factor.</param>
		/// <param name="nodeManager">Node manager.</param>
		public TreeNode (ITreeNodeManager<K, V> nodeManager
			, uint id
			, uint parentId
			, IEnumerable<Tuple<K, V>> entries = null
			, IEnumerable<uint> childrenIds = null)
		{
			if (nodeManager == null)
				throw new ArgumentNullException (nameof(nodeManager));

			this.id = id;
			this.parentId = parentId;

			// Setting up readonly attributes
			this.nodeManager = nodeManager;
			this.childrenIds = new List<uint>();
			this.entries = new List<Tuple<K, V>> (this.nodeManager.MinEntriesPerNode*2);

			// Loading up data
			if (entries != null) {
				this.entries.AddRange (entries);
			}

			if (childrenIds != null) {
				this.childrenIds.AddRange (childrenIds);
			}
		}

		//
		// Public Methods
		//

		/// <summary>
		/// Remove an entry from this instance. 
		/// </summary>
		public void Remove (int removeAt)
		{
			// Validate argument
			if (false == (removeAt >= 0) && (removeAt < this.entries.Count)) {
				throw new ArgumentOutOfRangeException ();
			}

			// http://webdocs.cs.ualberta.ca/~holte/T26/del-b-tree.html
			// https://en.wikipedia.org/wiki/B-tree#Deletion
			// If this is a node leave, flagged entry will be removed
			if (IsLeaf) {
				// Step 1: Remove X from the current node. 
				// Being a leaf node there are no subtrees to worry about.
				entries.RemoveAt (removeAt);
				nodeManager.MarkAsChanged (this);

				// If the removal does not cause underflow then we are done here
				if ((EntriesCount >= nodeManager.MinEntriesPerNode) || (parentId == 0)) {
					return;
				}
				// Otherwise, rebalance this node
				else {
					Rebalance ();
				}
			}
			// If the value to be deleted does not occur in a leaf,
			// we replace it with the largest value in its left subtree 
			// and then proceed to delete that value from the node that 
			// originally contained it
			else {
				// Grab the largest entry on the left subtree
				var leftSubTree = nodeManager.Find (this.childrenIds[removeAt]);
				TreeNode<K, V> largestNode;  int largestIndex;
				leftSubTree.FindLargest (out largestNode, out largestIndex);
				var replacementEntry = largestNode.GetEntry (largestIndex);

				// REplace it
				this.entries[removeAt] = replacementEntry;
				nodeManager.MarkAsChanged (this);

				// Remove it from the node we took it from
				largestNode.Remove (largestIndex);
			}
		}

		/// <summary>
		/// Get this node's index in its parent
		/// </summary>
		public int IndexInParent ()
		{
			var parent = nodeManager.Find (parentId);
			if (parent == null) {
				throw new Exception ("IndexInParent fails to find parent node of " + id);
			}
			var childrenIds = parent.ChildrenIds;
			for (var i = 0; i < childrenIds.Length; i++)
			{
				if (childrenIds[i] == id)
				{
					return i;
				}
			}

			throw new Exception ("Failed to find index of node " + id + " in its parent");
		}

		/// <summary>
		/// Find the largest entry on this subtree and output it to specified parameters
		/// </summary>
		public void FindLargest (out TreeNode<K, V> node, out int index)
		{
			// If this node is leave then we reached
			// the bottom of the tree, return this node's max value
			if (IsLeaf) {
				node = this;
				index = this.entries.Count -1;
				return;
			}
			// Otherwise, keep drilling down to the right
			else {
				var rightMostNode = nodeManager.Find(this.childrenIds[this.childrenIds.Count -1]);
				rightMostNode.FindLargest (out node, out index);
			}
		}

		/// <summary>
		/// Find the smallest entry on this subtree and output it to specified parameters
		/// </summary>
		public void FindSmallest (out TreeNode<K, V> node, out int index)
		{
			// If this node is leave then we reached
			// the bottom of the tree, return this node's max value
			if (IsLeaf) {
				node = this;
				index = 0;
				return;
			}
			// Otherwise, keep drilling down to the right
			else {
				var leftMostNode = nodeManager.Find(this.childrenIds[0]);
				leftMostNode.FindSmallest (out node, out index);
			}
		}

		public void InsertAsLeaf (K key, V value, int insertPosition)
		{
			Debug.Assert (IsLeaf, "Call this method on leaf node only");

			entries.Insert (insertPosition, new Tuple<K, V>(key, value));
			nodeManager.MarkAsChanged (this);
		}

		public void InsertAsParent (K key, V value, uint leftReference, uint rightReference, out int insertPosition)
		{
			Debug.Assert (false == IsLeaf, "Call this method on non-leaf node only");

			// Find insert position
			insertPosition = BinarySearchEntriesForKey (key);
			insertPosition = insertPosition >= 0 ? insertPosition : ~insertPosition;

			// Insert entry first
			entries.Insert (insertPosition, new Tuple<K, V>(key, value));

			// Then insert and update child references
			childrenIds.Insert (insertPosition, leftReference);
			childrenIds[insertPosition+1] = rightReference;

			// This node has been changed as we modified entries and children references
			nodeManager.MarkAsChanged (this);
		}

		/// <summary>
		/// Split this node in half
		/// </summary>
		public void Split (out TreeNode<K, V> outLeftNode, out TreeNode<K, V> outRightNode)
		{
			Debug.Assert (IsOverflow, "Calling Split when node is not overflow");

			var halfCount = this.nodeManager.MinEntriesPerNode;
			var middleEntry = entries[halfCount];

			// Create new node that holds all values
			// that larger than the middle one
			var rightEntries = new Tuple<K, V>[halfCount];
			var rightChildren = (uint[])null;
			entries.CopyTo (halfCount+1, rightEntries, 0, rightEntries.Length);
			if (false == IsLeaf) {
				rightChildren = new uint[halfCount + 1];
				childrenIds.CopyTo (halfCount+1, rightChildren, 0, rightChildren.Length);
			}
			var newRightNode = nodeManager.Create (rightEntries, rightChildren);

			// As we moved half of the children node to the new parent,
			// the ParentId property of these nodes also need tobe updated
			if (rightChildren != null) {
				foreach (var childId in rightChildren) {
					nodeManager.Find (childId).ParentId = newRightNode.Id;
				}
			}

			// Remove all values that larger than the middle 
			// one from current node
			entries.RemoveRange (halfCount);

			if (false == IsLeaf) {
				childrenIds.RemoveRange (halfCount + 1);
			}

			// alright now we have 2 nodes,
			// insert the middle element to parent node.
			var parent = parentId == 0 ? null : nodeManager.Find(parentId);

			// If there is no parent,
			// then the middle element becomes the new root node
			if (parent == null) {
				parent = this.nodeManager.CreateNewRoot (middleEntry.Item1
					, middleEntry.Item2
					, id
					, newRightNode.Id);
				this.ParentId = parent.Id;
				newRightNode.ParentId = parent.Id;
			}
			// Otherwise, elevate the middle element
			// to the parent node
			else {
				int insertPosition;
				parent.InsertAsParent (middleEntry.Item1
					, middleEntry.Item2
					, id
					, newRightNode.Id
					, out insertPosition);
				
				newRightNode.ParentId = parent.id;

				// If parent is overflow, split and update reference
				if (parent.IsOverflow) {
					TreeNode<K, V> left, right;
					parent.Split (out left, out right);
				}
			}

			// Output the node that 
			outLeftNode = this;
			outRightNode = newRightNode;

			// Mark this node as changed
			nodeManager.MarkAsChanged (this);
		}

		/// <summary>
		/// Perform a binary search on entries
		/// </summary>
		public int BinarySearchEntriesForKey (K key)
		{
			return entries.BinarySearch (new Tuple<K, V>(key, default(V)), this.nodeManager.EntryComparer);
		}

		/// <summary>
		/// Perform binary search on entries, but if there are multiple occurences,
		/// return either last or first occurence based on firstOccurrence param
		/// </summary>
		/// <param name="firstOccurence">If set to <c>true</c> first occurence.</param>
		public int BinarySearchEntriesForKey (K key, bool firstOccurence)
		{
			if (firstOccurence) {
				return entries.BinarySearchFirst (new Tuple<K, V>(key, default(V)), this.nodeManager.EntryComparer);
			} else {
				return entries.BinarySearchLast (new Tuple<K, V>(key, default(V)), this.nodeManager.EntryComparer);
			}
		}

		/// <summary>
		/// Get a children node by its internal position to this node
		/// </summary>
		public TreeNode<K, V> GetChildNode (int atIndex)
		{
			return nodeManager.Find (childrenIds[atIndex]);
		}

		/// <summary>
		/// Get a Key-Value entry inside this node
		/// </summary>
		public Tuple<K, V> GetEntry (int atIndex)
		{
			return entries[atIndex];
		}

		/// <summary>
		/// Check if there is an entry at given index
		/// </summary>
		public bool EntryExists (int atIndex)
		{
			return atIndex < entries.Count;
		}

		public override string ToString ()
		{
			if (IsLeaf) {
				var numbers = (from tuple in this.entries select tuple.Item1.ToString()).ToArray ();
				return string.Format ("[Node: Id={0}, ParentId={1}, Entries={2}]"
					, Id
					, ParentId
					, String.Join (",", numbers));	
			} else {
				var numbers = (from tuple in this.entries select tuple.Item1.ToString()).ToArray ();
				var ids = (from id in this.childrenIds select id.ToString()).ToArray ();
				return string.Format ("[Node: Id={0}, ParentId={1}, Entries={2}, Children={3}]"
					, Id
					, ParentId
					, String.Join (",", numbers)
					, String.Join (",", ids));
			}
		}

		//
		// Private Methods
		//

		/// <summary>
		/// Rebalance this node after an element has been removed causing it to underflow
		/// </summary>
		void Rebalance()
		{
			// If the deficient node's right sibling exists and has more 
			// than the minimum number of elements, then rotate left
			var indexInParent = IndexInParent ();
			var parent = nodeManager.Find (parentId);
			var rightSibling = ((indexInParent + 1) < parent.ChildrenNodeCount) ? parent.GetChildNode (indexInParent+1) : null;
			if ((rightSibling != null) && (rightSibling.EntriesCount > nodeManager.MinEntriesPerNode))
			{
				// Copy the separator from the parent to the end of the deficient node 
				// (the separator moves down; the deficient node now has the minimum number of elements)
				entries.Add (parent.GetEntry (indexInParent));

				// Replace the separator in the parent with the first element of the right sibling 
				// (right sibling loses one node but still has at least the minimum number of elements)
				parent.entries[indexInParent] = rightSibling.entries[0];
				rightSibling.entries.RemoveAt(0);

				// Move the first child reference from right sibling to me.
				if (false == rightSibling.IsLeaf) {
					// First, update parentId of the child that will be moved
					var n = nodeManager.Find (rightSibling.childrenIds[0]);
					n.parentId = this.id;
					nodeManager.MarkAsChanged (n);	
					// Then move it
					childrenIds.Add (rightSibling.childrenIds[0]);
					rightSibling.childrenIds.RemoveAt (0);
				}

				// The tree is now balanced
				nodeManager.MarkAsChanged (this);
				nodeManager.MarkAsChanged (parent);
				nodeManager.MarkAsChanged (rightSibling);
				return;
			}

			// Otherwise, if the deficient node's left sibling exists and has more
			// than the minimum number of elements, then rotate right
			var leftSibling = ((indexInParent -1) >= 0) ? parent.GetChildNode (indexInParent -1) : null;
			if ((leftSibling != null) && (leftSibling.EntriesCount > nodeManager.MinEntriesPerNode))
			{
				// Copy the separator from the parent to the start of the deficient node 
				// (the separator moves down; deficient node now has the minimum number of elements)
				entries.Insert (0, parent.GetEntry(indexInParent -1));

				// Replace the separator in the parent with the last element 
				// of the left sibling (left sibling loses one node but still has 
				// at least the minimum number of elements)
				parent.entries[indexInParent -1] = leftSibling.entries[leftSibling.entries.Count -1];
				leftSibling.entries.RemoveAt (leftSibling.entries.Count -1);

				// Move the last child reference from the left sibing to me.
				// First, update parentId of the child that will be moved.
				if (false == IsLeaf) {
					var n = nodeManager.Find (leftSibling.childrenIds[leftSibling.childrenIds.Count - 1]);
					n.parentId = this.id;
					nodeManager.MarkAsChanged (n);
					// Then move it
					childrenIds.Insert (0, leftSibling.childrenIds[leftSibling.childrenIds.Count - 1]);
					leftSibling.childrenIds.RemoveAt (leftSibling.childrenIds.Count - 1);
				}

				// The tree is now balanced;
				nodeManager.MarkAsChanged (this);
				nodeManager.MarkAsChanged (parent);
				nodeManager.MarkAsChanged (leftSibling);
				return;
			}

			// Otherwise, if both immediate siblings have only the minimum number of elements, 
			// then merge with a sibling sandwiching their separator taken off from their parent
			var leftChild = rightSibling != null ? this : leftSibling;
			var rightChild = rightSibling != null ? rightSibling : this;
			var seperatorParentIndex = rightSibling != null ? indexInParent : (indexInParent-1);

			// Step 1:
			// Copy the separator to the end of the left node (the left node may be the
			// deficient node or it may be the sibling with the minimum number of elements)
			leftChild.entries.Add (parent.GetEntry (seperatorParentIndex));

			// Move all elements from the right node to the left 
			// node (the left node now has the maximum number of elements, and the right node – empty)
			leftChild.entries.AddRange(rightChild.entries);
			leftChild.childrenIds.AddRange (rightChild.childrenIds);
			// Update parent id of the children that has been moved from rightChild to leftChild
			foreach (var id in rightChild.childrenIds)
			{
				var n = nodeManager.Find (id);
				n.parentId = leftChild.id;
				nodeManager.MarkAsChanged (n);;
			}

			// Remove the separator from the parent along with its
			// empty right child (the parent loses an element)
			parent.entries.RemoveAt (seperatorParentIndex);
			parent.childrenIds.RemoveAt (seperatorParentIndex + 1);
			nodeManager.Delete (rightChild);

			// If the parent is the root and now has no elements, 
			// then free it and make the merged node the new root (tree becomes shallower)
			if (parent.parentId == 0 && parent.EntriesCount == 0) {
				leftChild.parentId = 0;
				nodeManager.MarkAsChanged (leftChild); // Changed left one
				nodeManager.MakeRoot (leftChild);      
				nodeManager.Delete (parent);           // Deleted parent
			}
			// Otherwise, if the parent has fewer than 
			// the required number of elements, then rebalance the parent
			else if ((parent.parentId != 0) && (parent.EntriesCount < nodeManager.MinEntriesPerNode)) {
				nodeManager.MarkAsChanged (leftChild);  // Changed left one
				nodeManager.MarkAsChanged (parent);     // Changed parent
				parent.Rebalance ();
			} else {
				nodeManager.MarkAsChanged (leftChild);  // Changed left one
				nodeManager.MarkAsChanged (parent);     // Changed parent
			}
		}
	}
}