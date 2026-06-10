# 앨범 수집 콘텐츠 시스템 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 스테이지 클리어 시 앨범 사진을 수집하고 보상을 획득하는 하우징 콘텐츠 시스템 구현

**Architecture:** AlbumContent(ContentBase 상속)가 해금 상태를 관리하고, AlbumManager(Singleton_GameObject)가 사진 수집·보상 지급·Firebase 동기화를 담당한다. AlbumPopup이 하우징 UI와 보상 연출을 맡는다.

**Tech Stack:** Unity C#, DOTween, Firebase Realtime DB, ScriptableObject, NUnit(EditMode 테스트)

**설계 문서:** `Assets/WorkProgress/2026-06-07-album-collection-design.md`

---

## 파일 맵

| 파일 | 생성/수정 | 역할 |
|------|-----------|------|
| `Assets/_MainProject/Scripts/GameMain/Data/TBAlbumData.cs` | 생성 | TB_Album 데이터 클래스 + ScriptableObject 테이블 |
| `Assets/_MainProject/Scripts/GameMain/Data/TBPictureData.cs` | 생성 | TB_Picture 데이터 클래스 + ScriptableObject 테이블 |
| `Assets/_MainProject/Scripts/Editor/ESheetType.cs` | 수정 | TBAlbum, TBPicture enum 추가 |
| `Assets/_MainProject/Scripts/Editor/Parsers/TBAlbumParser.cs` | 생성 | TB_Album 시트 파서 |
| `Assets/_MainProject/Scripts/Editor/Parsers/TBPictureParser.cs` | 생성 | TB_Picture 시트 파서 |
| `Assets/_MainProject/Scripts/GameMain/Data/UserData.cs` | 수정 | 앨범 진행 필드 추가 |
| `Assets/_MainProject/Scripts/GameMain/Data/PlayerDataManager.cs` | 수정 | 앨범 관련 메서드 추가 |
| `Assets/_MainProject/Scripts/GameMain/Core/AlbumContent.cs` | 생성 | ContentBase 상속, 해금 상태 관리 |
| `Assets/_MainProject/Scripts/GameMain/Core/AlbumManager.cs` | 생성 | Singleton_GameObject, 사진 수집·보상·동기화 |
| `Assets/_MainProject/Scripts/GameMain/Core/GameManager.cs` | 수정 | LevelClear 시 AlbumManager 호출 추가 |
| `Assets/_MainProject/Scripts/GameMain/UI/AlbumPopup.cs` | 수정 | 하우징 UI + 보상 연출 구현 (기존 빈 파일) |
| `Assets/_MainProject/Scripts/GameMain/UI/AlbumPhotoPreviewPopup.cs` | 생성 | 사진 프리뷰 팝업 |
| `Assets/_MainProject/Scripts/GameMain/Core/MainManager.cs` | 수정 | 메인 진입 시 AlbumManager.CheckPendingReward() 호출 |
| `Assets/Tests/EditMode/AlbumSystemTests.cs` | 생성 | EditMode 단위 테스트 |

---

## Task 1: TBAlbumData + TBPictureData 데이터 클래스

**Files:**
- Create: `Assets/_MainProject/Scripts/GameMain/Data/TBAlbumData.cs`
- Create: `Assets/_MainProject/Scripts/GameMain/Data/TBPictureData.cs`

- [ ] **Step 1: TBAlbumData.cs 작성**

```csharp
using System;
using UnityEngine;

namespace TrumpTile.GameMain.Data
{
	[Serializable]
	public class TBAlbumData
	{
		public int    AlbumGroupId;
		public int    GroupNameId;
		public int    GoldRewardCount;
		public string Summary;
	}

	[CreateAssetMenu(fileName = "TBAlbumTable", menuName = "TrumpTile/Data/TB Album Table")]
	public class TBAlbumTable : ScriptableObject
	{
		public TBAlbumData[] items;

		public TBAlbumData GetById(int albumGroupId)
		{
			if (items == null)
			{
				return null;
			}
			foreach (TBAlbumData item in items)
			{
				if (item.AlbumGroupId == albumGroupId)
				{
					return item;
				}
			}
			return null;
		}
	}
}
```

- [ ] **Step 2: TBPictureData.cs 작성**

```csharp
using System;
using UnityEngine;

namespace TrumpTile.GameMain.Data
{
	[Serializable]
	public class TBPictureData
	{
		public int    AlbumGroupId;
		public int    PictureId;
		public int    StageValue;
		public int    PictureNameId;
		public int    PictureDescriptionId;
		public int    HammerRewardCount;
		public int    MagicStickRewardCount;
		public int    MagicHatRewardCount;
		public int    BombRewardCount;
		public string MainThumbnailSrc;
		public string PictureThumbnailSrc;
		public string PictureBackgroundSrc;
		public string Summary;
	}

	[CreateAssetMenu(fileName = "TBPictureTable", menuName = "TrumpTile/Data/TB Picture Table")]
	public class TBPictureTable : ScriptableObject
	{
		public TBPictureData[] items;

		public TBPictureData GetById(int pictureId)
		{
			if (items == null)
			{
				return null;
			}
			foreach (TBPictureData item in items)
			{
				if (item.PictureId == pictureId)
				{
					return item;
				}
			}
			return null;
		}

		public TBPictureData[] GetByAlbumGroup(int albumGroupId)
		{
			if (items == null)
			{
				return new TBPictureData[0];
			}
			return System.Array.FindAll(items, p => p.AlbumGroupId == albumGroupId);
		}
	}
}
```

- [ ] **Step 3: Unity Editor에서 컴파일 확인**

Unity Editor 하단 콘솔에 에러가 없으면 통과.

- [ ] **Step 4: 커밋**

```bash
git add Assets/_MainProject/Scripts/GameMain/Data/TBAlbumData.cs
git add Assets/_MainProject/Scripts/GameMain/Data/TBPictureData.cs
git commit -m "feat: TB_Album, TB_Picture 데이터 클래스 추가"
```

---

## Task 2: ESheetType 확장 + 시트 파서 2개

**Files:**
- Modify: `Assets/_MainProject/Scripts/Editor/ESheetType.cs:8`
- Create: `Assets/_MainProject/Scripts/Editor/Parsers/TBAlbumParser.cs`
- Create: `Assets/_MainProject/Scripts/Editor/Parsers/TBPictureParser.cs`

- [ ] **Step 1: ESheetType.cs에 enum 추가**

`Assets/_MainProject/Scripts/Editor/ESheetType.cs`의 기존 enum에 두 줄 추가:

```csharp
namespace TrumpTile.Editor
{
	public enum ESheetType
	{
		[SheetName("TB_Stage")]
		TBStage,
		[SheetName("TB_Item")]
		TBItem,
		[SheetName("TB_Album")]   // 추가
		TBAlbum,
		[SheetName("TB_Picture")] // 추가
		TBPicture,
	}
}
```

- [ ] **Step 2: TBAlbumParser.cs 작성**

```csharp
using System.Collections.Generic;
using System.IO;
using TrumpTile.GameMain.Data;
using UnityEditor;
using UnityEngine;

namespace TrumpTile.Editor
{
	public class TBAlbumParser : SheetParserBase
	{
		protected override ESheetType SheetType        => ESheetType.TBAlbum;
		protected override string     SaveRelativePath => "TBAlbum/TBAlbumTable.asset";

		[MenuItem("Tools/Parsers/TB_Album")]
		public static void Parse()
		{
			new TBAlbumParser().Run();
		}

		protected override void ParseAndSave(string[][] data)
		{
			if (data.Length < 2)
			{
				Debug.LogWarning("[TBAlbumParser] 데이터가 없습니다.");
				return;
			}

			Dictionary<string, int> columnMap = BuildColumnMap(data[0]);
			List<TBAlbumData> items = new List<TBAlbumData>();

			for (int row = 1; row < data.Length; row++)
			{
				string[] cells = data[row];
				if (IsEmptyRow(cells))
				{
					continue;
				}

				TBAlbumData item = new TBAlbumData();
				item.AlbumGroupId    = GetInt(cells, columnMap, "AlbumGroupId");
				item.GroupNameId     = GetInt(cells, columnMap, "GroupNameId");
				item.GoldRewardCount = GetInt(cells, columnMap, "GoldRewardCount");
				item.Summary         = GetString(cells, columnMap, "Summary");

				items.Add(item);
			}

			SaveTable(items);
		}

		private void SaveTable(List<TBAlbumData> items)
		{
			TBAlbumTable table = AssetDatabase.LoadAssetAtPath<TBAlbumTable>(SavePath);
			if (table == null)
			{
				table = ScriptableObject.CreateInstance<TBAlbumTable>();
				string dir = Path.GetDirectoryName(SavePath);
				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
				AssetDatabase.CreateAsset(table, SavePath);
			}

			table.items = items.ToArray();
			EditorUtility.SetDirty(table);
			AssetDatabase.SaveAssets();

			Debug.Log($"[TBAlbumParser] 저장 완료 → {SavePath} ({items.Count}개)");
		}
	}
}
```

- [ ] **Step 3: TBPictureParser.cs 작성**

```csharp
using System.Collections.Generic;
using System.IO;
using TrumpTile.GameMain.Data;
using UnityEditor;
using UnityEngine;

namespace TrumpTile.Editor
{
	public class TBPictureParser : SheetParserBase
	{
		protected override ESheetType SheetType        => ESheetType.TBPicture;
		protected override string     SaveRelativePath => "TBPicture/TBPictureTable.asset";

		[MenuItem("Tools/Parsers/TB_Picture")]
		public static void Parse()
		{
			new TBPictureParser().Run();
		}

		protected override void ParseAndSave(string[][] data)
		{
			if (data.Length < 2)
			{
				Debug.LogWarning("[TBPictureParser] 데이터가 없습니다.");
				return;
			}

			Dictionary<string, int> columnMap = BuildColumnMap(data[0]);
			List<TBPictureData> items = new List<TBPictureData>();

			for (int row = 1; row < data.Length; row++)
			{
				string[] cells = data[row];
				if (IsEmptyRow(cells))
				{
					continue;
				}

				TBPictureData item = new TBPictureData();
				item.AlbumGroupId          = GetInt(cells, columnMap, "AlbumGroupId");
				item.PictureId             = GetInt(cells, columnMap, "PictureId");
				item.StageValue            = GetInt(cells, columnMap, "StageValue");
				item.PictureNameId         = GetInt(cells, columnMap, "PictureNameId");
				item.PictureDescriptionId  = GetInt(cells, columnMap, "PictureDescriptionId");
				item.HammerRewardCount     = GetInt(cells, columnMap, "HamerRewardCount");
				item.MagicStickRewardCount = GetInt(cells, columnMap, "MagicStickRewardCount");
				item.MagicHatRewardCount   = GetInt(cells, columnMap, "MagicHatRewardCount");
				item.BombRewardCount       = GetInt(cells, columnMap, "BombRewardCount");
				item.MainThumbnailSrc      = GetString(cells, columnMap, "MainThumbnailSrc");
				item.PictureThumbnailSrc   = GetString(cells, columnMap, "PictureThumbnailSrc");
				item.PictureBackgroundSrc  = GetString(cells, columnMap, "PictureBackgroundSrc");
				item.Summary               = GetString(cells, columnMap, "Summary");

				items.Add(item);
			}

			SaveTable(items);
		}

		private void SaveTable(List<TBPictureData> items)
		{
			TBPictureTable table = AssetDatabase.LoadAssetAtPath<TBPictureTable>(SavePath);
			if (table == null)
			{
				table = ScriptableObject.CreateInstance<TBPictureTable>();
				string dir = Path.GetDirectoryName(SavePath);
				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
				AssetDatabase.CreateAsset(table, SavePath);
			}

			table.items = items.ToArray();
			EditorUtility.SetDirty(table);
			AssetDatabase.SaveAssets();

			Debug.Log($"[TBPictureParser] 저장 완료 → {SavePath} ({items.Count}개)");
		}
	}
}
```

> **주의:** 시트 컬럼명 `HamerRewardCount`(오타)를 그대로 사용. 기획 시트와 맞춰야 함.

- [ ] **Step 4: 컴파일 확인 후 커밋**

```bash
git add Assets/_MainProject/Scripts/Editor/ESheetType.cs
git add Assets/_MainProject/Scripts/Editor/Parsers/TBAlbumParser.cs
git add Assets/_MainProject/Scripts/Editor/Parsers/TBPictureParser.cs
git commit -m "feat: TB_Album, TB_Picture 시트 파서 추가"
```

---

## Task 3: UserData 앨범 필드 + PlayerDataManager 앨범 메서드

**Files:**
- Modify: `Assets/_MainProject/Scripts/GameMain/Data/UserData.cs`
- Modify: `Assets/_MainProject/Scripts/GameMain/Data/PlayerDataManager.cs`

- [ ] **Step 1: UserData.cs에 앨범 필드 추가**

`UserData.cs`의 기존 하우징 데이터 필드 아래에 추가:

```csharp
// 앨범 수집 데이터
public int CurrentAlbumGroupId;
public Dictionary<int, List<int>> CollectedPictureIds; // AlbumGroupId → 수집한 PictureId 목록
public List<int> CompletedAlbumGroupIds;               // 챕터 완성 보상 수령 완료 그룹 목록
public bool HasPendingAlbumReward;
```

- [ ] **Step 2: UserData 생성자(딕셔너리 파싱)에 앨범 데이터 파싱 추가**

기존 `public UserData(Dictionary<object, object> dataDictionary)` 생성자 내부, `ReadLocalData()` 호출 직전에 추가:

```csharp
// 앨범 데이터 파싱
CollectedPictureIds   = new Dictionary<int, List<int>>();
CompletedAlbumGroupIds = new List<int>();
HasPendingAlbumReward = false;
CurrentAlbumGroupId   = 1;

if (dataDictionary.ContainsKey("albumData"))
{
	Dictionary<object, object> albumData = dataDictionary["albumData"] as Dictionary<object, object>;
	if (albumData != null)
	{
		if (albumData.ContainsKey("currentAlbumGroupId"))
		{
			CurrentAlbumGroupId = (int)Convert.ToInt64(albumData["currentAlbumGroupId"]);
		}
		if (albumData.ContainsKey("hasPendingAlbumReward"))
		{
			HasPendingAlbumReward = albumData["hasPendingAlbumReward"].ToString() == "True";
		}
		if (albumData.ContainsKey("collectedPictureIds"))
		{
			Dictionary<object, object> collected = albumData["collectedPictureIds"] as Dictionary<object, object>;
			if (collected != null)
			{
				foreach (KeyValuePair<object, object> pair in collected)
				{
					if (int.TryParse(pair.Key.ToString(), out int groupId))
					{
						List<int> pictureIds = new List<int>();
						if (pair.Value is List<object> idList)
						{
							foreach (object id in idList)
							{
								if (int.TryParse(id.ToString(), out int pid))
								{
									pictureIds.Add(pid);
								}
							}
						}
						CollectedPictureIds[groupId] = pictureIds;
					}
				}
			}
		}
		if (albumData.ContainsKey("completedAlbumGroupIds"))
		{
			if (albumData["completedAlbumGroupIds"] is List<object> completedList)
			{
				foreach (object id in completedList)
				{
					if (int.TryParse(id.ToString(), out int gid))
					{
						CompletedAlbumGroupIds.Add(gid);
					}
				}
			}
		}
	}
}
```

- [ ] **Step 3: UserData.InitOnlyLoacalData()에 앨범 기본값 추가**

기존 메서드 내부에 추가:

```csharp
CurrentAlbumGroupId    = 1;
CollectedPictureIds    = new Dictionary<int, List<int>>();
CompletedAlbumGroupIds = new List<int>();
HasPendingAlbumReward  = false;
```

- [ ] **Step 4: PlayerDataManager.cs에 앨범 프로퍼티 추가**

`#region 프로퍼티` 블록 내에 추가:

```csharp
public int                         CurrentAlbumGroupId    => mUserData != null ? mUserData.CurrentAlbumGroupId : 1;
public Dictionary<int, List<int>>  CollectedPictureIds    => mUserData?.CollectedPictureIds;
public List<int>                   CompletedAlbumGroupIds => mUserData?.CompletedAlbumGroupIds;
public bool                        HasPendingAlbumReward  => mUserData != null && mUserData.HasPendingAlbumReward;
```

- [ ] **Step 5: PlayerDataManager.cs에 앨범 메서드 추가**

`#region 컨텐츠 해금` 블록 뒤에 새 region 추가:

```csharp
#region 앨범 수집

public bool IsPictureCollected(int albumGroupId, int pictureId)
{
	if (mUserData?.CollectedPictureIds == null)
	{
		return false;
	}
	return mUserData.CollectedPictureIds.TryGetValue(albumGroupId, out List<int> ids)
		&& ids.Contains(pictureId);
}

public void AddCollectedPicture(int albumGroupId, int pictureId)
{
	if (mUserData == null)
	{
		return;
	}
	if (!mUserData.CollectedPictureIds.ContainsKey(albumGroupId))
	{
		mUserData.CollectedPictureIds[albumGroupId] = new List<int>();
	}
	if (!mUserData.CollectedPictureIds[albumGroupId].Contains(pictureId))
	{
		mUserData.CollectedPictureIds[albumGroupId].Add(pictureId);
	}
}

public void SetAlbumGroupComplete(int albumGroupId)
{
	if (mUserData == null)
	{
		return;
	}
	if (!mUserData.CompletedAlbumGroupIds.Contains(albumGroupId))
	{
		mUserData.CompletedAlbumGroupIds.Add(albumGroupId);
	}
	mUserData.CurrentAlbumGroupId = albumGroupId + 1;
}

public void SetPendingAlbumReward(bool bHasPending)
{
	if (mUserData == null)
	{
		return;
	}
	mUserData.HasPendingAlbumReward = bHasPending;
}

public List<int> GetCollectedPictureIds(int albumGroupId)
{
	if (mUserData?.CollectedPictureIds == null)
	{
		return new List<int>();
	}
	return mUserData.CollectedPictureIds.TryGetValue(albumGroupId, out List<int> ids)
		? ids
		: new List<int>();
}

#endregion
```

- [ ] **Step 6: 컴파일 확인 후 커밋**

```bash
git add Assets/_MainProject/Scripts/GameMain/Data/UserData.cs
git add Assets/_MainProject/Scripts/GameMain/Data/PlayerDataManager.cs
git commit -m "feat: UserData 앨범 필드 + PlayerDataManager 앨범 메서드 추가"
```

---

## Task 4: AlbumContent + EditMode 테스트

**Files:**
- Create: `Assets/_MainProject/Scripts/GameMain/Core/AlbumContent.cs`
- Create: `Assets/Tests/EditMode/AlbumSystemTests.cs`

- [ ] **Step 1: EAlbumPictureState enum 정의 — AlbumContent.cs 작성**

```csharp
using TrumpTile.GameMain.Data;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	public enum EAlbumPictureState
	{
		Locked,    // StageValue 미달 또는 이전 챕터 미완성
		Available, // StageValue 달성, 미수집
		Collected, // 수집 완료
	}

	[System.Serializable]
	public class AlbumContent : ContentBase
	{
		private const int UNLOCK_STAGE = 3; // CurrentStage >= 3 = 스테이지 2 클리어 완료

		public override void Initialize()
		{
			base.Initialize();

			if (PlayerDataManager.Inst != null && PlayerDataManager.Inst.CurrentStage >= UNLOCK_STAGE)
			{
				SetUnlock();
			}
		}

		/// <summary>
		/// 사진 한 장의 수집 상태를 반환한다.
		/// </summary>
		public static EAlbumPictureState GetPictureState(int pictureId, int stageValue, int currentStage, System.Collections.Generic.List<int> collectedIds)
		{
			if (collectedIds != null && collectedIds.Contains(pictureId))
			{
				return EAlbumPictureState.Collected;
			}
			if (currentStage >= stageValue)
			{
				return EAlbumPictureState.Available;
			}
			return EAlbumPictureState.Locked;
		}

		/// <summary>
		/// 그룹 내 모든 사진이 수집 완료되었는지 확인한다.
		/// </summary>
		public static bool IsChapterComplete(TBPictureData[] groupPictures, System.Collections.Generic.List<int> collectedIds)
		{
			if (groupPictures == null || groupPictures.Length == 0)
			{
				return false;
			}
			foreach (TBPictureData picture in groupPictures)
			{
				if (collectedIds == null || !collectedIds.Contains(picture.PictureId))
				{
					return false;
				}
			}
			return true;
		}
	}
}
```

- [ ] **Step 2: EditMode 테스트 파일 작성**

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;

namespace TrumpTile.Tests
{
	public class AlbumSystemTests
	{
		// --- GetPictureState 테스트 ---

		[Test]
		public void GetPictureState_WhenCollected_ReturnsCollected()
		{
			List<int> collected = new List<int> { 101 };
			EAlbumPictureState state = AlbumContent.GetPictureState(101, stageValue: 5, currentStage: 3, collected);
			Assert.AreEqual(EAlbumPictureState.Collected, state);
		}

		[Test]
		public void GetPictureState_WhenStageMetAndNotCollected_ReturnsAvailable()
		{
			List<int> collected = new List<int>();
			EAlbumPictureState state = AlbumContent.GetPictureState(101, stageValue: 5, currentStage: 5, collected);
			Assert.AreEqual(EAlbumPictureState.Available, state);
		}

		[Test]
		public void GetPictureState_WhenStageNotMet_ReturnsLocked()
		{
			List<int> collected = new List<int>();
			EAlbumPictureState state = AlbumContent.GetPictureState(101, stageValue: 10, currentStage: 5, collected);
			Assert.AreEqual(EAlbumPictureState.Locked, state);
		}

		[Test]
		public void GetPictureState_WhenCollectedListNull_DoesNotThrow()
		{
			EAlbumPictureState state = AlbumContent.GetPictureState(101, stageValue: 3, currentStage: 5, null);
			Assert.AreEqual(EAlbumPictureState.Available, state);
		}

		// --- IsChapterComplete 테스트 ---

		[Test]
		public void IsChapterComplete_WhenAllCollected_ReturnsTrue()
		{
			TBPictureData[] pictures = new TBPictureData[]
			{
				new TBPictureData { PictureId = 1 },
				new TBPictureData { PictureId = 2 },
			};
			List<int> collected = new List<int> { 1, 2 };
			Assert.IsTrue(AlbumContent.IsChapterComplete(pictures, collected));
		}

		[Test]
		public void IsChapterComplete_WhenPartiallyCollected_ReturnsFalse()
		{
			TBPictureData[] pictures = new TBPictureData[]
			{
				new TBPictureData { PictureId = 1 },
				new TBPictureData { PictureId = 2 },
			};
			List<int> collected = new List<int> { 1 };
			Assert.IsFalse(AlbumContent.IsChapterComplete(pictures, collected));
		}

		[Test]
		public void IsChapterComplete_WhenEmptyGroup_ReturnsFalse()
		{
			Assert.IsFalse(AlbumContent.IsChapterComplete(new TBPictureData[0], new List<int>()));
		}
	}
}
```

- [ ] **Step 3: Unity Editor에서 테스트 실행**

`Window > General > Test Runner > EditMode` 탭 열기 → `AlbumSystemTests` 선택 → Run 클릭.
예상 결과: 5개 테스트 모두 초록색(Pass).

- [ ] **Step 4: 커밋**

```bash
git add Assets/_MainProject/Scripts/GameMain/Core/AlbumContent.cs
git add Assets/Tests/EditMode/AlbumSystemTests.cs
git commit -m "feat: AlbumContent + EditMode 테스트 추가"
```

---

## Task 5: AlbumManager

**Files:**
- Create: `Assets/_MainProject/Scripts/GameMain/Core/AlbumManager.cs`

- [ ] **Step 1: AlbumManager.cs 작성**

```csharp
using System.Collections.Generic;
using TrumpTile.FrameLibrary;
using TrumpTile.GameMain.Data;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	public class AlbumManager : Singleton_GameObject<AlbumManager>
	{
		[Header("데이터 테이블")]
		[SerializeField] private TBAlbumTable   mAlbumTable;
		[SerializeField] private TBPictureTable mPictureTable;

		[Header("아이템 ID (TB_Item 기준)")]
		[SerializeField] private int mHammerItemId    = 1006;
		[SerializeField] private int mMagicStickItemId = 1005;
		[SerializeField] private int mMagicHatItemId  = 1007;
		[SerializeField] private int mBombItemId      = 1008;

		public event System.Action<TBPictureData> OnPictureCollected;
		public event System.Action<TBAlbumData>   OnChapterCompleted;

		private void Awake()
		{
			DontDestroyOnLoad(gameObject);
		}

		/// <summary>
		/// 스테이지 클리어 시 호출. 수집 가능한 사진이 생겼으면 대기 플래그를 설정한다.
		/// </summary>
		public void OnStageClear(int clearedStage)
		{
			if (mPictureTable == null || PlayerDataManager.Inst == null)
			{
				return;
			}

			int groupId = PlayerDataManager.Inst.CurrentAlbumGroupId;
			TBPictureData[] groupPictures = mPictureTable.GetByAlbumGroup(groupId);

			foreach (TBPictureData picture in groupPictures)
			{
				if (clearedStage >= picture.StageValue
					&& !PlayerDataManager.Inst.IsPictureCollected(groupId, picture.PictureId))
				{
					PlayerDataManager.Inst.SetPendingAlbumReward(true);
					return;
				}
			}
		}

		/// <summary>
		/// 메인 화면 진입 시 호출. 대기 보상이 있으면 콜백을 통해 AlbumPopup에 연출을 요청한다.
		/// </summary>
		public void CheckPendingReward(System.Action<List<TBPictureData>> onPendingFound)
		{
			if (!PlayerDataManager.Inst.HasPendingAlbumReward)
			{
				return;
			}

			int groupId = PlayerDataManager.Inst.CurrentAlbumGroupId;
			TBPictureData[] groupPictures = mPictureTable.GetByAlbumGroup(groupId);
			int currentStage = PlayerDataManager.Inst.CurrentStage;
			List<int> collectedIds = PlayerDataManager.Inst.GetCollectedPictureIds(groupId);

			List<TBPictureData> pendingPictures = new List<TBPictureData>();
			foreach (TBPictureData picture in groupPictures)
			{
				if (currentStage >= picture.StageValue
					&& !collectedIds.Contains(picture.PictureId))
				{
					pendingPictures.Add(picture);
				}
			}

			if (pendingPictures.Count > 0)
			{
				onPendingFound?.Invoke(pendingPictures);
			}
			else
			{
				PlayerDataManager.Inst.SetPendingAlbumReward(false);
			}
		}

		/// <summary>
		/// 사진 1장을 수집하고 보상을 지급한다. 챕터 완성 여부도 체크한다.
		/// </summary>
		public void CollectPicture(TBPictureData picture)
		{
			if (picture == null || PlayerDataManager.Inst == null)
			{
				return;
			}

			int groupId = picture.AlbumGroupId;

			PlayerDataManager.Inst.AddCollectedPicture(groupId, picture.PictureId);

			GrantItemRewards(picture);

			TBPictureData[] groupPictures = mPictureTable.GetByAlbumGroup(groupId);
			List<int> collectedIds = PlayerDataManager.Inst.GetCollectedPictureIds(groupId);

			if (AlbumContent.IsChapterComplete(groupPictures, collectedIds))
			{
				TBAlbumData albumData = mAlbumTable.GetById(groupId);
				if (albumData != null
					&& !PlayerDataManager.Inst.CompletedAlbumGroupIds.Contains(groupId))
				{
					PlayerDataManager.Inst.AddGold(albumData.GoldRewardCount);
					PlayerDataManager.Inst.SetAlbumGroupComplete(groupId);
					OnChapterCompleted?.Invoke(albumData);
				}
			}

			OnPictureCollected?.Invoke(picture);
		}

		/// <summary>
		/// 현재 앨범 그룹의 진행률을 반환한다. (수집 사진 수, 전체 사진 수)
		/// </summary>
		public (int collected, int total) GetCurrentProgress()
		{
			if (mPictureTable == null || PlayerDataManager.Inst == null)
			{
				return (0, 0);
			}

			int groupId = PlayerDataManager.Inst.CurrentAlbumGroupId;
			TBPictureData[] groupPictures = mPictureTable.GetByAlbumGroup(groupId);
			List<int> collectedIds = PlayerDataManager.Inst.GetCollectedPictureIds(groupId);

			int collected = 0;
			foreach (TBPictureData picture in groupPictures)
			{
				if (collectedIds.Contains(picture.PictureId))
				{
					collected++;
				}
			}

			return (collected, groupPictures.Length);
		}

		/// <summary>
		/// 현재 앨범 그룹의 사진 목록과 각각의 상태를 반환한다.
		/// </summary>
		public List<(TBPictureData picture, EAlbumPictureState state)> GetCurrentGroupPictureStates()
		{
			List<(TBPictureData, EAlbumPictureState)> result = new List<(TBPictureData, EAlbumPictureState)>();

			if (mPictureTable == null || PlayerDataManager.Inst == null)
			{
				return result;
			}

			int groupId = PlayerDataManager.Inst.CurrentAlbumGroupId;
			TBPictureData[] groupPictures = mPictureTable.GetByAlbumGroup(groupId);
			int currentStage = PlayerDataManager.Inst.CurrentStage;
			List<int> collectedIds = PlayerDataManager.Inst.GetCollectedPictureIds(groupId);

			foreach (TBPictureData picture in groupPictures)
			{
				EAlbumPictureState state = AlbumContent.GetPictureState(
					picture.PictureId, picture.StageValue, currentStage, collectedIds);
				result.Add((picture, state));
			}

			return result;
		}

		private void GrantItemRewards(TBPictureData picture)
		{
			if (picture.HammerRewardCount > 0)
			{
				PlayerDataManager.Inst.AddItemCount(mHammerItemId, picture.HammerRewardCount);
			}
			if (picture.MagicStickRewardCount > 0)
			{
				PlayerDataManager.Inst.AddItemCount(mMagicStickItemId, picture.MagicStickRewardCount);
			}
			if (picture.MagicHatRewardCount > 0)
			{
				PlayerDataManager.Inst.AddItemCount(mMagicHatItemId, picture.MagicHatRewardCount);
			}
			if (picture.BombRewardCount > 0)
			{
				PlayerDataManager.Inst.AddItemCount(mBombItemId, picture.BombRewardCount);
			}
		}
	}
}
```

> **Inspector 설정 필요:** AlbumManager GameObject 생성 후 `mAlbumTable`, `mPictureTable`에 SOData 에셋 연결. 아이템 ID는 TB_Item 테이블 기준으로 맞게 설정.

- [ ] **Step 2: 컴파일 확인 후 커밋**

```bash
git add Assets/_MainProject/Scripts/GameMain/Core/AlbumManager.cs
git commit -m "feat: AlbumManager 추가 - 사진 수집·보상 지급·진행률 관리"
```

---

## Task 6: GameManager LevelClear 연결

**Files:**
- Modify: `Assets/_MainProject/Scripts/GameMain/Core/GameManager.cs:579-629`

- [ ] **Step 1: SaveLevelProgress()에 AlbumManager 호출 추가**

`GameManager.cs`의 `SaveLevelProgress()` 메서드를 다음과 같이 수정:

```csharp
private void SaveLevelProgress(int level, int stars)
{
	Debug.Log($"[GameManager] SaveLevelProgress - Level: {level}, Stars: {stars}");

	PlayerDataManager.Inst.ClearStage(level, stars);
	AlbumManager.Inst?.OnStageClear(level); // 추가

	Debug.Log($"[GameManager] Saved - NextStage: {PlayerDataManager.Inst.CurrentStage}");
}
```

- [ ] **Step 2: 컴파일 확인 후 커밋**

```bash
git add Assets/_MainProject/Scripts/GameMain/Core/GameManager.cs
git commit -m "feat: 스테이지 클리어 시 AlbumManager 알림 연결"
```

---

## Task 7: AlbumPopup (하우징 UI) 구현

**Files:**
- Modify: `Assets/_MainProject/Scripts/GameMain/UI/AlbumPopup.cs`

> **Inspector 설정 전제:** Unity Prefab에서 아래 UI 요소들이 참조되어 있어야 함.
> - 게이지 Slider + 진행 텍스트 TMP_Text
> - 사진 썸네일 그리드 (AlbumSlotView 프리팹 배열)
> - 선물 상자 GameObject + CanvasGroup
> - 보상 아이콘 모음 GameObject
> - 골드 UI RectTransform, 스테이지 버튼 RectTransform (날아가기 타겟)

- [ ] **Step 1: AlbumPopup.cs 구현**

```csharp
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
	public class AlbumPopup : PopupBase
	{
		[Header("게이지")]
		[SerializeField] private Slider      mProgressSlider;
		[SerializeField] private TMP_Text    mProgressText;

		[Header("사진 슬롯 (그리드)")]
		[SerializeField] private AlbumSlotView[] mSlotViewArray;

		[Header("보상 연출")]
		[SerializeField] private GameObject  mGiftBoxObj;
		[SerializeField] private CanvasGroup mGiftBoxCanvasGroup;
		[SerializeField] private GameObject  mRewardIconsObj;
		[SerializeField] private RectTransform mGoldTargetRect;
		[SerializeField] private RectTransform mStageButtonTargetRect;

		[Header("다음 챕터 잠금")]
		[SerializeField] private GameObject  mLockIconObj;

		private CanvasGroup mPopupCanvasGroup;

		public override void Initialize()
		{
			base.Initialize();
			mPopupCanvasGroup = GetComponent<CanvasGroup>();
			if (mPopupCanvasGroup == null)
			{
				mPopupCanvasGroup = gameObject.AddComponent<CanvasGroup>();
			}
			RefreshUI();
		}

		public override void Show()
		{
			base.Show();
			RefreshUI();
		}

		private void RefreshUI()
		{
			if (AlbumManager.Inst == null)
			{
				return;
			}

			(int collected, int total) = AlbumManager.Inst.GetCurrentProgress();
			UpdateGauge(collected, total);

			List<(TBPictureData picture, EAlbumPictureState state)> pictureStates
				= AlbumManager.Inst.GetCurrentGroupPictureStates();

			for (int i = 0; i < mSlotViewArray.Length; i++)
			{
				if (i < pictureStates.Count)
				{
					mSlotViewArray[i].gameObject.SetActive(true);
					mSlotViewArray[i].Setup(pictureStates[i].picture, pictureStates[i].state, OnSlotClicked);
				}
				else
				{
					mSlotViewArray[i].gameObject.SetActive(false);
				}
			}
		}

		private void UpdateGauge(int collected, int total)
		{
			float ratio = total > 0 ? (float)collected / total : 0F;
			mProgressSlider.value = ratio;
			mProgressText.text    = $"{collected}/{total}";
		}

		/// <summary>
		/// 대기 중인 사진 목록을 순서대로 수집 연출 후 보상을 지급한다.
		/// </summary>
		public void PlayRewardSequence(List<TBPictureData> pendingPictures)
		{
			StartCoroutine(Co_RewardSequence(pendingPictures));
		}

		private IEnumerator Co_RewardSequence(List<TBPictureData> pendingPictures)
		{
			SetInteractable(false);

			foreach (TBPictureData picture in pendingPictures)
			{
				yield return StartCoroutine(Co_CollectOnePicture(picture));
			}

			PlayerDataManager.Inst.SetPendingAlbumReward(false);
			SetInteractable(true);
		}

		private IEnumerator Co_CollectOnePicture(TBPictureData picture)
		{
			// 게이지 채우기 전 보상 지급 (데이터 먼저 반영)
			AlbumManager.Inst.CollectPicture(picture);

			// 게이지 애니메이션
			(int collected, int total) = AlbumManager.Inst.GetCurrentProgress();
			float targetRatio = total > 0 ? (float)collected / total : 0F;
			yield return mProgressSlider.DOValue(targetRatio, 0.6F).SetEase(Ease.OutQuad).WaitForCompletion();
			mProgressText.text = $"{collected}/{total}";

			// 선물 상자 등장 및 흔들기
			mGiftBoxObj.SetActive(true);
			mGiftBoxCanvasGroup.alpha = 0F;
			mGiftBoxObj.transform.localScale = Vector3.zero;

			Sequence boxSeq = DOTween.Sequence();
			boxSeq.Append(mGiftBoxCanvasGroup.DOFade(1F, 0.2F));
			boxSeq.Join(mGiftBoxObj.transform.DOScale(1F, 0.3F).SetEase(Ease.OutBack));
			boxSeq.Append(mGiftBoxObj.transform.DOShakeRotation(0.5F, 15F, 10));
			yield return boxSeq.WaitForCompletion();

			// 상자 펑 → 보상 아이콘 등장
			mGiftBoxObj.transform.DOPunchScale(Vector3.one * 0.3F, 0.2F);
			yield return new WaitForSeconds(0.2F);
			mGiftBoxObj.SetActive(false);

			mRewardIconsObj.SetActive(true);
			mRewardIconsObj.transform.localScale = Vector3.zero;
			yield return mRewardIconsObj.transform.DOScale(1F, 0.3F).SetEase(Ease.OutBack).WaitForCompletion();
			yield return new WaitForSeconds(0.3F);

			// 보상 날아가기
			yield return StartCoroutine(Co_FlyRewardIcons(picture));

			mRewardIconsObj.SetActive(false);

			// 슬롯 UI 갱신
			RefreshUI();
			yield return new WaitForSeconds(0.2F);
		}

		private IEnumerator Co_FlyRewardIcons(TBPictureData picture)
		{
			bool bHasItem = picture.HammerRewardCount > 0
				|| picture.MagicStickRewardCount > 0
				|| picture.MagicHatRewardCount > 0
				|| picture.BombRewardCount > 0;

			List<Tween> tweens = new List<Tween>();

			if (bHasItem && mStageButtonTargetRect != null)
			{
				RectTransform iconRect = mRewardIconsObj.GetComponent<RectTransform>();
				tweens.Add(iconRect.DOMove(mStageButtonTargetRect.position, 0.5F).SetEase(Ease.InQuad));
			}

			foreach (Tween t in tweens)
			{
				yield return t.WaitForCompletion();
			}
		}

		private void OnSlotClicked(TBPictureData picture, EAlbumPictureState state)
		{
			switch (state)
			{
				case EAlbumPictureState.Locked:
					// TODO: "아직 수집할 수 없습니다" 메세지 팝업
					Debug.Log("[AlbumPopup] Locked: 아직 수집할 수 없습니다.");
					break;
				case EAlbumPictureState.Available:
					// TODO: 튜토리얼 가이드 표시
					Debug.Log("[AlbumPopup] Available: 튜토리얼 가이드 표시.");
					break;
				case EAlbumPictureState.Collected:
					AlbumPhotoPreviewPopup preview = FindObjectOfType<AlbumPhotoPreviewPopup>(true);
					if (preview != null)
					{
						preview.Setup(picture);
						preview.Show();
					}
					break;
			}
		}

		private void SetInteractable(bool bInteractable)
		{
			if (mPopupCanvasGroup != null)
			{
				mPopupCanvasGroup.interactable   = bInteractable;
				mPopupCanvasGroup.blocksRaycasts = bInteractable;
			}
		}
	}
}
```

- [ ] **Step 2: AlbumSlotView.cs 작성 (그리드 슬롯 단위 뷰)**

```csharp
using System;
using TrumpTile.GameMain.Core;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
	public class AlbumSlotView : MonoBehaviour
	{
		[SerializeField] private Image    mThumbnailImage;
		[SerializeField] private GameObject mLockIcon;
		[SerializeField] private GameObject mAvailableGlow;
		[SerializeField] private Button   mButton;

		private TBPictureData          mPictureData;
		private EAlbumPictureState     mState;
		private Action<TBPictureData, EAlbumPictureState> mOnClick;

		private void Awake()
		{
			mButton.onClick.AddListener(OnClick);
		}

		public void Setup(TBPictureData picture, EAlbumPictureState state, Action<TBPictureData, EAlbumPictureState> onClick)
		{
			mPictureData = picture;
			mState       = state;
			mOnClick     = onClick;

			mLockIcon.SetActive(state == EAlbumPictureState.Locked);
			mAvailableGlow.SetActive(state == EAlbumPictureState.Available);

			bool bShowThumbnail = state == EAlbumPictureState.Collected;
			mThumbnailImage.gameObject.SetActive(bShowThumbnail);

			if (bShowThumbnail && !string.IsNullOrEmpty(picture.PictureThumbnailSrc))
			{
				Sprite sprite = Resources.Load<Sprite>(picture.PictureThumbnailSrc);
				if (sprite != null)
				{
					mThumbnailImage.sprite = sprite;
				}
			}
		}

		private void OnClick()
		{
			mOnClick?.Invoke(mPictureData, mState);
		}
	}
}
```

- [ ] **Step 3: 컴파일 확인 후 커밋**

```bash
git add Assets/_MainProject/Scripts/GameMain/UI/AlbumPopup.cs
git add Assets/_MainProject/Scripts/GameMain/UI/AlbumSlotView.cs
git commit -m "feat: AlbumPopup 하우징 UI + 보상 연출 구현"
```

---

## Task 8: AlbumPhotoPreviewPopup

**Files:**
- Create: `Assets/_MainProject/Scripts/GameMain/UI/AlbumPhotoPreviewPopup.cs`

- [ ] **Step 1: AlbumPhotoPreviewPopup.cs 작성**

```csharp
using TMPro;
using TrumpTile.GameMain.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TrumpTile.GameMain.UI
{
	public class AlbumPhotoPreviewPopup : PopupBase
	{
		[Header("사진 프리뷰")]
		[SerializeField] private Image    mBackgroundImage;
		[SerializeField] private TMP_Text mTitleText;
		[SerializeField] private TMP_Text mDescriptionText;
		[SerializeField] private Button   mCloseButton;

		public override void Initialize()
		{
			base.Initialize();
			mCloseButton.onClick.AddListener(Hide);
			gameObject.SetActive(false);
		}

		public void Setup(TBPictureData picture)
		{
			if (!string.IsNullOrEmpty(picture.PictureBackgroundSrc))
			{
				Sprite bg = Resources.Load<Sprite>(picture.PictureBackgroundSrc);
				if (bg != null)
				{
					mBackgroundImage.sprite = bg;
				}
			}

			// TODO: StringMaster 로컬라이징 연동 시 PictureNameId/PictureDescriptionId로 텍스트 조회
			mTitleText.text       = $"Picture_{picture.PictureId}";
			mDescriptionText.text = string.Empty;
		}
	}
}
```

> **TODO:** StringMaster 로컬라이징 시스템 연동 후 `PictureNameId`, `PictureDescriptionId`로 실제 텍스트 조회 처리 필요.

- [ ] **Step 2: 컴파일 확인 후 커밋**

```bash
git add Assets/_MainProject/Scripts/GameMain/UI/AlbumPhotoPreviewPopup.cs
git commit -m "feat: AlbumPhotoPreviewPopup 추가"
```

---

## Task 9: 메인 화면 진입 시 보상 체크 연결

**Files:**
- Modify: `Assets/_MainProject/Scripts/GameMain/Core/MainManager.cs`

- [ ] **Step 1: MainManager.cs 수정**

기존 `Start()` 코루틴에 보상 체크 추가:

```csharp
using System.Collections;
using TrumpTile.GameMain.Data;
using TrumpTile.GameMain.UI;
using UnityEngine;

namespace TrumpTile.GameMain.Core
{
	public class MainManager : MonoBehaviour
	{
		[SerializeField] private AlbumPopup mAlbumPopup;

		private void Awake()
		{
			UIBase[] uiBaseArray = FindObjectsOfType<UIBase>(true);
			foreach (UIBase item in uiBaseArray)
			{
				item.Initialize();
			}
			_ = AdManager.Inst;
		}

		private IEnumerator Start()
		{
			yield return StartCoroutine(SceneTransister.Inst.Co_PlayFadeInAnim());
			AudioEvent.Play(EAudioKey.BGM_Main);
			EventManager.Inst.ActiveEvent("MainSceneLoadComplete");

			// 앨범 대기 보상 체크 (페이드인 완료 후 실행)
			yield return new WaitForSeconds(0.5F);
			CheckAlbumPendingReward();
		}

		private void CheckAlbumPendingReward()
		{
			if (AlbumManager.Inst == null || mAlbumPopup == null)
			{
				return;
			}

			AlbumManager.Inst.CheckPendingReward(pendingPictures =>
			{
				mAlbumPopup.Show();
				mAlbumPopup.PlayRewardSequence(pendingPictures);
			});
		}
	}
}
```

> **Inspector 설정 필요:** MainScene의 MainManager GameObject에서 `mAlbumPopup` 필드에 AlbumPopup 프리팹 연결.

- [ ] **Step 2: 컴파일 확인 후 커밋**

```bash
git add Assets/_MainProject/Scripts/GameMain/Core/MainManager.cs
git commit -m "feat: 메인 화면 진입 시 앨범 대기 보상 자동 체크"
```

---

## 최종 Unity 씬/인스펙터 설정 체크리스트

- [ ] MainScene에 `AlbumManager` GameObject 추가 (Singleton_GameObject, DontDestroyOnLoad)
  - `mAlbumTable` → `Assets/_MainProject/SODatas/TBAlbum/TBAlbumTable.asset` 연결
  - `mPictureTable` → `Assets/_MainProject/SODatas/TBPicture/TBPictureTable.asset` 연결
  - 아이템 ID 4개 TB_Item 기준으로 확인 후 설정
- [ ] MainScene UI에 `AlbumPopup` 프리팹 배치 및 필드 연결
- [ ] MainScene UI에 `AlbumPhotoPreviewPopup` 프리팹 배치
- [ ] MainScene의 `MainManager`에 `mAlbumPopup` 연결
- [ ] `ContentDatabase` SOData에 `AlbumContent` 항목 추가
- [ ] Google Sheets `TB_Album` 시트에 `GoldRewardCount` 컬럼 추가 후 `Tools > Parsers > TB_Album` 실행
- [ ] Google Sheets `TB_Picture` 시트 작성 후 `Tools > Parsers > TB_Picture` 실행
- [ ] Unity Test Runner EditMode에서 `AlbumSystemTests` 5개 모두 Pass 확인
