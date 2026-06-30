using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using TrumpTile.GameMain.Core;

namespace TrumpTile.Tests.EditMode
{
	public class GemTileCoverageTests
	{
		private GemTile CreateGemTileWithOrigin(List<(int, int, int)> originList)
		{
			GameObject go = new GameObject("TestGemTile");
			GemTile gem = go.AddComponent<GemTile>();

			FieldInfo field = typeof(GemTile).GetField("mOriginIndexList", BindingFlags.NonPublic | BindingFlags.Instance);
			field.SetValue(gem, originList);

			return gem;
		}

		[Test]
		public void IsCoveringTile_ReturnsTrue_WhenCoordinateInOriginList()
		{
			List<(int, int, int)> originList = new List<(int, int, int)> { (2, 3, 1), (3, 3, 1) };
			GemTile gem = CreateGemTileWithOrigin(originList);

			bool bResult = gem.IsCoveringTile(2, 3, 1);

			Assert.IsTrue(bResult);
			Object.DestroyImmediate(gem.gameObject);
		}

		[Test]
		public void IsCoveringTile_ReturnsFalse_WhenCoordinateNotInOriginList()
		{
			List<(int, int, int)> originList = new List<(int, int, int)> { (2, 3, 1), (3, 3, 1) };
			GemTile gem = CreateGemTileWithOrigin(originList);

			bool bResult = gem.IsCoveringTile(5, 5, 1);

			Assert.IsFalse(bResult);
			Object.DestroyImmediate(gem.gameObject);
		}

		[Test]
		public void IsCoveringTile_ReturnsFalse_WhenGameObjectInactive()
		{
			List<(int, int, int)> originList = new List<(int, int, int)> { (2, 3, 1) };
			GemTile gem = CreateGemTileWithOrigin(originList);
			gem.gameObject.SetActive(false);

			bool bResult = gem.IsCoveringTile(2, 3, 1);

			Assert.IsFalse(bResult);
			Object.DestroyImmediate(gem.gameObject);
		}
	}
}
