using System;
using System.Collections.Generic;
using System.Linq;
using FooCore;
using NUnit.Framework;

namespace FooTest
{
	[TestFixture]
	public class BTreeDeletionTest
	{
		[Test]
		public void NonFullRootNodeTest ()
		{
			// Unique tree
			var tree = new Tree<double, string>(
				new TreeMemoryNodeManager<double, string>(2, Comparer<double>.Default)
			);

			tree.Insert (1, "1");
			tree.Insert (3, "3");
			tree.Insert (6, "6");
			tree.Insert (8, "8");

			tree.Delete (6);
			Assert.IsTrue ((from t in tree.LargerThanOrEqualTo(0) select t.Item1).SequenceEqual(new double[] { 1, 3, 8 }));

			tree.Delete (3);
			Assert.IsTrue ((from t in tree.LargerThanOrEqualTo(0) select t.Item1).SequenceEqual(new double[] { 1, 8 }));

			tree.Delete (1);
			Assert.IsTrue ((from t in tree.LargerThanOrEqualTo(0) select t.Item1).SequenceEqual(new double[] { 8 }));

			tree.Delete (8);
			Assert.IsTrue ((from t in tree.LargerThanOrEqualTo(0) select t.Item1).Count() == 0);
		}

		[Test]
		public void UniqueTreeTest ()
		{
			// Unique tree
			var expectedRemain = new List<double>();
			var tree = new Tree<double, string>(
				new TreeMemoryNodeManager<double, string>(2, Comparer<double>.Default)
			);

			// Insert random numbers
			for (var i = 0; i < 1000; i++) {
				tree.Insert (i, i.ToString());
				expectedRemain.Add (i);
			}

			// Start deleting randomly
			var rnd = new Random ();
			for (var i = 0; i < 1000; i++) {
				var deleteAt = rnd.Next (0, expectedRemain.Count);
				var keyToDelete = expectedRemain[deleteAt];
				expectedRemain.RemoveAt (deleteAt);
				tree.Delete (keyToDelete);
				var remain = (from entry in tree.LargerThanOrEqualTo(0) select entry.Item1).ToArray();
				Assert.IsTrue (remain.SequenceEqual (expectedRemain));
			}

			Assert.Throws<InvalidOperationException>(delegate {
				tree.Delete (888, "888");
			});
		}

		[Test]
		public void NonUniqueTreeTestWithNoDuplicateKey ()
		{
			// Unique tree
			var expectedRemain = new List<double>();
			var tree = new Tree<double, string>(
				new TreeMemoryNodeManager<double, string>(2, Comparer<double>.Default),
				true
			);

			// Insert random numbers
			for (var i = 0; i < 1000; i++) {
				tree.Insert (i, i.ToString());
				expectedRemain.Add (i);
			}

			// Start deleting randomly
			var rnd = new Random ();
			for (var i = 0; i < 1000; i++) {
				var deleteAt = rnd.Next (0, expectedRemain.Count);
				var keyToDelete = expectedRemain[deleteAt];
				expectedRemain.RemoveAt (deleteAt);
				tree.Delete (keyToDelete, keyToDelete.ToString());
				var remain = (from entry in tree.LargerThanOrEqualTo(0) select entry.Item1).ToArray();
				Assert.IsTrue (remain.SequenceEqual (expectedRemain));
			}
		}

		[Test]
		public void NonUniqueTreeTestWithDuplicateKeys ()
		{
			
			// Unique tree
			var expectedRemain = new List<Tuple<double, string>>();
			var tree = new Tree<double, string>(
				new TreeMemoryNodeManager<double, string>(2, Comparer<double>.Default),
				true
			);
			/*
			tree.Insert (1, "A");
			tree.Insert (1, "B");
			tree.Insert (1, "C");
			tree.Insert (1, "D");
			tree.Insert (1, "E");
			tree.Insert (1, "F");
			tree.Insert (1, "G");
			tree.Insert (1, "H");

			tree.Delete (1, "E");
			var remain = (from entry in tree.LargerThanOrEqualTo(0) select entry.Item2).ToArray();
			Assert.IsTrue (remain.SequenceEqual(
				new List<string>{
					"A",
					"B",
					"C",
					"D",
					"F",
					"G",
					"H"
				}
			)); */

			// Insert random numbers
			var rnd = new Random ();
			for (var i = 0; i < 1000; i++) {
				tree.Insert (i, "A");
				expectedRemain.Add (new Tuple<double, string>(i, "A"));

				var dupCount = rnd.Next (0, 3);
				for (var t = 0; t < dupCount; t++) {
					if (t == 0) {
						tree.Insert (i, "B");
						expectedRemain.Add (new Tuple<double, string>(i, "B"));
					} else if (t == 1) {
						tree.Insert (i, "C");
						expectedRemain.Add (new Tuple<double, string>(i, "C"));
					} else {
						throw new Exception ();
					}
				}
			}


			// Start deleting randomly
			for (var i = 0; i < 1000; i++) {
				var deleteAt = rnd.Next (0, expectedRemain.Count);
				var keyToDelete = expectedRemain[deleteAt];
				expectedRemain.RemoveAt (deleteAt);
				tree.Delete (keyToDelete.Item1, keyToDelete.Item2);
				var remain = (from entry in tree.LargerThanOrEqualTo(0) orderby entry.Item1, entry.Item2 select entry).ToArray();
				Assert.IsTrue (remain.SequenceEqual (expectedRemain));
			} 
		}
	}
}

