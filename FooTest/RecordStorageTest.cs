using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using FooCore;

namespace UnitTest
{
	[TestFixture]
	public class RecordStorageTest
	{
		[Test]
		public void TestUpdateEqualSizeBlock ()
		{
			var recordStorage = new RecordStorage (new BlockStorage(new MemoryStream(), 8192, 48));
			var x1 = UnitTestHelper.RandomData(2491);
			var x2 = UnitTestHelper.RandomData(9182);
			var x3 = UnitTestHelper.RandomData(5182);

			recordStorage.Create (x1); // Use 1 block
			recordStorage.Create (x2); // Use 2 blocks
			recordStorage.Create (x3); // Use 1 block

			var x2u = UnitTestHelper.RandomData (9177); // Use 2 blocks, still
			recordStorage.Update (2, x2u);

			Assert.IsTrue (recordStorage.Find (1).SequenceEqual (x1));
			Assert.IsTrue (recordStorage.Find (2).SequenceEqual (x2u));
			Assert.IsTrue (recordStorage.Find (4).SequenceEqual (x3));
		}

		[Test]
		public void TestUpdateBlockToSmallerSize ()
		{
			var recordStorage = new RecordStorage (new BlockStorage(new MemoryStream(), 8192, 48));
			var x1 = UnitTestHelper.RandomData(2491);
			var x2 = UnitTestHelper.RandomData(9182);
			var x3 = UnitTestHelper.RandomData(5182);

			recordStorage.Create (x1); // Use 1 block
			recordStorage.Create (x2); // Use 2 blocks
			recordStorage.Create (x3); // Use 1 block

			var x2u = UnitTestHelper.RandomData (1177); // Use 1 block, so this record should be truncated
			recordStorage.Update (2, x2u);

			Assert.IsTrue (recordStorage.Find (1).SequenceEqual (x1));
			Assert.IsTrue (recordStorage.Find (2).SequenceEqual (x2u));
			Assert.IsTrue (recordStorage.Find (4).SequenceEqual (x3));

			Assert.IsTrue (recordStorage.Create(UnitTestHelper.RandomData(10)) == 3); // Check if block #3 being reused
		}

		[Test]
		public void TestUpdateBlockToBiggerSize ()
		{
			var recordStorage = new RecordStorage (new BlockStorage(new MemoryStream(), 8192, 48));
			var x1 = UnitTestHelper.RandomData(2491);
			var x2 = UnitTestHelper.RandomData(9182);
			var x3 = UnitTestHelper.RandomData(5182);

			recordStorage.Create (x1); // Use 1 block
			recordStorage.Create (x2); // Use 2 blocks
			recordStorage.Create (x3); // Use 1 block

			var x2u = UnitTestHelper.RandomData (8192 * 2 + 19); // Use 3 block, so this record should be extended
			recordStorage.Update (2, x2u);

			Assert.IsTrue (recordStorage.Find (1).SequenceEqual (x1));
			Assert.IsTrue (recordStorage.Find (2).SequenceEqual (x2u));
			Assert.IsTrue (recordStorage.Find (4).SequenceEqual (x3));
		}

		[Test]
		public void TestUpdateBlockToMuchBiggerSize ()
		{
			var recordStorage = new RecordStorage (new BlockStorage(new MemoryStream(), 8192, 48));
			var x1 = UnitTestHelper.RandomData(2491);
			var x2 = UnitTestHelper.RandomData(9182);
			var x3 = UnitTestHelper.RandomData(5182);

			recordStorage.Create (x1); // Use 1 block
			recordStorage.Create (x2); // Use 2 blocks
			recordStorage.Create (x3); // Use 1 block

			var x2u = UnitTestHelper.RandomData (8192 * 11 + 19); // Use 12 blocks
			recordStorage.Update (2, x2u);

			Assert.IsTrue (recordStorage.Find (1).SequenceEqual (x1));
			Assert.IsTrue (recordStorage.Find (2).SequenceEqual (x2u));
			Assert.IsTrue (recordStorage.Find (4).SequenceEqual (x3));
		}

		[Test]
		public void TestCreateNewPersist ()
		{
			var customData = new byte[4096*16 + 27];
			var rnd = new Random ();
			for (var i = 0; i < customData.Length; i++) {
				customData[i] = (byte)rnd.Next(0,256);
			}

			using (var ms = new MemoryStream())
			{
				var recordStorage = new RecordStorage(new BlockStorage(ms));
				var recordId = recordStorage.Create (customData);

				// First record, shoud has id of 1..
				Assert.AreEqual (1, recordId);

				// Test read back the data
				Assert.True (customData.SequenceEqual(recordStorage.Find(1)));

				// Now test persistant
				var recordStorage2 = new RecordStorage(new BlockStorage(ms));
				Assert.True (customData.SequenceEqual(recordStorage2.Find(1)));
			}
		}

		[Test]
		public void TestCreateNewPersistEmpty ()
		{
			var customData = new byte[0];
			var rnd = new Random ();
			for (var i = 0; i < customData.Length; i++) {
				customData[i] = (byte)rnd.Next(0,256);
			}

			using (var ms = new MemoryStream())
			{
				var recordStorage = new RecordStorage(new BlockStorage(ms));
				var recordId = recordStorage.Create (customData);

				// First record, shoud has id of 1..
				Assert.AreEqual (1, recordId);

				// Test read back the data
				Assert.True (customData.SequenceEqual(recordStorage.Find(1)));

				// Now test persistant
				var recordStorage2 = new RecordStorage(new BlockStorage(ms));
				Assert.True (customData.SequenceEqual(recordStorage2.Find(1)));
			}
		}

		[Test]
		public void TestCreateNewPersistSmall ()
		{
			var customData = new byte[1];
			var rnd = new Random ();
			for (var i = 0; i < customData.Length; i++) {
				customData[i] = (byte)rnd.Next(0,256);
			}

			using (var ms = new MemoryStream())
			{
				var recordStorage = new RecordStorage(new BlockStorage(ms));
				var recordId = recordStorage.Create (customData);

				// First record, shoud has id of 1..
				Assert.AreEqual (1, recordId);

				// Test read back the data
				Assert.True (customData.SequenceEqual(recordStorage.Find(1)));

				// Now test persistant
				var recordStorage2 = new RecordStorage(new BlockStorage(ms));	
				Assert.True (customData.SequenceEqual(recordStorage2.Find(1)));
			}
		}

		[Test]
		public void TestTrackingOfLargeFreeBlockList ()
		{
			var tmp = Path.Combine (System.IO.Path.GetTempPath(), "data.bin");

			try {
				using (var ms = new FileStream(tmp, FileMode.Create))
				{
					var recordStorage = new RecordStorage(new BlockStorage(ms));

					// Create 10,000 records
					var ids = new Dictionary<uint, bool>();
					for (var i = 0; i < 15342; i++) {
						ids.Add (recordStorage.Create (new byte[]{ 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 }), true);
					}

					// Now delete them all
					foreach (var kv in ids)
					{
						recordStorage.Delete (kv.Key);
					}

					// Create records and make sure they reuse these ids,
					// And make use it reuses the second block (15343) of free block tracking record as well
					var reusedSecondFreeBlockTracker = false;
					for (var i = 15342; i >= 0; i--)
					{
						var id = recordStorage.Create (new byte[]{ 0x00, 0x01, 0x02, 0x03});
						if (id == 15343) {
							reusedSecondFreeBlockTracker = true;
						} else {
							Assert.IsTrue (ids.ContainsKey(id));
						}
					}

					Assert.True (reusedSecondFreeBlockTracker);

					// Make sure it still continue increment the id..
					Assert.AreEqual (15344, recordStorage.Create (new byte[]{ 0x00, 0x01, 0x02, 0x03}));
				}
			} finally {
				if (File.Exists(tmp))
				{
					File.Delete (tmp);
					Console.WriteLine ("Deleted: " + tmp);
				}
			}
		}

		[Test]
		public void TestDeletion()
		{
			var d1 = GenerateRandomData (1029);	
			var d2 = GenerateRandomData (14 * 1024 * 4);	
			var d3 = GenerateRandomData (3591);	

			var d4 = GenerateRandomData (4444);	
			var d5 = GenerateRandomData (5555);	
			var d6 = GenerateRandomData (6666);	

			using (var ms = new MemoryStream())
			{
				var recordStorage = new RecordStorage(new BlockStorage(ms));
				var r1 = recordStorage.Create (d1);
				var r2 = recordStorage.Create (d2);
				var r3 = recordStorage.Create (d3);

				Assert.AreEqual (1, r1);
				Assert.AreEqual (2, r2);
				Assert.AreEqual (4, r3);

				Assert.True (recordStorage.Find(r1).SequenceEqual(d1));
				Assert.True (recordStorage.Find(r2).SequenceEqual(d2));
				Assert.True (recordStorage.Find(r3).SequenceEqual(d3));

				// Delete off 2, free 2 records
				recordStorage.Delete (r2);

				Assert.True (recordStorage.Find(r2)  == null);

				// INsert 2 new records, should take id of 2,3,4
				var r4 = recordStorage.Create (d4);
				var r5 = recordStorage.Create (d5);
				var r6 = recordStorage.Create (d6);
				Assert.AreEqual (3, r4);
				Assert.AreEqual (2, r5);
				Assert.AreEqual (5, r6);

				// Check that data is not being corrupted if we use the reusable block
				Assert.True (recordStorage.Find(r4).SequenceEqual(d4));
				Assert.True (recordStorage.Find(r5).SequenceEqual(d5));
				Assert.True (recordStorage.Find(r6).SequenceEqual(d6));

				// Test persistance
				var recordStorage2 = new RecordStorage(new BlockStorage(ms));
				Assert.True (recordStorage2.Find(r1).SequenceEqual(d1));
				Assert.True (recordStorage2.Find(r3).SequenceEqual(d3));
				Assert.True (recordStorage2.Find(r4).SequenceEqual(d4));
				Assert.True (recordStorage2.Find(r5).SequenceEqual(d5));
				Assert.True (recordStorage2.Find(r6).SequenceEqual(d6));
			}
		}

		static byte[] GenerateRandomData (int size)
		{
			var customData = new byte[size];
			var rnd = new Random ();
			for (var i = 0; i < customData.Length; i++) {
				customData[i] = (byte)rnd.Next(0,256);
			}
			return customData;
		}
	}
}

