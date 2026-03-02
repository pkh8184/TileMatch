# C# 코딩 스탠다드 (lonpeach)
출처: https://tech.lonpeach.com/2017/12/24/CSharp-Coding-Standard/

## 명명 규칙
- **PascalCase**: 클래스, 구조체, 메서드, 네임스페이스
- **camelCase**: 지역 변수, 함수 파라미터
- **private 멤버**: `m` 접두사 (예: `mAge`)
- **bool 변수**: `b` 접두사 (예: `bFired`)
- **인터페이스**: `I` 접두사 (예: `IInterface`)
- **Enum**: `E` 접두사 (예: `EDirection`)
- **Flag Enum**: 접미사 `Flags`
- **상수**: ALL_CAPS_WITH_UNDERSCORES
- **Nullable 반환/파라미터**: `OrNull` 접미사

## 포매팅
- **들여쓰기**: 탭 (4스페이스 상당) — 스페이스 금지
- **중괄호**: 항상 새 줄에 시작
- **중괄호 필수**: 단일 라인 스코프도 반드시 중괄호 사용
- 지역 변수는 첫 사용 위치 가까이에 선언

## 메서드
- 이름: 동사+명사 패턴 (예: `GetAge()`)
- 비공개 메서드: `Internal` 접미사
- 재귀 메서드: `Recursive` 접미사
- 오버로딩 지양, 설명적인 이름 사용

## 제어 흐름
- switch: 반드시 default 케이스 포함
- 폴스루 시 명시적 주석

## 데이터/컬렉션
- `var` 키워드 금지 (enumerator 예외)
- 제네릭 컨테이너 사용 (System.Collections.Generic)
- 오브젝트 이니셜라이저 지양, 명시적 생성자 사용

## 오류 처리
- 외부 데이터만 시스템 경계에서 검증
- 내부적으로 예외 throw 금지
- public 함수에 null 파라미터 금지
