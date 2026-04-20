using System;

namespace TrumpTile.Editor
{
	[AttributeUsage(AttributeTargets.Field)]
	public class SheetNameAttribute : Attribute
	{
		public string Name { get; }

		public SheetNameAttribute(string name)
		{
			Name = name;
		}
	}
}
