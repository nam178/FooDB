using System;
using System.Collections.Generic;

namespace FooCore
{
	public interface IIndex<K, V>
	{
		/// <summary>
		/// Create new entry in this index that maps key K to value V
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
		void Insert (K key, V value);

		/// <summary>
		/// Find an entry by key
		/// </summary>
		/// <param name="key">Key.</param>
		Tuple<K, V> Get (K key);

		/// <summary>
		/// Find all entries that contain a key larger than or equal to specified key
		/// </summary>
		IEnumerable<Tuple<K, V>> LargerThanOrEqualTo (K key);

		/// <summary>
		/// Find all entries that contain a key larger than specified key
		/// </summary>
		IEnumerable<Tuple<K, V>> LargerThan (K key);

		/// <summary>
		/// Find all entries that contain a key less than or equal specified key
		/// </summary>
		IEnumerable<Tuple<K, V>> LessThanOrEqualTo (K key);

		/// <summary>
		/// Find all entries that contain a key less than specified key
		/// </summary>
		IEnumerable<Tuple<K, V>> LessThan (K key);

		/// <summary>
		/// Delete an entry from this index, optionally use specified IComparer to compare values
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
		/// <param name="valueComparer">Value comparer; Default value is Comparer[k].Default</param>
		bool Delete (K key, V value, IComparer<V> valueComparer = null);

		/// <summary>
		/// Delete all entries of given key
		/// </summary>
		/// <param name="key">Key.</param>
		bool Delete (K key);
	}
}

