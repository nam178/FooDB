using System;
using System.Collections.Generic;
using System.Linq;
using FooCore;
using NUnit.Framework;

namespace UnitTest
{
	[TestFixture]
	public class BTreeTest
	{
		[Test]
		public void EmptyTreeTest ()
		{
			var tree = new Tree<int, string>(new TreeMemoryNodeManager<int, string>(2, Comparer<int>.Default));

			Assert.AreEqual (0, (from node in tree.LargerThan(7) select node).Count());
			Assert.AreEqual (0, (from node in tree.LargerThanOrEqualTo(7) select node).Count());
			Assert.AreEqual (0, (from node in tree.LessThan(7) select node).Count());
			Assert.AreEqual (0, (from node in tree.LessThanOrEqualTo(7) select node).Count());

		}

		[Test]
		public void NonFullRootNodeTest ()
		{
			var tree = new Tree<int, string>(new TreeMemoryNodeManager<int, string>(2, Comparer<int>.Default));

			tree.Insert (1, "1");

			Assert.AreEqual (0, (from node in tree.LargerThan(7) select node).Count());
			Assert.AreEqual (0, (from node in tree.LargerThanOrEqualTo(7) select node).Count());
			Assert.AreEqual (1, (from node in tree.LessThan(7) select node).FirstOrDefault().Item1);
			Assert.AreEqual (1, (from node in tree.LessThanOrEqualTo(7) select node).FirstOrDefault().Item1);
			Assert.AreEqual (1, (from node in tree.LessThanOrEqualTo(1) select node).FirstOrDefault().Item1);
			Assert.AreEqual (0, (from node in tree.LessThan(1) select node).Count());

			tree.Insert (5, "2");
			tree.Insert (9, "9");

			// 1,5,9

			Assert.IsTrue ((from node in tree.LessThanOrEqualTo(9) select node.Item1).SequenceEqual(new int[] { 9, 5, 1 }));
			Assert.IsTrue ((from node in tree.LessThan(9) select node.Item1).SequenceEqual(new int[] { 5, 1 }));
			Assert.IsTrue ((from node in tree.LargerThanOrEqualTo(5) select node.Item1).SequenceEqual(new int[] { 5, 9 }));
			Assert.IsTrue ((from node in tree.LargerThan(5) select node.Item1).SequenceEqual(new int[] { 9 }));


			Assert.Throws<TreeKeyExistsException>(delegate {
				tree.Insert (9, "9");
			});
		}

		[Test]
		public void SplitRootNodeTest ()
		{
			// Insert too much at root node that it overvlow
			var tree = new Tree<int, string>(new TreeMemoryNodeManager<int, string>(2, Comparer<int>.Default));

			tree.Insert (0, "00");
			tree.Insert (1, "11");
			tree.Insert (2, "22");
			tree.Insert (3, "33");
			tree.Insert (4, "44");

			// Validate the tree
			Assert.IsNull (tree.Get(8));
			Assert.IsNull (tree.Get(-1));
			Assert.IsNull (tree.Get(99));

			Assert.NotNull (tree.Get (0));
			Assert.NotNull (tree.Get (1));
			Assert.NotNull (tree.Get (2));
			Assert.NotNull (tree.Get (3));
			Assert.NotNull (tree.Get (4));

			Assert.IsTrue ((from tpl in tree.LargerThanOrEqualTo(4) select tpl.Item1).SequenceEqual(new int[]{ 4 }));
			Assert.IsTrue ((from tpl in tree.LargerThan(1) select tpl.Item1).SequenceEqual(new int[]{ 2, 3, 4 }));
			Assert.IsTrue ((from tpl in tree.LessThan(3) select tpl.Item1).SequenceEqual(new int[]{ 2,1,0 }));
			Assert.IsTrue ((from tpl in tree.LessThanOrEqualTo(3) select tpl.Item1).SequenceEqual(new int[]{ 3, 2,1,0 }));
		}

		[Test]
		public void SplitChildNodeTest ()
		{
			// Insert too much at root node that it overvlow
			var tree = new Tree<int, string>(new TreeMemoryNodeManager<int, string>(2, Comparer<int>.Default));

			for (var i = 0; i <= 100; i++)
			{
				tree.Insert (i, i.ToString());
				var result = (from tuple in tree.LargerThanOrEqualTo(0) select tuple.Item1).ToList();
				Assert.AreEqual (i + 1, result.Count);
			}
		}

		[Test]
		public void RandomTest ()
		{
			var tree = new Tree<int, string>(new TreeMemoryNodeManager<int, string>(2, Comparer<int>.Default));

			// Generate a random sequence
			var seq = new List<int>();
			var rnd = new Random (198);

			// Insert random sequence 
			for (var i = 0; i < 1000; i++)
			{
				var n = rnd.Next (0, 2000);
				while (seq.Contains(n) == true) {
					n = rnd.Next (0, 2000);
				}

				tree.Insert (n, n.ToString());
				seq.Add (n);
			}

			// Validate result
			var sortedSeq = seq.FindAll (t => true);
			sortedSeq.Sort (Comparer<int>.Default);
			Assert.IsTrue (sortedSeq.SequenceEqual(from tuple in tree.LargerThanOrEqualTo(0) select tuple.Item1));

			// Randomly query
			for (var n = 0; n <= 100; n++)
			{
				var number = n = rnd.Next (0, 2000);
				var lastD = 0;
				foreach (var d in from tuple in tree.LargerThanOrEqualTo(number) select tuple.Item1)
				{
					Assert.IsTrue (d >= number);
					Assert.IsTrue (d > lastD);
					lastD = d;
				}


				lastD = 0;
				foreach (var d in from tuple in tree.LargerThan(number) select tuple.Item1)
				{
					Assert.IsTrue (d > number);
					Assert.IsTrue (d > lastD);
					lastD = d;
				}

				lastD = 999999;
				foreach (var d in from tuple in tree.LessThan(number) select tuple.Item1)
				{
					Assert.IsTrue (d < number);
					Assert.IsTrue (d < lastD);
					lastD = d;
				}

				lastD = 999999;
				foreach (var d in from tuple in tree.LessThanOrEqualTo(number) select tuple.Item1)
				{
					Assert.IsTrue (d <= number);
					Assert.IsTrue (d < lastD);
					lastD = d;
				}
			}
		}
	}
}

