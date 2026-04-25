# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 협업 원칙
- 너와 나는 협업자니까 비위 맞추지 말고 틀린게 있으면 말해줘
- 간결하게 말하기
- 한글로만 말하기
- 처음보는 사람 입장에서 이해하기 쉽게 말하기
- Claude.md 파일은 절대 삭제하지 않기

## 작업 워크플로우
- WorkProgress/workprogress_summary.md 확인 및 앞으로 할 작업이 있다면 사용자에게 알려주기
- 새 기능 구현 시 /brainstorming 사용
- 이후 /writing-plans 사용해 계획을 세우기
- Worktree는 사용 안함
- 작업 시작 전, 계획을 다 세웠으면 새 원격 Branch를 만들어서 작업 시작
- 작업 시작 시 /subagent-driven-development 사용
- 코드 작성 전 /test-driven-development 사용
- 버그 발견 및 에러 발생 시 /systematic-debugging 실행
- 작업 완료 후 /verification-before-completion 실행
- Phase 완료 시 /requesting-code-review 실행
- 새로운 Phase일 경우 "Phase_숫자_yyyy-mm-dd" 파일명으로 md파일을 만들어서 WorkProgress 폴더에 저장
- 커밋/푸시 전에 사용자 확인
- Sequential Thinking MCP 사용

## 코딩 스탠다드
참조: https://tech.lonpeach.com/2017/12/24/CSharp-Coding-Standard/

### 명명 규칙
- PascalCase: 클래스, 메서드, 네임스페이스
- camelCase: 지역 변수, 파라미터
- private 멤버: `m` 접두사 (예: `mAge`)
- bool 변수: `b` 접두사 (예: `bFired`)
- Enum: `E` 접두사 (예: `EDirection`)
- 상수: ALL_CAPS_WITH_UNDERSCORES

### 포매팅
- 들여쓰기: 탭 (스페이스 금지)
- 중괄호: 항상 새 줄, 단일 라인도 반드시 중괄호
- `var` 키워드 금지

## 프로젝트 개요
Unity 모바일 타일 매칭 퍼즐 게임. Firebase Realtime DB로 서버 연동, Google Sheets로 게임 데이터 관리.

- 스크립트 경로: `Assets/_MainProject/Scripts/`
- ScriptableObject 에셋: `Assets/_MainProject/SODatas/`
- 빌드: `BuildTool.cs` (Editor 메뉴) 또는 Unity Editor 직접 사용

## 아키텍처 개요

### Core 매니저 (`GameMain/Core/`)
- **GameManager** — 게임 전체 흐름(레벨 로드, 상태 전환, 점수). 아이템 로직도 현재 여기 포함되어 있으나 리팩토링 예정
- **BoardManager** — 보드 타일 배치, 레이어, 블로킹 상태 관리
- **SlotManager** — 슬롯 타일 관리, 매칭 처리. `OnMatch` / `OnGameOver` / `OnLevelClear` 이벤트 발생
- **EffectManager** — 모든 시각 이펙트 중앙 관리 (매칭, 아이템, 승리/패배)
- **UIManager** — 인게임 HUD, 아이템 버튼, 패널 전환
- **AudioManager** — `AudioEvent.Play(EAudioKey.XXX)` 정적 호출로 사용
- **ComboSystem** — 콤보 데이터 및 등급 계산

### 데이터 레이어 (`GameMain/Data/`)
- **PlayerDataManager** — Firebase에서 받은 `UserData`를 래핑하는 단일 진실 공급원(Single Source of Truth). 서버 데이터 접근은 반드시 여기를 통함
- **UserData** — Firebase Realtime DB 스냅샷. `ObscuredInt`로 조작 방지
- **DataManager** — 레벨 데이터(LevelData) Addressables 로드 담당
- **ItemData** — `EItemType` enum, `ItemData` 클래스, `ItemTable` ScriptableObject

> ⚠️ `UserDataManager` (GameMain/UI/)는 Firebase 연동 전 임시 로컬 매니저. `PlayerDataManager`와 역할이 중복되며 리팩토링으로 제거 예정.

### 스프레드시트 연동 (`Scripts/Editor/`)
Google Sheets → ScriptableObject 자동 변환 파이프라인.

**새 시트 추가 시 4단계:**
1. `ESheetType`에 enum 추가 (`[SheetName("TB_XXX")]` 어트리뷰트 필수)
2. 데이터 클래스 생성 (`TBXxxData`) + ScriptableObject 테이블 (`TBXxxTable`)
3. `TBXxxParser : SheetParserBase` 파서 클래스 생성
4. `Tools > Spreadsheet Importer > Import All` 실행

현재 시트: `TB_Stage`, `TB_Item` (추가 예정)

### 이벤트 시스템 (`EventManager` / `EventKeys`)
씬 간 커플링 방지용 이벤트 버스. `RequestEventKeys`는 씬 전환 요청 전용.

### 프레임워크 (`FrameLibrary/`)
- `Singleton<T>` — 일반 싱글톤
- `Singleton_GameObject<T>` — MonoBehaviour 싱글톤 (DontDestroyOnLoad 포함)

## 주요 데이터 흐름
```
로그인
  → Firebase에서 Dictionary 수신
  → PlayerDataManager.Initialize(dictionary)
  → UserData 생성 (서버 스냅샷)

게임 시작
  → DataManager.LoadLevelAsync(level) — Addressables
  → BoardManager.LoadLevel(levelData)
  → SlotManager.Initialize()

매칭 발생
  → SlotManager.OnMatch 이벤트
  → GameManager.OnMatchHandler (콤보·점수)
  → ComboSystem으로 등급 계산

레벨 클리어
  → GameManager.LevelClear()
  → UserDataManager.ClearStage() (임시, 리팩토링 예정)
  → Firebase 서버 동기화
```
