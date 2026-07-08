using System;
using UnityEditor;
using UnityEngine;
using TrumpTile.FrameLibrary;

public static class DailyPuzzleDebugMenu
{
	[MenuItem("TrumpTile/Debug/Clear Daily Puzzle Save")]
	public static void ClearDailyPuzzleSave()
	{
		string key = "DailyCleared_" + GameTime.Today.ToString("yyyy-MM-dd");
		PlayerPrefs.DeleteKey(key);
		PlayerPrefs.Save();
		Debug.Log($"[Debug] Deleted key: {key}");
	}
}
