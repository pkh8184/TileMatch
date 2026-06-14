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

### 기본 프레임워크
- 코드를 작성할 때 단일 책임 원칙 형태로 구성 필요
