using System;

namespace FooApplication
{
	/// <summary>
	/// Our database stores cows, first we define our Cow model
	/// </summary>
	public class CowModel
	{
		public Guid Id {
			get;
			set;
		}

		public string Breed {
			get;
			set;
		}

		public int Age {
			get;
			set;
		}

		public string Name {
			get;
			set;
		}

		public byte[] DnaData {
			get;
			set;
		}

		public override string ToString ()
		{
			return string.Format ("[CowModel: Id={0}, Breed={1}, Age={2}, Name={3}, DnaData={4}]", Id, Breed, Age, Name, DnaData.Length + " bytes");
		}
	}
}

