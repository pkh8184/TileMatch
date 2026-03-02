using UnityEngine;
using System;

namespace TrumpTile.GameMain.Data
{
	/// <summary>
	/// 스테이지 테이블 데이터
	/// </summary>
	[Serializable]
	public class StageData
	{
		[Header("기본 정보")]
		public int stageId;              // 스테이지 고유 ID
		public int stageLevel;           // 스테이지 레벨 (표시용)
		public int nextStageId;          // 다음 스테이지 ID (0이면 없음)

		[Header("보상")]
		public int rewardId;             // 보상 ID (ItemTable 참조)
		public int rewardCount;          // 보상 개수

		[Header("튜토리얼")]
		public int tutorialId;           // 튜토리얼 ID (0이면 없음)
		public ETutorialType startTutorialType;  // 시작 튜토리얼 타입
		public ETutorialType endTutorialType;    // 종료 튜토리얼 타입

		[Header("리소스 경로")]
		public string levelDataSrc;      // 레벨 데이터 경로 (Resources/)
		public string backgroundSrc;     // 배경 이미지 경로
		public string bgmSrc;            // BGM 경로

		[Header("게임 설정")]
		public int maxSlots;             // 최대 슬롯 수 (기본 7)
		public int matchCount;           // 매칭 필요 타일 수 (기본 3)
		public float timeLimit;          // 제한 시간 (0이면 무제한)

		[Header("별 획득 조건")]
		public int star1Score;           // 별 1개 점수
		public int star2Score;           // 별 2개 점수
		public int star3Score;           // 별 3개 점수

		[Header("초기 아이템")]
		public int initialShuffleCount;  // 시작 셔플 개수
		public int initialRemoveSlotCount; // 시작 슬롯비우기 개수
		public int initialLuckyMatchCount; // 시작 럭키매치 개수
	}

	/// <summary>
	/// 튜토리얼 타입
	/// </summary>
	public enum ETutorialType
	{
		None = 0,
		BasicMatch = 1,         // 기본 매칭 방법
		SlotFull = 2,           // 슬롯 가득 참 경고
		UseShuffleItem = 3,     // 셔플 아이템 사용법
		UseRemoveSlotItem = 4,  // 슬롯비우기 아이템 사용법
		UseLuckyMatchItem = 5,  // 럭키매치 아이템 사용법
		LayerExplain = 6,       // 레이어 설명
		ComboExplain = 7,       // 콤보 설명
	}

	/// <summary>
	/// 스테이지 테이블 ScriptableObject
	/// </summary>
	[CreateAssetMenu(fileName = "StageTable", menuName = "TrumpTile/Data/Stage Table")]
	public class StageTable : ScriptableObject
	{
		public StageData[] stages;

		/// <summary>
		/// 스테이지 ID로 데이터 찾기
		/// </summary>
		public StageData GetStageById(int stageId)
		{
			if (stages == null)
			{
				return null;
			}

			foreach (StageData stage in stages)
			{
				if (stage.stageId == stageId)
				{
					return stage;
				}
			}
			return null;
		}

		/// <summary>
		/// 스테이지 레벨로 데이터 찾기
		/// </summary>
		public StageData GetStageByLevel(int level)
		{
			if (stages == null)
			{
				return null;
			}

			foreach (StageData stage in stages)
			{
				if (stage.stageLevel == level)
				{
					return stage;
				}
			}
			return null;
		}

		/// <summary>
		/// 총 스테이지 수
		/// </summary>
		public int TotalStageCount => stages != null ? stages.Length : 0;
	}
}
