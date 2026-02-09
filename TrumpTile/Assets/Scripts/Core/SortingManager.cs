using UnityEngine;

namespace TrumpTile.Core
{
	/// <summary>
	/// Sorting Order 중앙 관리
	/// 
	/// [Sorting Layer 구조]
	/// - Background: 0 (배경)
	/// - Board: 100~999 (보드 타일)
	/// - Slot: 1000~1099 (슬롯 타일)
	/// - Effect: 2000~2999 (이펙트)
	/// - Popup: 3000~3999 (팝업)
	/// - Transition: 9999 (씬 전환)
	/// 
	/// [보드 타일 Sorting 규칙]
	/// - 기본값: 100
	/// - 레이어당 증가: +100
	/// - Y좌표 보정: +gridY (낮을수록 앞에)
	/// - 공식: 100 + (layer * 100) + (maxGridY - gridY)
	/// </summary>
	public static class SortingManager
	{
		#region Sorting Layer Constants

		// 기본 레이어 시작값
		public const int BACKGROUND_BASE = 0;
		public const int BOARD_BASE = 100;
		public const int SLOT_BASE = 1000;
		public const int EFFECT_BASE = 2000;
		public const int POPUP_BASE = 3000;
		public const int TRANSITION_BASE = 9999;

		// 레이어당 증가값
		public const int LAYER_INCREMENT = 100;

		// 슬롯 내 타일 간격
		public const int SLOT_TILE_INCREMENT = 10;

		// 최대 그리드 Y (Y 보정용)
		private static int maxGridY = 20;

		#endregion

		#region Configuration

		/// <summary>
		/// 최대 그리드 Y 설정 (레벨 로드 시 호출)
		/// </summary>
		public static void SetMaxGridY(int value)
		{
			maxGridY = Mathf.Max(1, value);
		}

		#endregion

		#region Board Tile Sorting

		/// <summary>
		/// 보드 타일의 Sorting Order 계산
		/// </summary>
		/// <param name="layer">타일 레이어 (0부터 시작)</param>
		/// <param name="gridY">그리드 Y 좌표</param>
		/// <returns>Sorting Order</returns>
		public static int GetBoardTileSortingOrder(int layer, int gridY)
		{
			// 레이어가 높을수록, Y가 낮을수록 앞에 표시
			int layerOrder = layer * LAYER_INCREMENT;
			int yOrder = Mathf.Clamp(maxGridY - gridY, 0, LAYER_INCREMENT - 1);

			return BOARD_BASE + layerOrder + yOrder;
		}

		/// <summary>
		/// 보드 타일의 Sorting Order 계산 (상세)
		/// </summary>
		public static int GetBoardTileSortingOrder(int layer, int gridX, int gridY)
		{
			// X도 고려하여 더 정밀한 정렬
			int layerOrder = layer * LAYER_INCREMENT;
			int yOrder = Mathf.Clamp(maxGridY - gridY, 0, 50);
			int xOrder = Mathf.Clamp(gridX, 0, 49);

			return BOARD_BASE + layerOrder + yOrder + xOrder;
		}

		#endregion

		#region Slot Tile Sorting

		/// <summary>
		/// 슬롯 타일의 Sorting Order 계산
		/// </summary>
		/// <param name="slotIndex">슬롯 인덱스 (0~6)</param>
		/// <returns>Sorting Order</returns>
		public static int GetSlotTileSortingOrder(int slotIndex)
		{
			return SLOT_BASE + (slotIndex * SLOT_TILE_INCREMENT);
		}

		/// <summary>
		/// 슬롯으로 이동 중인 타일의 Sorting Order
		/// </summary>
		public static int GetMovingToSlotSortingOrder()
		{
			return SLOT_BASE + 99; // 슬롯 타일들보다 앞에
		}

		#endregion

		#region Effect Sorting

		/// <summary>
		/// 이펙트 Sorting Order
		/// </summary>
		public static int GetEffectSortingOrder(int priority = 0)
		{
			return EFFECT_BASE + priority;
		}

		/// <summary>
		/// 매칭 이펙트 Sorting Order
		/// </summary>
		public static int GetMatchEffectSortingOrder()
		{
			return EFFECT_BASE + 100;
		}

		#endregion

		#region UI Sorting

		/// <summary>
		/// 팝업 Sorting Order
		/// </summary>
		public static int GetPopupSortingOrder(int priority = 0)
		{
			return POPUP_BASE + priority;
		}

		/// <summary>
		/// 전환 효과 Sorting Order
		/// </summary>
		public static int GetTransitionSortingOrder()
		{
			return TRANSITION_BASE;
		}

		#endregion

		#region Debug

		/// <summary>
		/// Sorting 정보 로그 출력
		/// </summary>
		public static void LogSortingInfo(string context, int layer, int gridY, int sortingOrder)
		{
			Debug.Log($"[SortingManager] {context} - Layer: {layer}, GridY: {gridY}, SortingOrder: {sortingOrder}");
		}

		#endregion
	}
}