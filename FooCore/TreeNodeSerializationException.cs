using System;

namespace FooCore
{
	public class TreeNodeSerializationException : Exception
	{
		public TreeNodeSerializationException (Exception innerException) 
			: base ("Failed to serialize/deserialize heat map node", innerException)
		{
			
		}
	}
}

