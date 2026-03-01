using System;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	/// <summary>
	/// 콤보 항목 데이터
	/// </summary>
	[Serializable]
	public class ComboEntry
	{
		[Tooltip("화면에 표시될 콤보 레이블 (예: Good, Nice, Great ...)")]
		public string label;

		[Tooltip("이 콤보가 발동되는 연속 매치 횟수")]
		public int consecutiveCount;

		[Tooltip("음성(효과음) 재생 여부")]
		public bool hasSound;

		[Tooltip("TRUE면 동일 횟수를 가진 다른 랜덤 항목들과 경쟁하여 하나가 랜덤 선택됨")]
		public bool isRandom;

		[Tooltip("마지막 매치 후 콤보가 종료되기까지의 대기 시간 (초)")]
		public float comboEndTime;

		[Tooltip("hasSound가 TRUE일 때 재생할 오디오 클립")]
		public AudioClip audioClip;

		[Tooltip("화면에 표시될 레이블 색상")]
		public Color labelColor = Color.yellow;
	}

	/// <summary>
	/// 콤보 테이블 데이터 에셋
	///
	/// [에셋 생성]
	/// Project 창 우클릭 → Create → TrumpTile → Combo → ComboDatabase
	///
	/// [활용]
	/// - 난이도별로 별도 에셋 생성 가능 (ComboDatabase_Easy, ComboDatabase_Hard ...)
	/// - Addressable로 등록하여 레벨별 동적 교체 가능
	/// - ComboSystem 인스펙터의 Combo Database 슬롯에 드래그 앤 드롭
	/// </summary>
	[CreateAssetMenu(fileName = "ComboDatabase", menuName = "TrumpTile/Combo/ComboDatabase")]
	public class ComboDatabase : ScriptableObject
	{
		[SerializeField] private ComboEntry[] mEntries;

		public ComboEntry[] Entries => mEntries;
	}
}
