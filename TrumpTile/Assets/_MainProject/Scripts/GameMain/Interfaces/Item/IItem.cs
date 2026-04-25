using System;
using System.Collections;

namespace TrumpTile.GameMain.Item
{
	public interface IItem
	{
		int ItemId { get; }
		bool CanExecute();
		IEnumerator Execute(Action onComplete);
	}
}
