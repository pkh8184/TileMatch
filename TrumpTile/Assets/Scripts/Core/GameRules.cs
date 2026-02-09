using UnityEngine;

namespace TrumpTile.Core
{
	/// <summary>
	/// 게임 규칙 및 상수 중앙 관리
	/// 
	/// [핵심 규칙]
	/// 1. 타일 선택: blocked 타일은 선택 불가
	/// 2. 매칭: 같은 타입 3개가 슬롯에 모이면 제거
	/// 3. 게임오버: 슬롯 7칸이 가득 차면 패배
	/// 4. 승리: 보드의 모든 타일 제거
	/// 5. 아이템: 게임 상태가 Playing일 때만 사용 가능
	/// </summary>
	public static class GameRules
	{
		#region Slot Rules

		/// <summary>최대 슬롯 개수</summary>
		public const int MAX_SLOTS = 7;

		/// <summary>매칭에 필요한 타일 수</summary>
		public const int MATCH_COUNT = 3;

		/// <summary>부활 시 비우는 슬롯 수</summary>
		public const int REVIVE_CLEAR_SLOTS = 2;

		/// <summary>게임당 최대 부활 횟수</summary>
		public const int MAX_REVIVE_COUNT = 3;

		#endregion

		#region Board Rules

		/// <summary>타일 간 최소 겹침 거리 (이 이하면 blocked)</summary>
		public const float OVERLAP_THRESHOLD = 0.8f;

		/// <summary>레이어 오프셋 (X, Y 방향)</summary>
		public const float LAYER_OFFSET = 0.15f;

		/// <summary>타일 셀 크기</summary>
		public const float CELL_SIZE = 1.0f;

		#endregion

		#region Timing Rules

		/// <summary>타일 이동 시간</summary>
		public const float TILE_MOVE_DURATION = 0.3f;

		/// <summary>매칭 애니메이션 시간</summary>
		public const float MATCH_ANIMATION_DURATION = 0.2f;

		/// <summary>섞기 애니메이션 시간</summary>
		public const float SHUFFLE_DURATION = 0.5f;

		/// <summary>버튼 쿨다운 시간</summary>
		public const float BUTTON_COOLDOWN = 0.5f;

		/// <summary>게임오버 카운트다운 시간</summary>
		public const float GAMEOVER_COUNTDOWN = 10f;

		/// <summary>레벨 클리어 대기 시간</summary>
		public const float LEVEL_CLEAR_DELAY = 0.5f;

		#endregion

		#region Scoring Rules

		/// <summary>기본 매칭 점수</summary>
		public const int BASE_MATCH_SCORE = 100;

		/// <summary>콤보당 추가 점수</summary>
		public const int COMBO_BONUS = 50;

		/// <summary>별 1개 기준 점수</summary>
		public const int STAR_1_THRESHOLD = 1000;

		/// <summary>별 2개 기준 점수</summary>
		public const int STAR_2_THRESHOLD = 2000;

		/// <summary>별 3개 기준 점수</summary>
		public const int STAR_3_THRESHOLD = 3000;

		#endregion

		#region Item Rules

		/// <summary>초기 Strike 아이템 수</summary>
		public const int INITIAL_STRIKE_COUNT = 3;

		/// <summary>초기 BlackHole 아이템 수</summary>
		public const int INITIAL_BLACKHOLE_COUNT = 3;

		/// <summary>초기 Boom 아이템 수</summary>
		public const int INITIAL_BOOM_COUNT = 3;

		/// <summary>Boom 아이템이 제거하는 세트 수</summary>
		public const int BOOM_REMOVE_SETS = 3;

		#endregion

		#region Validation Methods

		/// <summary>
		/// 슬롯에 타일 추가 가능 여부
		/// </summary>
		public static bool CanAddTileToSlot(int currentSlotCount)
		{
			return currentSlotCount < MAX_SLOTS;
		}

		/// <summary>
		/// 게임오버 조건 확인
		/// </summary>
		public static bool IsGameOver(int currentSlotCount)
		{
			return currentSlotCount >= MAX_SLOTS;
		}

		/// <summary>
		/// 매칭 가능 여부 확인
		/// </summary>
		public static bool CanMatch(int sameTypeCount)
		{
			return sameTypeCount >= MATCH_COUNT;
		}

		/// <summary>
		/// 부활 가능 여부 확인
		/// </summary>
		public static bool CanRevive(int currentReviveCount)
		{
			return currentReviveCount < MAX_REVIVE_COUNT;
		}

		/// <summary>
		/// 별 개수 계산
		/// </summary>
		public static int CalculateStars(int score)
		{
			if (score >= STAR_3_THRESHOLD) return 3;
			if (score >= STAR_2_THRESHOLD) return 2;
			if (score >= STAR_1_THRESHOLD) return 1;
			return 0;
		}

		/// <summary>
		/// 콤보 보너스 계산
		/// </summary>
		public static int CalculateComboBonus(int comboCount)
		{
			if (comboCount <= 1) return 0;
			return COMBO_BONUS * (comboCount - 1);
		}

		/// <summary>
		/// 매칭 점수 계산
		/// </summary>
		public static int CalculateMatchScore(int comboCount)
		{
			return BASE_MATCH_SCORE + CalculateComboBonus(comboCount);
		}

		#endregion

		#region State Validation

		/// <summary>
		/// 아이템 사용 가능 상태인지 확인
		/// </summary>
		public static bool CanUseItem(GameManager.GameState state, bool isItemInProgress)
		{
			return state == GameManager.GameState.Playing && !isItemInProgress;
		}

		/// <summary>
		/// 타일 선택 가능 상태인지 확인
		/// </summary>
		public static bool CanSelectTile(GameManager.GameState state, bool isShuffling)
		{
			return state == GameManager.GameState.Playing && !isShuffling;
		}

		#endregion
	}
}