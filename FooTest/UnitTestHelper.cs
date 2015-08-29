using System;

namespace UnitTest
{
	public static class UnitTestHelper
	{
		public static byte[] RandomData (int length)
		{
			var data = new byte[length];
			var rnd = new Random ();
			for (var i = 0; i < data.Length; i++) {
				data[i] = (byte)rnd.Next (0, 256);
			}
			return data;
		}
	}
}