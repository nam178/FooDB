using System;
using System.IO;
using System.Collections.Generic;
using FooCore;

namespace FooApplication
{
	/// <summary>
	/// Then, define our database
	/// </summary>
	class CowDatabase : IDisposable
	{
		readonly Stream mainDatabaseFile;
		readonly Stream primaryIndexFile;
		readonly Stream secondaryIndexFile;
		readonly Tree<Guid, uint> primaryIndex;
		readonly Tree<Tuple<string, int>, uint> secondaryIndex;
		readonly RecordStorage cowRecords;
		readonly CowSerializer cowSerializer = new CowSerializer ();

		/// <summary>
		/// </summary>
		/// <param name="pathToCowDb">Path to cow db.</param>
		public CowDatabase (string pathToCowDb)	
		{
			if (pathToCowDb == null)
				throw new ArgumentNullException ("pathToCowDb");

			// As soon as CowDatabase is constructed, open the steam to talk to the underlying files
			this.mainDatabaseFile = new FileStream (pathToCowDb, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
			this.primaryIndexFile = new FileStream (pathToCowDb + ".pidx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
			this.secondaryIndexFile = new FileStream (pathToCowDb + ".sidx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);

			// Construct the RecordStorage that use to store main cow data
			this.cowRecords = new RecordStorage (new BlockStorage(this.mainDatabaseFile, 4096, 48));

			// Construct the primary and secondary indexes 
			this.primaryIndex = new Tree<Guid, uint> (
				new TreeDiskNodeManager<Guid, uint>(
					new GuidSerializer(),
					new TreeUIntSerializer(),
					new RecordStorage(new BlockStorage(this.primaryIndexFile, 4096))
				),
				false
			);

			this.secondaryIndex = new Tree<Tuple<string, int>, uint> (
				new TreeDiskNodeManager<Tuple<string, int>, uint>(
					new StringIntSerializer(), 
					new TreeUIntSerializer(), 
					new RecordStorage(new BlockStorage(this.secondaryIndexFile, 4096))
				),
				true
			);
		}

		/// <summary>
		/// Update given cow
		/// </summary>
		public void Update (CowModel cow)
		{
			if (disposed) {
				throw new ObjectDisposedException ("CowDatabase");
			}

			throw new NotImplementedException ();
		}

		/// <summary>
		/// Insert a new cow entry into our cow database
		/// </summary>
		public void Insert (CowModel cow)
		{
			if (disposed) {
				throw new ObjectDisposedException ("CowDatabase");
			}

			// Serialize the cow and insert it
			var recordId = this.cowRecords.Create (this.cowSerializer.Serialize(cow));

			// Primary index
			this.primaryIndex.Insert (cow.Id, recordId);

			// Secondary index
			this.secondaryIndex.Insert (new Tuple<string, int>(cow.Breed, cow.Age), recordId);
		}

		/// <summary>
		/// Find a cow by its unique id
		/// </summary>
		public CowModel Find (Guid cowId)
		{
			if (disposed) {
				throw new ObjectDisposedException ("CowDatabase");
			}

			// Look in the primary index for this cow
			var entry = this.primaryIndex.Get (cowId);
			if (entry == null) {
				return null;
			}

			return this.cowSerializer.Deserializer (this.cowRecords.Find (entry.Item2));
		}

		/// <summary>
		/// Find all cows that belongs to given breed and age
		/// </summary>
		public IEnumerable<CowModel> FindBy (string breed, int age)
		{
			var comparer = Comparer<Tuple<string, int>>.Default;
			var searchKey = new Tuple<string, int>(breed, age);

			// Use the secondary index to find this cow
			foreach (var entry in this.secondaryIndex.LargerThanOrEqualTo (searchKey))
			{
				// As soon as we reached larger key than the key given by client, stop
				if (comparer.Compare(entry.Item1, searchKey) > 0) {
					break;
				}

				// Still in range, yield return
				yield return this.cowSerializer.Deserializer (this.cowRecords.Find (entry.Item2));
			}
		}

		/// <summary>
		/// Delete specified cow from our database
		/// </summary>
		public void Delete (CowModel cow)
		{
			throw new NotImplementedException ();
		}

		#region Dispose
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		bool disposed = false;

		protected virtual void Dispose (bool disposing)
		{
			if (disposing && !disposed)
			{
				this.mainDatabaseFile.Dispose ();
				this.secondaryIndexFile.Dispose();
				this.primaryIndexFile.Dispose ();
				this.disposed = true;
			}
		}

		~CowDatabase() 
		{
			Dispose (false);
		}
		#endregion
	}
}

