# Extra Slot Unlock 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기본 6슬롯에서 골드 구매로 7번째 슬롯을 영구 해금하는 인게임 기능 구현

**Architecture:** `UserDataManager`에 해금 상태를 저장하고, `SlotManager.Initialize()`가 게임 시작 시 슬롯 수를 결정한다. 인게임에서 `LockedSlotView`(자물쇠 UI)를 터치하면 `ExtraSlotPurchasePopup`이 열려 구매를 처리한다. Hide 애니메이션 완료 후 슬롯 확장 및 자물쇠 비활성화가 처리된다.

**Tech Stack:** Unity C#, DOTween (PopupBase 애니메이션), PlayerPrefs (JsonUtility 직렬화), TMPro

**Spec:** `docs/superpowers/specs/2026-03-23-extra-slot-unlock-design.md`

---

## 파일 목록

| 파일 | 작업 |
|------|------|
| `Assets/_MainProject/Scripts/GameMain/UI/UIBase.cs` | 수정 |
| `Assets/_MainProject/Scripts/GameMain/UI/UserDataManager.cs` | 수정 |
| `Assets/_MainProject/Scripts/GameMain/Core/SlotManager.cs` | 수정 |
| `Assets/_MainProject/Scripts/GameMain/Core/GameManager.cs` | 수정 |
| `Assets/_MainProject/Scripts/GameMain/UI/LockedSlotView.cs` | 신규 |
| `Assets/_MainProject/Scripts/GameMain/UI/ExtraSlotPurchasePopup.cs` | 신규 |

---

## Task 1: UIBase null 체크

**Files:**
- Modify: `Assets/_MainProject/Scripts/GameMain/UI/UIBase.cs:25-26`

> 주의: 이 수정은 `ExtraSlotPurchasePopup`이 `PlayerDataManager` 없는 씬에서 안전하게 동작하기 위한 것. 기존 `ProfilePopup.Refresh()` 등 서브클래스 내부의 `PlayerDataManager.Inst` 호출은 이번 작업 범위 외.

- [ ] **Step 1: UIBase.Initialize()에 null 체크 추가**

`UIBase.cs` 25번 줄의 두 줄을 아래로 교체:

```csharp
if (PlayerDataManager.Inst != null)
{
    PlayerDataManager.Inst.OnPlayerDataRefresh += Refresh;
    PlayerDataManager.Inst.OnPlayerLocalDataRefresh += RefreshLocalData;
}
```

- [ ] **Step 2: 수동 검증**

Unity Editor에서 Play → 콘솔에 NullReferenceException 없는지 확인

- [ ] **Step 3: 커밋**

```bash
git add Assets/_MainProject/Scripts/GameMain/UI/UIBase.cs
git commit -m "[클라] UIBase PlayerDataManager null 체크 추가"
```

---

## Task 2: UserDataManager - 해금 데이터

**Files:**
- Modify: `Assets/_MainProject/Scripts/GameMain/UI/UserDataManager.cs`

- [ ] **Step 1: 필드 및 프로퍼티 추가**

`[Header("아이템 보유량")]` 섹션 아래에 추가:

```csharp
[Header("슬롯 해금")]
[SerializeField] private bool mExtraSlotUnlocked = false;

public bool IsExtraSlotUnlocked => mExtraSlotUnlocked;
```

- [ ] **Step 2: PurchaseExtraSlot 메서드 추가**

`#region 아이템` 섹션 안에 추가:

```csharp
/// <summary>
/// 추가 슬롯 구매 (골드 차감 + 영구 해금)
/// </summary>
public bool PurchaseExtraSlot(int goldCost)
{
    if (mExtraSlotUnlocked)
    {
        return false;
    }

    if (!UseGold(goldCost))
    {
        return false;
    }

    mExtraSlotUnlocked = true;
    SaveData();
    return true;
}
```

- [ ] **Step 3: UserSaveData 직렬화 필드 추가**

`UserSaveData` 클래스 마지막에 추가:

```csharp
public bool extraSlotUnlocked;
```

- [ ] **Step 4: SaveData() 직렬화 추가**

`SaveData()` 내 `UserSaveData` 초기화 블록에 추가:

```csharp
extraSlotUnlocked = this.mExtraSlotUnlocked,
```

- [ ] **Step 5: LoadData() 역직렬화 추가**

`LoadData()` 내 복원 블록에 추가:

```csharp
mExtraSlotUnlocked = saveData.extraSlotUnlocked;
```

- [ ] **Step 6: ResetData()에 초기화 추가**

`ResetData()` 내에 추가:

```csharp
mExtraSlotUnlocked = false;
```

- [ ] **Step 7: 수동 검증**

Unity Editor Play → Inspector에서 `mExtraSlotUnlocked` true로 설정 → Play 종료 후 재진입 시 값 유지되는지 확인

- [ ] **Step 8: 커밋**

```bash
git add Assets/_MainProject/Scripts/GameMain/UI/UserDataManager.cs
git commit -m "[클라] UserDataManager 추가 슬롯 해금 데이터 추가"
```

---

## Task 3: SlotManager - 기본값 6, Initialize()

**Files:**
- Modify: `Assets/_MainProject/Scripts/GameMain/Core/SlotManager.cs:21`

- [ ] **Step 1: mMaxSlots 기본값 변경**

21번 줄:

```csharp
// 변경 전
[SerializeField] private int mMaxSlots = 7;

// 변경 후
[SerializeField] private int mMaxSlots = 6;
```

- [ ] **Step 2: Initialize() 메서드 추가**

`#region Public Methods` 안 `ResetSlots()` 위에 추가:

```csharp
/// <summary>
/// 게임 시작 시 해금 상태에 따라 슬롯 수 초기화
/// ResetSlots() 이전에 반드시 호출
/// </summary>
public void Initialize()
{
    bool bUnlocked = UserDataManager.Instance != null && UserDataManager.Instance.IsExtraSlotUnlocked;
    SetSlotCount(bUnlocked ? 7 : 6);
    Debug.Log($"[SlotManager] Initialize - MaxSlots: {mMaxSlots}, ExtraSlotUnlocked: {bUnlocked}");
}
```

- [ ] **Step 3: 수동 검증**

Play → 콘솔에서 `[SlotManager] Initialize - MaxSlots: 6` 로그 확인

- [ ] **Step 4: 커밋**

```bash
git add Assets/_MainProject/Scripts/GameMain/Core/SlotManager.cs
git commit -m "[클라] SlotManager 기본 슬롯 6개, Initialize() 추가"
```

---

## Task 4: GameManager - Initialize() 호출 순서

**Files:**
- Modify: `Assets/_MainProject/Scripts/GameMain/Core/GameManager.cs:36,195`

> 참고: `GameManager.mMaxSlots`는 현재 `SlotManager`에 직접 전달되지 않는 독립 Inspector 필드. `SlotManager.Initialize()`가 슬롯 수를 결정하므로 두 값을 6으로 동기화하여 혼란을 방지.

- [ ] **Step 1: mMaxSlots 기본값 변경**

36번 줄:

```csharp
// 변경 전
[SerializeField] private int mMaxSlots = 7;

// 변경 후
[SerializeField] private int mMaxSlots = 6;
```

- [ ] **Step 2: StartLevelAsync()에 Initialize() 호출 추가**

195번 줄 `mSlotManager?.ResetSlots();` 바로 위에 추가:

```csharp
mSlotManager?.Initialize();  // 반드시 ResetSlots() 이전
mSlotManager?.ResetSlots();
```

- [ ] **Step 3: 수동 검증**

Play → 콘솔에서 Initialize 로그가 ResetSlots 로그보다 먼저 출력되는지 확인

- [ ] **Step 4: 커밋**

```bash
git add Assets/_MainProject/Scripts/GameMain/Core/GameManager.cs
git commit -m "[클라] GameManager SlotManager.Initialize() 호출 추가"
```

---

## Task 5: LockedSlotView - 자물쇠 UI

**Files:**
- Create: `Assets/_MainProject/Scripts/GameMain/UI/LockedSlotView.cs`

씬 구성: 7번째 슬롯 위치에 새 GameObject 생성 → 이 스크립트 부착 → 자물쇠 Image 자식으로 추가 → Inspector에서 `mPurchasePopup` 연결

- [ ] **Step 1: LockedSlotView.cs 생성**

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using TrumpTile.GameMain.Core;

namespace TrumpTile.GameMain.UI
{
    /// <summary>
    /// 인게임 7번째 슬롯 잠금 UI
    /// 터치 시 ExtraSlotPurchasePopup 오픈
    /// </summary>
    public class LockedSlotView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private ExtraSlotPurchasePopup mPurchasePopup;

        private void Start()
        {
            bool bUnlocked = UserDataManager.Instance != null && UserDataManager.Instance.IsExtraSlotUnlocked;
            if (bUnlocked)
            {
                gameObject.SetActive(false);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (SlotManager.Instance != null && SlotManager.Instance.IsProcessing)
            {
                return;
            }

            mPurchasePopup?.Show();
        }

        /// <summary>
        /// 구매 완료 후 ExtraSlotPurchasePopup에서 호출
        /// </summary>
        public void OnUnlocked()
        {
            gameObject.SetActive(false);
        }
    }
}
```

- [ ] **Step 2: 수동 검증**

Play → 7번째 슬롯 클릭 시 팝업 오픈 확인, IsProcessing 중엔 열리지 않는지 확인

- [ ] **Step 3: 커밋**

```bash
git add Assets/_MainProject/Scripts/GameMain/UI/LockedSlotView.cs
git commit -m "[클라] LockedSlotView 자물쇠 슬롯 UI 추가"
```

---

## Task 6: ExtraSlotPurchasePopup - 구매 팝업

**Files:**
- Create: `Assets/_MainProject/Scripts/GameMain/UI/ExtraSlotPurchasePopup.cs`

씬 구성: Canvas 하위에 새 GameObject → 이 스크립트 부착 → background/내용 UI 구성 → Inspector에서 `mLockedSlotView`, `mExtraSlotGoldPrice`, `mUnlockDelay`, 텍스트/버튼 연결

> `mUnlockDelay`는 `PopupBase.mHideDuration`과 동일하게 Inspector에서 맞춰 설정. `mHideDuration`이 private이라 직접 읽을 수 없으므로 별도 필드로 관리.

- [ ] **Step 1: ExtraSlotPurchasePopup.cs 생성**

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TrumpTile.GameMain.Core;

namespace TrumpTile.GameMain.UI
{
    /// <summary>
    /// 추가 슬롯 구매 팝업
    /// PopupBase 상속 - DOTween Show/Hide 애니메이션 자동 적용
    /// </summary>
    public class ExtraSlotPurchasePopup : PopupBase
    {
        [Header("슬롯 구매 설정")]
        [SerializeField] private int mExtraSlotGoldPrice = 500;
        [SerializeField] private LockedSlotView mLockedSlotView;
        [Header("Hide 애니메이션 완료 대기 시간 (PopupBase.mHideDuration과 동일하게 설정)")]
        [SerializeField] private float mUnlockDelay = 1F;

        [Header("UI 요소")]
        [SerializeField] private TMP_Text mPriceText;
        [SerializeField] private TMP_Text mCurrentGoldText;
        [SerializeField] private TMP_Text mNoticeText;
        [SerializeField] private Button mConfirmButton;
        [SerializeField] private Button mCancelButton;

        private void Awake()
        {
            if (mConfirmButton != null)
            {
                mConfirmButton.onClick.AddListener(OnConfirm);
            }

            if (mCancelButton != null)
            {
                mCancelButton.onClick.AddListener(OnCancel);
            }
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (mPriceText != null)
            {
                mPriceText.text = $"{mExtraSlotGoldPrice:N0}G";
            }

            if (mCurrentGoldText != null && UserDataManager.Instance != null)
            {
                mCurrentGoldText.text = $"보유 골드: {UserDataManager.Instance.Gold:N0}G";
            }

            if (mNoticeText != null)
            {
                mNoticeText.gameObject.SetActive(false);
            }
        }

        private void OnConfirm()
        {
            bool bSuccess = UserDataManager.Instance != null &&
                            UserDataManager.Instance.PurchaseExtraSlot(mExtraSlotGoldPrice);

            if (bSuccess)
            {
                Hide();
                StartCoroutine(UnlockAfterHide());
            }
            else
            {
                if (mNoticeText != null)
                {
                    mNoticeText.gameObject.SetActive(true);
                    mNoticeText.text = "골드가 부족합니다.";
                }
            }
        }

        private IEnumerator UnlockAfterHide()
        {
            yield return new WaitForSeconds(mUnlockDelay);
            SlotManager.Instance?.SetSlotCount(7);
            mLockedSlotView?.OnUnlocked();
        }

        private void OnCancel()
        {
            Hide();
        }
    }
}
```

- [ ] **Step 2: 수동 검증 - 정상 구매**

1. Play → 7번째 슬롯 클릭 → 팝업 오픈 확인
2. 확인 버튼 클릭 → Hide 애니메이션 완료 후 슬롯 7개로 늘어나고 자물쇠 사라지는지 확인
3. 골드가 `mExtraSlotGoldPrice`만큼 차감됐는지 Inspector에서 확인

- [ ] **Step 3: 수동 검증 - 골드 부족**

1. `UserDataManager.mGold` Inspector에서 0으로 설정
2. 팝업 열기 → 확인 클릭 → "골드가 부족합니다." 텍스트 출력 확인, 슬롯 변화 없음 확인

- [ ] **Step 4: 수동 검증 - 영구 해금**

1. 구매 완료 후 Play 종료 → 재진입
2. 7번째 슬롯이 처음부터 활성화되어 있고 자물쇠 없는지 확인

- [ ] **Step 5: 커밋**

```bash
git add Assets/_MainProject/Scripts/GameMain/UI/ExtraSlotPurchasePopup.cs
git commit -m "[클라] ExtraSlotPurchasePopup 추가 슬롯 구매 팝업 구현"
```

---

## 씬 설정 체크리스트

구현 완료 후 Unity 씬에서 수동으로 처리해야 하는 항목:

- [ ] 7번째 슬롯 위치에 `LockedSlotView` GameObject 생성 및 배치
- [ ] 자물쇠 아이콘 Image 자식으로 추가
- [ ] Canvas 하위에 `ExtraSlotPurchasePopup` GameObject 생성 (비활성화 상태로 배치)
- [ ] `PopupBase`의 `background` 필드에 배경 오브젝트 연결
- [ ] `LockedSlotView.mPurchasePopup` → `ExtraSlotPurchasePopup` 연결
- [ ] `ExtraSlotPurchasePopup.mLockedSlotView` → `LockedSlotView` 연결 (두 오브젝트가 서로 참조)
- [ ] `ExtraSlotPurchasePopup.mExtraSlotGoldPrice` 가격 설정
- [ ] `ExtraSlotPurchasePopup.mUnlockDelay` → `PopupBase.mHideDuration`과 동일하게 설정
- [ ] `SlotManager.mSlotPositions` 배열에 7번째 Transform 연결 확인
