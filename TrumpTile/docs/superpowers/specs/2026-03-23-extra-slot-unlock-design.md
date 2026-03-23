# Extra Slot Unlock 기능 설계

## 개요

기본 슬롯 수를 6개로 설정하고, 사용자가 골드로 구매 시 7번째 슬롯을 영구 해금하는 기능.

## 요구사항

- 기본 슬롯: 6개
- 해금 슬롯: 7번째 (골드로 영구 구매)
- 인게임에서 잠긴 7번째 슬롯 터치 → 구매 팝업
- 구매 후 즉시 적용, 이후 모든 게임에서 유지

## 컴포넌트 설계

### 1. UIBase 수정

`UIBase.Initialize()`의 `PlayerDataManager.Inst` 접근에 null 체크 추가.

```csharp
// 기존 (문제)
PlayerDataManager.Inst.OnPlayerDataRefresh += Refresh;

// 변경 후
if (PlayerDataManager.Inst != null)
{
    PlayerDataManager.Inst.OnPlayerDataRefresh += Refresh;
    PlayerDataManager.Inst.OnPlayerLocalDataRefresh += RefreshLocalData;
}
```

이로써 `ExtraSlotPurchasePopup`을 포함한 모든 `UIBase` 서브클래스가 `PlayerDataManager` 초기화 순서와 무관하게 안전하게 동작함.

### 2. 데이터 레이어

**`UserDataManager`**
- 필드: `bool mExtraSlotUnlocked = false`
- 프로퍼티: `bool IsExtraSlotUnlocked => mExtraSlotUnlocked`
- 메서드: `bool PurchaseExtraSlot(int goldCost)`
  - 이미 해금된 경우 false 반환
  - 골드 부족 시 false 반환 (`UseGold()` 위임)
  - 성공 시 `mExtraSlotUnlocked = true` 후 `SaveData()` 호출
- `UserSaveData` 직렬화 쌍 (반드시 함께 구현):
  - `SaveData()`: `extraSlotUnlocked = this.mExtraSlotUnlocked`
  - `LoadData()`: `mExtraSlotUnlocked = saveData.extraSlotUnlocked`
  - `ResetData()`: `mExtraSlotUnlocked = false`
- 기존 유저 하위 호환: `JsonUtility.FromJson` 미존재 필드는 false(기본값) 처리됨

> `EItemType`에 `ExtraSlot`을 추가하지 않음. 소모성 아이템이 아닌 영구 해금 상태이므로 `UserDataManager`에서 별도 필드로 관리.

### 3. SlotManager

- `mMaxSlots` Inspector 기본값: 7 → **6** 으로 변경
- `Initialize()` 메서드 신규 추가
  - `UserDataManager.IsExtraSlotUnlocked`를 읽어 `SetSlotCount(6 또는 7)` 호출
- `mSlotPositions` 배열은 씬에서 7개 유지 (7번째 Transform은 실제 슬롯 위치)
- `LockedSlotView`는 7번째 슬롯 위치에 별도 오버레이 GameObject (슬롯 Transform과 독립)

### 4. GameManager

- `mMaxSlots` 필드: 7 → **6** 으로 변경 (`GameManager.cs` 36번 줄)
- `StartLevelAsync()` 내 호출 순서:
  1. `SlotManager.Initialize()` ← **반드시 ResetSlots() 이전** (슬롯 수 결정)
  2. `SlotManager.ResetSlots()`

### 5. LockedSlotView (신규)

- 7번째 슬롯 위치에 별도 오버레이 GameObject로 배치
- 자물쇠 아이콘 Image
- `Start()`에서 자체 초기화 (`SlotManager`와 무관하게 독립 실행):
  - `UserDataManager.IsExtraSlotUnlocked == true` → `gameObject.SetActive(false)`
  - false → 자물쇠 아이콘 활성화
- 터치 시 (`IPointerClickHandler.OnPointerClick()`):
  - `SlotManager.Instance.IsProcessing == true` → 무시
  - `ExtraSlotPurchasePopup.Show()` 호출
- `SlotManager`는 `LockedSlotView`를 직접 참조하지 않음

### 6. ExtraSlotPurchasePopup (신규)

- `PopupBase` 상속, `Initialize()` 오버라이드 없음 (UIBase 수정으로 안전해짐)
- 씬에 미리 배치
- Inspector 필드: `int mExtraSlotGoldPrice`, `LockedSlotView mLockedSlotView`
- 골드 표시: `UserDataManager.Gold`
- 표시 내용: 슬롯 해금 설명, 골드 가격
- 버튼: 확인(구매) / 취소
- 확인 시:
  1. `UserDataManager.PurchaseExtraSlot(mExtraSlotGoldPrice)` 호출
  2. 성공: 팝업 닫기(`Hide()`) → Hide 애니메이션 완료 콜백에서 `SlotManager.SetSlotCount(7)` + `mLockedSlotView.gameObject.SetActive(false)` 처리 (애니메이션 도중 상태 변경 방지)
  3. 실패(골드 부족): 골드 부족 안내 텍스트 표시
- 골드 가격은 `DataManager.ItemTable` 미사용, Inspector `mExtraSlotGoldPrice`로 설정

## 데이터 흐름

```
[게임 시작]
  → GameManager.StartLevelAsync()
    → SlotManager.Initialize()   ← ResetSlots() 이전 필수
      → UserDataManager.IsExtraSlotUnlocked
        → true: SetSlotCount(7)
        → false: SetSlotCount(6)
    → SlotManager.ResetSlots()
  → LockedSlotView.Start() [독립 실행, SlotManager와 무관]
    → UserDataManager.IsExtraSlotUnlocked
      → true: gameObject.SetActive(false)
      → false: 자물쇠 아이콘 활성

[인게임 잠금 슬롯 터치]
  → LockedSlotView.OnPointerClick()
    → SlotManager.IsProcessing == true → 무시
    → ExtraSlotPurchasePopup.Show()
      → [확인] → UserDataManager.PurchaseExtraSlot(mExtraSlotGoldPrice)
        → 성공: Hide() → 애니메이션 완료 후 SlotManager.SetSlotCount(7) + LockedSlotView.SetActive(false)
        → 실패: 골드 부족 안내
      → [취소] → Hide()

[재시작/다음 게임]
  → SlotManager.Initialize() → PlayerPrefs에서 해금 상태 복원
```

## 변경 파일 목록

| 파일 | 변경 유형 | 주요 내용 |
|------|----------|----------|
| `UIBase.cs` | 수정 | `PlayerDataManager.Inst` null 체크 추가 |
| `UserDataManager.cs` | 수정 | 해금 필드/메서드/SaveData/LoadData/ResetData 추가 |
| `SlotManager.cs` | 수정 | mMaxSlots 기본값 6, Initialize() 추가 |
| `GameManager.cs` | 수정 | mMaxSlots 기본값 6, StartLevelAsync()에 Initialize() 호출 추가 |
| `LockedSlotView.cs` | 신규 | 자물쇠 UI, 터치 처리 |
| `ExtraSlotPurchasePopup.cs` | 신규 | 구매 팝업, PopupBase 상속 |
