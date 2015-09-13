using System;

namespace FooCore
{
	public interface ISerializer<K>
	{
		byte[] Serialize (K value);

		K Deserialize (byte[] buffer, int offset, int length);

		bool IsFixedSize {
			get;
		}

		int Length {
			get;
		}
	}

}

