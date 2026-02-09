# 🎴 Tile Match Level Editor

트럼프 카드 스타일 타일 매칭 게임을 위한 Unity 레벨 에디터 시스템입니다.

## 📁 프로젝트 구조

```
Scripts/LevelEditor/
├── LevelData.cs                    # 레벨 데이터 구조
├── Editor/
│   ├── LevelEditorWindow.cs        # 메인 레벨 에디터
│   ├── LevelPreviewWindow.cs       # 3D 프리뷰 & 인스펙터
│   ├── LevelGeneratorWindow.cs     # 자동 레벨 생성기
│   └── LevelEditorUtilities.cs     # 유틸리티 & 브라우저
```

---

## 🚀 시작하기

### 1. 설치

1. `Scripts/LevelEditor` 폴더를 Unity 프로젝트의 `Assets/Scripts/` 에 복사
2. Unity 에디터가 스크립트를 컴파일할 때까지 대기
3. 메뉴에서 `Tools > Tile Match` 확인

### 2. 첫 번째 레벨 만들기

1. **Tools > Tile Match > Level Editor** 열기 (단축키: `Ctrl+Shift+L`)
2. **New** 버튼 클릭
3. 레벨 저장 위치 선택
4. 타일 팔레트에서 카드 선택 후 그리드에 클릭하여 배치

---

## 🛠 주요 기능

### Level Editor Window (레벨 에디터)

| 기능 | 설명 |
|------|------|
| **Select (V)** | 타일 선택, 드래그로 다중 선택 |
| **Paint (B)** | 선택한 타일 타입으로 페인팅 |
| **Erase (E)** | 타일 삭제 |
| **Fill (G)** | 영역 채우기 |
| **Eyedropper (I)** | 기존 타일 타입 가져오기 |

#### 레이어 시스템
- 최대 5개 레이어 (0-4)
- 숫자 키 1-5로 레이어 전환
- 상위 레이어 타일이 하위 타일을 덮음
- "Show All" 토글로 모든 레이어 표시

#### 단축키

| 단축키 | 동작 |
|--------|------|
| `Ctrl+Z` | Undo |
| `Ctrl+Shift+Z` / `Ctrl+Y` | Redo |
| `Ctrl+S` | 저장 |
| `Ctrl+A` | 현재 레이어 전체 선택 |
| `Delete` / `Backspace` | 선택 타일 삭제 |
| `Escape` | 선택 해제 |
| `1-5` | 레이어 전환 |
| `V/B/E/G/I` | 도구 전환 |
| 마우스 휠 | 줌 |
| 휠 클릭 드래그 | 패닝 |

---

### Level Generator (레벨 생성기)

**Tools > Tile Match > Level Generator**

자동으로 여러 레벨을 한 번에 생성합니다.

#### 패턴 타입

| 패턴 | 설명 |
|------|------|
| **Pyramid** | 피라미드 형태, 위로 갈수록 좁아짐 |
| **Diamond** | 다이아몬드/마름모 형태 |
| **Rectangle** | 사각형, 밀도 조절 가능 |
| **Cross** | 십자가 형태 |
| **Heart** | 하트 모양 (특별 이벤트용) |
| **Star** | 5각 별 모양 |
| **Spiral** | 나선형 |
| **Random** | 무작위 배치 |

#### 난이도 스케일링

- **Difficulty Curve**: 레벨 진행에 따른 난이도 증가 곡선
- **Tile Types**: 사용되는 타일 종류 수 (적을수록 쉬움)
- **Special Tiles**: 레벨이 높아질수록 특수 타일 비율 증가

---

### Level Browser (레벨 브라우저)

**Tools > Tile Match > Level Browser**

프로젝트의 모든 레벨을 한눈에 관리합니다.

- 검색 및 필터링
- 난이도별 분류
- 유효성 검사 상태 표시
- 빠른 편집/미리보기 접근

---

### Level Preview (3D 미리보기)

**Tools > Tile Match > Level Preview**

레벨을 3D로 미리 확인합니다.

- 좌클릭 드래그: 회전
- 우클릭 드래그: 패닝
- 스크롤: 줌
- 레벨 데이터 드래그 앤 드롭 지원

---

## 📊 레벨 데이터 구조

### LevelData (ScriptableObject)

```csharp
// 기본 정보
int levelNumber;           // 레벨 번호
string levelName;          // 레벨 이름
LevelDifficulty difficulty; // 난이도

// 보드 설정
int boardWidth;            // 보드 너비 (4-12)
int boardHeight;           // 보드 높이 (4-12)
int maxLayers;             // 최대 레이어 수 (1-5)

// 게임 규칙
int slotCount;             // 하단 슬롯 수 (기본 7)
int matchCount;            // 매칭 필요 수 (기본 3)
float timeLimit;           // 제한 시간 (0 = 무제한)
int targetScore;           // 목표 점수

// 타일 배치
List<TilePlacement> tilePlacements;

// 아이템 설정
int initialShuffleCount;   // 셔플 아이템
int initialUndoCount;      // 되돌리기 아이템
int initialHintCount;      // 힌트 아이템
```

### TilePlacement

```csharp
int gridX;                 // X 좌표
int gridY;                 // Y 좌표
int layer;                 // 레이어 (0 = 바닥)
string tileTypeId;         // 타일 타입 ID (예: "Spade_Ace")
bool isLocked;             // 잠금 상태
bool isFrozen;             // 얼음 상태
int frozenCount;           // 얼음 두께 (1-3)
```

---

## 🎨 특수 타일

### Frozen (얼음) ❄

- 여러 번 탭해야 선택 가능
- `frozenCount`로 얼음 두께 설정
- 시각적으로 얼음 효과 표시

### Locked (잠금) 🔒

- 특정 조건 만족 시 해제
- 게임 로직에서 조건 구현 필요

---

## ✅ 유효성 검사

레벨 저장 시 자동으로 검사되는 항목:

1. **타일 수**: `matchCount`의 배수여야 함
2. **타일 타입 균형**: 각 타입이 `matchCount`의 배수
3. **보드 범위**: 모든 타일이 보드 내에 있어야 함

```csharp
// 코드에서 검증
string errorMessage;
if (!levelData.Validate(out errorMessage))
{
    Debug.LogError(errorMessage);
}
```

---

## 📤 내보내기 / 가져오기

### JSON 내보내기

**Tools > Tile Match > Export Levels to JSON**

모든 레벨을 JSON 형식으로 내보냅니다. 다른 프로젝트나 버전 관리에 유용합니다.

### JSON 가져오기

**Tools > Tile Match > Import Levels from JSON**

JSON 파일에서 레벨을 가져옵니다.

---

## 🔧 커스터마이징

### 새로운 패턴 추가

`LevelGeneratorWindow.cs`에서 새 패턴 메서드 추가:

```csharp
private List<TilePlacement> GenerateCustomPattern(int width, int height, int maxLayer)
{
    List<TilePlacement> placements = new List<TilePlacement>();
    
    // 패턴 로직 구현
    
    return placements;
}
```

### 새로운 특수 타일 추가

1. `LevelData.cs`의 `SpecialTileType` enum에 추가
2. `TilePlacement`에 관련 필드 추가
3. 에디터 UI 업데이트
4. 게임 로직에서 처리

---

## 📝 게임과 연동

### 레벨 로드

```csharp
public class GameManager : MonoBehaviour
{
    public void LoadLevel(LevelData levelData)
    {
        // 보드 크기 설정
        boardManager.SetSize(levelData.boardWidth, levelData.boardHeight);
        
        // 타일 생성
        foreach (var placement in levelData.tilePlacements)
        {
            var tile = CreateTile(placement.tileTypeId);
            tile.SetPosition(placement.gridX, placement.gridY, placement.layer);
            tile.SetFrozen(placement.isFrozen, placement.frozenCount);
            tile.SetLocked(placement.isLocked);
        }
        
        // 슬롯 설정
        slotManager.SetSlotCount(levelData.slotCount);
        
        // 아이템 초기화
        itemManager.Initialize(
            levelData.initialShuffleCount,
            levelData.initialUndoCount,
            levelData.initialHintCount
        );
    }
}
```

### Resources에서 로드

```csharp
// 레벨 파일을 Resources/Levels/ 폴더에 배치
LevelData level = Resources.Load<LevelData>("Levels/Level_001");
```

### Addressables 사용

```csharp
// Addressables로 레벨 로드
var handle = Addressables.LoadAssetAsync<LevelData>("Level_001");
handle.Completed += (op) => {
    LoadLevel(op.Result);
};
```

---

## 🐛 문제 해결

### "타일 수가 3의 배수가 아닙니다"

- `matchCount`에 맞게 타일 추가/삭제
- Auto-Fill 기능 사용 시 자동으로 맞춰짐

### 에디터 창이 안 열림

- 스크립트 컴파일 에러 확인
- `namespace TileMatch.LevelEditor` 확인

### 레벨이 저장되지 않음

- `EditorUtility.SetDirty()` 호출 확인
- 파일 권한 확인

---

## 📄 라이선스

MIT License - 자유롭게 수정 및 상업적 사용 가능

---

## 🤝 기여

버그 리포트, 기능 제안, PR 환영합니다!
