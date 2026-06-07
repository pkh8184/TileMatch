# 앨범 수집 콘텐츠 시스템 설계

**작성일:** 2026-06-07  
**상태:** 승인 완료

---

## 1. 목적

스테이지 클리어에 따라 앨범 사진을 수집하고 보상을 획득하는 콘텐츠.  
유저의 수집 욕구를 자극해 지속적인 플레이를 유도한다.

---

## 2. 핵심 규칙 정리

| 항목 | 내용 |
|------|------|
| 콘텐츠 해금 조건 | 스테이지 2 클리어 후 |
| 사진 수집 조건 | `CurrentStage >= picture.StageValue` |
| StageValue 의미 | 게임 스테이지 번호 (누적 클리어 횟수 아님) |
| 게이지 표기 | `수집한 사진 수 / 전체 사진 수` (정수) |
| 사진 보상 | 아이템 4종 (망치, 마술봉, 마술모자, 폭탄) |
| 챕터 완성 보상 | 골드 (TB_Album.GoldRewardCount) |
| 보상 수령 | 계정당 1회, Firebase 즉시 반영 |
| 하우징 UI | 앨범 팝업 자체가 하우징 UI (별도 화면 없음) |

---

## 3. 전체 아키텍처

```
[스테이지 클리어]
      ↓
GameManager.OnLevelClear()
      ↓
AlbumManager.OnStageClear(currentStage)
  → 수집 가능한 사진 탐색 (currentStage >= picture.StageValue)
  → HasPendingAlbumReward = true 설정
      ↓
[메인 화면 진입]
      ↓
AlbumManager.CheckPendingReward()
  → 대기 보상 있으면 AlbumPopup.PlayRewardSequence() 호출
      ↓
AlbumPopup
  → 게이지 채워지는 애니메이션
  → 선물 상자 오픈 연출
  → 보상 날아가기 애니메이션
  → (챕터 완성 시) 자물쇠 해제 + 다음 챕터 흰색 전환
```

---

## 4. 새로 만들 파일

| 파일 경로 | 역할 |
|-----------|------|
| `Scripts/GameMain/Core/AlbumContent.cs` | ContentBase 상속, 해금·수집 상태 관리 |
| `Scripts/GameMain/Core/AlbumManager.cs` | Singleton_GameObject, 게이지·보상·Firebase |
| `Scripts/GameMain/UI/AlbumPopup.cs` | 하우징 UI + 보상 연출 (기존 빈 파일 구현) |
| `Scripts/GameMain/UI/AlbumPhotoPreviewPopup.cs` | 사진 프리뷰 팝업 |
| `Scripts/GameMain/Data/TBAlbumData.cs` | TB_Album ScriptableObject |
| `Scripts/GameMain/Data/TBPictureData.cs` | TB_Picture ScriptableObject |
| `Scripts/Editor/Parsers/TBAlbumParser.cs` | TB_Album 시트 파서 |
| `Scripts/Editor/Parsers/TBPictureParser.cs` | TB_Picture 시트 파서 |

## 5. 기존 파일 수정

| 파일 | 수정 내용 |
|------|-----------|
| `UserData.cs` | 앨범 진행 데이터 필드 추가 |
| `GameManager.cs` | LevelClear() 내부에 AlbumManager.OnStageClear() 호출 추가 |
| `ESheetType.cs` | TB_Album, TB_Picture enum 추가 |
| Google Sheet TB_Album | GoldRewardCount 컬럼 추가 |

---

## 6. 데이터 레이어

### 6-1. UserData 추가 필드 (Firebase: albumData 노드)

```csharp
public ObscuredInt CurrentAlbumGroupId;
public Dictionary<int, List<int>> CollectedPictureIds; // AlbumGroupId → 수집한 PictureId 목록
public List<int> CompletedAlbumGroupIds;               // 챕터 완성 보상 수령 완료 그룹 목록
public bool HasPendingAlbumReward;                     // 수령 대기 중인 보상 존재 여부
```

### 6-2. TBAlbumData (ScriptableObject)

| 컬럼 | 타입 | 설명 |
|------|------|------|
| AlbumGroupId | int | 그룹 ID |
| GroupNameId | int | StringMaster 연결 키 |
| GoldRewardCount | int | 챕터 완성 시 골드 보상 |
| Summary | string | 기획 내부용 |

### 6-3. TBPictureData (ScriptableObject)

| 컬럼 | 타입 | 설명 |
|------|------|------|
| AlbumGroupId | int | 소속 그룹 ID |
| PictureId | int | 사진 고유 ID |
| StageValue | int | 수집 가능 게임 스테이지 번호 |
| PictureNameId | int | StringMaster 연결 키 (프리뷰 제목) |
| PictureDescriptionId | int | StringMaster 연결 키 (프리뷰 설명) |
| HammerRewardCount | int | 망치 보상 개수 |
| MagicStickRewardCount | int | 마술봉 보상 개수 |
| MagicHatRewardCount | int | 마술모자 보상 개수 |
| BombRewardCount | int | 폭탄 보상 개수 |
| MainThumbnailSrc | string | 메인화면 썸네일 경로 |
| PictureThumbnailSrc | string | 앨범 썸네일 경로 |
| PictureBackgroundSrc | string | 프리뷰 배경 경로 |
| Summary | string | 기획 내부용 |

---

## 7. 코어 로직

### 7-1. AlbumContent (ContentBase 상속)

```
- Initialize()         PlayerData에서 수집 상태 로드
- GetPictureState()    PictureId → EAlbumPictureState 반환
- IsChapterComplete()  현재 그룹 모든 사진 수집 완료 여부

EAlbumPictureState:
  Locked    → 이전 챕터 미완성 or CurrentStage < StageValue
  Available → CurrentStage >= StageValue, 미수집
  Collected → 수집 완료
```

### 7-2. AlbumManager (Singleton_GameObject)

```
- OnStageClear(int currentStage)
    └ 수집 가능 사진 탐색
    └ HasPendingAlbumReward = true

- CheckPendingReward()
    └ 메인 화면 진입 시 호출
    └ 대기 보상 있으면 AlbumPopup.PlayRewardSequence() 요청

- CollectPicture(int pictureId)
    └ CollectedPictureIds 갱신
    └ 챕터 완성 체크 → 골드 지급
    └ Firebase 동기화

- GetCurrentProgress(int albumGroupId)
    └ (수집 사진 수, 전체 사진 수) 반환
```

### 7-3. GameManager 연결 지점

```csharp
// 기존 LevelClear() 내부에 추가
AlbumManager.Inst.OnStageClear(PlayerDataManager.Inst.CurrentStage);
```

---

## 8. UI 레이어

### 8-1. AlbumPopup (하우징 UI)

**구성 요소:**
- 게이지 바 + 텍스트 (`수집 사진 수 / 전체 사진 수`)
- 사진 썸네일 그리드
  - Collected: 썸네일 이미지
  - Available: 썸네일 + 반짝이는 강조 효과
  - Locked: 자물쇠 아이콘 + 흰색 실루엣
- 사진 클릭 분기
  - Locked → "아직 수집할 수 없습니다" 메세지
  - Available → 콘텐츠 튜토리얼 가이드
  - Collected → AlbumPhotoPreviewPopup 오픈

**보상 연출 순서 (PlayRewardSequence):**
1. 게이지 바 채워지는 애니메이션
2. 선물 상자 등장 → 흔들리기
3. 상자 펑 → 아이템/골드 아이콘 등장
4. 아이템 → 스테이지 버튼으로 날아감 / 골드 → 골드 재화 UI로 날아감
5. (챕터 완성 시) 자물쇠 빛나며 사라짐 → 다음 챕터 흰색 실루엣으로 전환

**연출 중 조작 불가** (CanvasGroup.interactable = false)

### 8-2. AlbumPhotoPreviewPopup

**구성 요소:**
- 사진 배경 이미지 (PictureBackgroundSrc)
- 사진 제목 (PictureNameId → 로컬라이징)
- 사진 설명 (PictureDescriptionId → 로컬라이징)
- 닫기 버튼

---

## 9. 사진 수집 상태 분기 요약

| 상태 | 조건 | 클릭 시 동작 |
|------|------|-------------|
| Locked | 이전 챕터 미완성 OR StageValue 미달 | "아직 수집할 수 없습니다" |
| Available | StageValue 달성, 미수집 | 튜토리얼 가이드 표시 |
| Collected | 수집 완료 | 프리뷰 팝업 오픈 |
