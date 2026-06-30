using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using TrumpTile.GameMain.Core;

namespace TrumpTile.Tests.EditMode
{
	public class BoardManagerGemBlockingTests
	{
		private BoardManager mBoardManager;
		private TileController mTile;
		private Vector3 mGemPos;
		private Vector3 mGemOverlapTilePos;

		[SetUp]
		public void SetUp()
		{
			GameObject boardGo = new GameObject("TestBoardManager");
			mBoardManager = boardGo.AddComponent<BoardManager>();

			Vector3[,,] boardMap = new Vector3[5, 5, 5];
			SetPrivateField(mBoardManager, "mBoardMap", boardMap);

			mGemPos = (Vector3)GetPrivateField(mBoardManager, "mGemPos");
			mGemOverlapTilePos = (Vector3)GetPrivateField(mBoardManager, "mGemOverlapTilePos");

			GameObject tileGo = new GameObject("TestTile");
			GameObject icon = new GameObject("Icon");
			icon.transform.SetParent(tileGo.transform);
			icon.AddComponent<SpriteRenderer>();
			mTile = tileGo.AddComponent<TileController>();

			SetPrivateField(mTile, "mGridX", 1);
			SetPrivateField(mTile, "mGridY", 2);
			SetPrivateField(mTile, "mLayerIndex", 3);
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(mBoardManager.gameObject);
			Object.DestroyImmediate(mTile.gameObject);
		}

		private static void SetPrivateField(object target, string fieldName, object value)
		{
			FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
			field.SetValue(target, value);
		}

		private static object GetPrivateField(object target, string fieldName)
		{
			FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
			return field.GetValue(target);
		}

		private void SetBoardMapCell(Vector3 value)
		{
			Vector3[,,] boardMap = (Vector3[,,])GetPrivateField(mBoardManager, "mBoardMap");
			boardMap[1, 2, 3] = value;
		}

		[Test]
		public void IsCoveredByGem_ReturnsTrue_WhenCellIsReservedEmptyByGem()
		{
			SetBoardMapCell(mGemPos);

			bool bResult = mBoardManager.IsCoveredByGem(mTile);

			Assert.IsTrue(bResult);
		}

		[Test]
		public void IsCoveredByGem_ReturnsFalse_WhenCellHasOriginalTileStillOnBoard()
		{
			SetBoardMapCell(mGemOverlapTilePos);

			bool bResult = mBoardManager.IsCoveredByGem(mTile);

			Assert.IsFalse(bResult);
		}

		[Test]
		public void IsCoveredByGem_ReturnsFalse_WhenCellIsUnrelatedToAnyGem()
		{
			SetBoardMapCell(Vector3.zero);

			bool bResult = mBoardManager.IsCoveredByGem(mTile);

			Assert.IsFalse(bResult);
		}
	}
}
