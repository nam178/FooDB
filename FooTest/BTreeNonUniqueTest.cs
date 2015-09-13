using System;
using System.Collections.Generic;
using System.Linq;
using FooCore;
using NUnit.Framework;

namespace FooTest
{
	[TestFixture]
	public class BTreeNonUniqueTest
	{
		[Test]
		public void NonFullRootNodeTest ()
		{
			var tree = new Tree<int, string>(new TreeMemoryNodeManager<int, string>(2, Comparer<int>.Default),
				allowDuplicateKeys: true);

			tree.Insert (1, "1");

			Assert.AreEqual (0, (from node in tree.LargerThan(7) select node).Count());
			Assert.AreEqual (0, (from node in tree.LargerThanOrEqualTo(7) select node).Count());
			Assert.AreEqual (1, (from node in tree.LessThan(7) select node).FirstOrDefault().Item1);
			Assert.AreEqual (1, (from node in tree.LessThanOrEqualTo(7) select node).FirstOrDefault().Item1);
			Assert.AreEqual (1, (from node in tree.LessThanOrEqualTo(1) select node).FirstOrDefault().Item1);
			Assert.AreEqual (0, (from node in tree.LessThan(1) select node).Count());

			tree.Insert (5, "5");
			tree.Insert (9, "9");

			// 1,5,9

			Assert.IsTrue ((from node in tree.LessThanOrEqualTo(9) select node.Item1).SequenceEqual(new int[] { 9, 5, 1 }));
			Assert.IsTrue ((from node in tree.LessThan(9) select node.Item1).SequenceEqual(new int[] { 5, 1 }));
			Assert.IsTrue ((from node in tree.LargerThanOrEqualTo(5) select node.Item1).SequenceEqual(new int[] { 5, 9 }));
			Assert.IsTrue ((from node in tree.LargerThan(5) select node.Item1).SequenceEqual(new int[] { 9 }));


			Assert.DoesNotThrow(delegate {
				tree.Insert (5, "5.1");
			});

			// Iterating test
			var found = (from node in tree.LargerThanOrEqualTo(5) select node.Item2).ToList ();
			Assert.AreEqual (3, found.Count);
			Assert.IsTrue (found.Contains("5.1"));
			Assert.IsTrue (found.Contains("5"));
		}

		[Test]
		public void BreakNodeTest ()
		{
			// Generate a random sequence
			var seq = new List<double>();
			var rnd = new Random ();
			for (var i = 0; i < 1000; i++) {
				seq.Add (rnd.Next(0, 10));
			}

			// Start inserting
			var tree = new Tree<double, string>(
				new TreeMemoryNodeManager<double, string>(2, Comparer<double>.Default),
				allowDuplicateKeys: true
			);
			foreach (var t in seq) {
				tree.Insert (t, t.ToString());
			}

			// Test 1
			{
				var keys = (from t in tree.LargerThanOrEqualTo(0) select t.Item1).ToArray();
				Assert.AreEqual (seq.Count, keys.Length);
				foreach (var key in keys) {
					Assert.AreEqual (OccurencesInList(key, seq), OccurencesInList(key, keys));
				}
			}

			// Test 2
			{
				var keys = (from t in tree.LargerThanOrEqualTo(7) select t.Item1).ToArray();
				Assert.AreEqual ((from t in seq where t >= 7 select t).Count(), keys.Length);
				foreach (var key in keys) {
					Assert.AreEqual (OccurencesInList(key, from t in seq where t >= 7 select t), OccurencesInList(key, keys));
				}
			}

			// Test 3
			{
				var keys = (from t in tree.LargerThan(7) select t.Item1).ToArray();
				Assert.AreEqual ((from t in seq where t > 7 select t).Count(), keys.Length);
				foreach (var key in keys) {
					Assert.AreEqual (OccurencesInList(key, from t in seq where t > 7 select t), OccurencesInList(key, keys));
				}
			}

			// Test 4
			{
				var keys = (from t in tree.LargerThanOrEqualTo(7.5) select t.Item1).ToArray();
				Assert.AreEqual ((from t in seq where t >= 7.5 select t).Count(), keys.Length);
				foreach (var key in keys) {
					Assert.AreEqual (OccurencesInList(key, from t in seq where t >= 7 select t), OccurencesInList(key, keys));
				}
			}

			// Test 5
			{
				var keys = (from t in tree.LargerThan(7.5) select t.Item1).ToArray();
				Assert.AreEqual ((from t in seq where t > 7.5 select t).Count(), keys.Length);
				foreach (var key in keys) {
					Assert.AreEqual (OccurencesInList(key, from t in seq where t > 7.5 select t), OccurencesInList(key, keys));
				}
			}

			// Test 6
			{
				var keys = (from t in tree.LessThan(7) select t.Item1).ToArray();
				Assert.AreEqual ((from t in seq where t < 7 select t).Count(), keys.Length);
				foreach (var key in keys) {
					Assert.AreEqual (OccurencesInList(key, from t in seq where t < 7 select t), OccurencesInList(key, keys));
				}
			}

			// Test 7
			{
				var keys = (from t in tree.LessThan(7.5) select t.Item1).ToArray();
				Assert.AreEqual ((from t in seq where t < 7.5 select t).Count(), keys.Length);
				foreach (var key in keys) {
					Assert.AreEqual (OccurencesInList(key, from t in seq where t < 7.5 select t), OccurencesInList(key, keys));
				}
			}

			// Test 8
			{
				var keys = (from t in tree.LessThanOrEqualTo(7) select t.Item1).ToArray();
				Assert.AreEqual ((from t in seq where t <= 7 select t).Count(), keys.Length);
				foreach (var key in keys) {
					Assert.AreEqual (OccurencesInList(key, from t in seq where t <= 7 select t), OccurencesInList(key, keys));
				}
			}

			// Test 9
			{
				var keys = (from t in tree.LessThanOrEqualTo(7.5) select t.Item1).ToArray();
				Assert.AreEqual ((from t in seq where t <= 7.5 select t).Count(), keys.Length);
				foreach (var key in keys) {
					Assert.AreEqual (OccurencesInList(key, from t in seq where t <= 7.5 select t), OccurencesInList(key, keys));
				}
			}
		}

		int OccurencesInList (double value, IEnumerable<double> list)
		{
			return (from t in list where t == value select t).Count();
		}
	}
}

