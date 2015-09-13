using FooCore;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;

[TestFixture]
public class TreeDiskNodeSerializerTest
{
	[Test]
	public void TestVariableLengthKeySize ()
	{
		var nodeManager = new TreeDiskNodeManager<string, long> (
			new TreeStringSerialzier (), 
			new TreeLongSerializer(),
			new RecordStorage (new BlockStorage(new MemoryStream(), 4096, 48))
		);
		var node = new TreeNode<string, long> (nodeManager, 11, 88, new List<Tuple<string, long>> {
			new Tuple<string, long>("The quick brown foxs run over the lazy dog", 22L),
			new Tuple<string, long>("I dont really understand what i am doing", 33L),
			new Tuple<string, long>(String.Empty, 55L)
		}, new List<uint> {
			111, 222, 333, 444, 555
		});
		var serializer = new TreeDiskNodeSerializer<string, long> (nodeManager, new TreeStringSerialzier(), new TreeLongSerializer());

		var data = serializer.Serialize (node);

		var node2 = serializer.Deserialize (11, data);

		Assert.NotNull (node2);
		Assert.AreEqual (node.Id, node2.Id);
		Assert.AreEqual (node.ParentId, node2.ParentId);
		Assert.IsTrue (node.Entries.SequenceEqual(node2.Entries));
		Assert.IsTrue (node.ChildrenIds.SequenceEqual(node2.ChildrenIds));
	}
	
	[Test]
	public void TestEmptyNodeFixedSize ()
	{
		var nodeManager = new TreeDiskNodeManager<int, long> (
			new TreeIntSerializer (),
			new TreeLongSerializer(),
			new RecordStorage (new BlockStorage(new MemoryStream(), 4096, 48))
		);
		var node = new TreeNode<int, long> (nodeManager, 11, 88);
		var serializer = new TreeDiskNodeSerializer<int, long> (nodeManager, new TreeIntSerializer(), new TreeLongSerializer());

		var data = serializer.Serialize (node);

		var node2 = serializer.Deserialize (11, data);

		Assert.NotNull (node2);
		Assert.AreEqual (node.Id, node2.Id);
		Assert.AreEqual (node.ParentId, node2.ParentId);
		Assert.IsTrue (node.Entries.SequenceEqual(node2.Entries));
		Assert.IsTrue (node.ChildrenIds.SequenceEqual(node2.ChildrenIds));
	}

	[Test]
	public void TestNonEmptyNodeFixedSize ()
	{
		var nodeManager = new TreeDiskNodeManager<int, long> (
			new TreeIntSerializer (), 
			new TreeLongSerializer(),
			new RecordStorage (new BlockStorage(new MemoryStream(), 4096, 48))
		);
		var node = new TreeNode<int, long> (nodeManager, 11, 88, new List<Tuple<int, long>> {
			new Tuple<int, long>(11, 22L),
			new Tuple<int, long>(22, 33L),
			new Tuple<int, long>(44, 55L)
		}, new List<uint> {
			111,222,333,444,555
		});
		var serializer = new TreeDiskNodeSerializer<int, long> (nodeManager, new TreeIntSerializer(), new TreeLongSerializer());

		var data = serializer.Serialize (node);

		var node2 = serializer.Deserialize (11, data);

		Assert.NotNull (node2);
		Assert.AreEqual (node.Id, node2.Id);
		Assert.AreEqual (node.ParentId, node2.ParentId);
		Assert.IsTrue (node.Entries.SequenceEqual(node2.Entries));
		Assert.IsTrue (node.ChildrenIds.SequenceEqual(node2.ChildrenIds));
	}
}