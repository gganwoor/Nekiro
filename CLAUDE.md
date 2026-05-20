# NEKIRO — 프로젝트 인수인계 문서

## 프로젝트 개요

**게임 이름:** Nekiro  
**엔진:** Unity 2D (C#)  
**목표:** 학교 축제 동아리 부스 시연용 데모 버전 완성  
**장르:** 설계형 전투 게임

### 핵심 컨셉
반응 속도가 아닌 **플레이어의 전략 설계로 적을 격파**하는 전투 시스템.
적의 공격 궤적이 예고선으로 표시되면, 플레이어는 마우스로 대응 궤적을 직접 그린다.
플레이어가 그린 궤적과 적의 공격 궤적이 공간상에서 교차하면 패링 성공 판정.

---

## 세계관 및 아트 방향

- **분위기:** 다크 판타지 + 엘든링/세키로 느낌. 한때 위대했던 세계가 무너져가는 설정
- **적 구성:** 인간형 적(타락한 인간) + 괴물형 적(타락한 신의 잔재)
- **아트 스타일:** 2D 일러스트. 채도 낮은 다크 판타지 컬러 팔레트
- **플레이어 캐릭터:** 삿갓 + 찢어진 망토를 입은 날렵한 여행자 스타일. 올리브 그린 + 청회색 색감
- **배경:** 달빛이 비치는 어두운 대나무 숲 (밤, 신비로운 분위기)

---

## 씬 구성

### TitleScene (완성)
- 배경: 황혼녘 성을 바라보는 캐릭터 이미지
- 메뉴: Start, How to Play, Setting, Quit
- 버튼 효과: hover 시 아이콘(카타나)이 왼쪽으로 이동 + 텍스트 금색으로 변경, 클릭 시 아이콘 오른쪽으로 빠르게 이동 + 페이드 아웃 후 씬 전환
- 폰트: Cinzel (TMP)

### BattleScene (작업 중) = 튜토리얼 씬
- 배경: 대나무 숲 (달빛, 바닥 포함)
- 플레이어: 왼쪽 배치, 대기/준비 자세 애니메이션 보유
- 적: 오른쪽 배치 (현재 임시 스프라이트)
- UI: 플레이어 HP/스태미나바 (하단 좌측), 적 HP/스태미나바 (상단 우측)

---

## 현재 스크립트 목록 및 역할

### TrajectoryDrawer.cs (Player 오브젝트에 부착)
마우스 입력으로 궤적을 그리는 시스템.

**주요 변수:**
- `lineWidth`, `lineColor`, `minPointDistance`: 선 설정
- `points`: 플레이어가 그린 궤적의 점들 리스트
- `isDrawing`: 현재 그리는 중인지 여부
- `drawingEnabled`: 설계 페이즈에서만 그리기 허용

**주요 함수:**
- `SetDrawingEnabled(bool)`: 그리기 활성화/비활성화
- `ExecuteJudgement()`: 궤적 교차 판정 실행 후 결과 적용

**현재 Update() 로직:**
마우스를 뗄 때:
- 1단계(DrawTrajectory)면 TutorialManager.CompleteStep() 호출
- 2~4단계면 ExecuteJudgement() 호출

### EnemyAttack.cs (Enemy 오브젝트에 부착)
적의 공격 예고선을 표시하는 시스템.

**주요 변수:**
- `warningDuration`: 예고선 표시 시간
- `attackStart`, `attackEnd`: 공격 궤적 시작/끝 좌표
- `isWarningActive`: 예고선 활성 상태 여부

**주요 함수:**
- `StartAttack()`: 예고선 표시 시작 (BattleManager 또는 TutorialManager에서 호출)

### JudgementSystem.cs (GameManager에 부착)
두 선분의 교차 여부를 판정하는 시스템.

**주요 함수:**
- `CheckParry(playerPoints, enemyStart, enemyEnd)`: 플레이어 궤적과 적 공격선 교차 여부 반환

### PlayerStats.cs (Player 오브젝트에 부착)
플레이어 체력/스태미나 관리.

**주요 변수:**
- `maxHP`, `currentHP`, `maxStamina`, `currentStamina`
- `hpBar`, `staminaBar`: UI Image 연결

**주요 함수:**
- `TakeDamage(float)`: 체력 감소
- `UseStamina(float)`: 스태미나 감소
- `RecoverStamina(float)`: 스태미나 회복

### EnemyStats.cs (Enemy 오브젝트에 부착)
적 체력/스태미나 관리. PlayerStats와 동일한 구조.

### BattleManager.cs (GameManager에 부착)
전투 페이즈 흐름 자동 순환 관리.

**페이즈 순서:** Wait → Design → Execute → Result → 반복

**주요 변수:**
- `designPhaseTime`: 설계 페이즈 제한 시간
- `currentPhase`: 현재 페이즈

**주의:** TutorialManager 사용 시 `battleManager.enabled = false`로 꺼둠.
튜토리얼 4단계(실전)에서 다시 활성화.

### CameraShake.cs (Main Camera에 부착)
카메라 흔들림 효과. 싱글톤 패턴.

**사용법:** `CameraShake.instance.Shake(duration, magnitude)`

### HitStop.cs (GameManager에 부착)
히트 정지 효과 (Time.timeScale 조작). 싱글톤 패턴.

**사용법:** `HitStop.instance.Stop(duration)`
- `WaitForSecondsRealtime` 사용 (timeScale 0일 때도 작동)

### VignetteEffect.cs (GameManager에 부착)
패링 실패 시 화면 가장자리 붉어짐 효과. 싱글톤 패턴.

**연결 필요:** Canvas의 VignetteImage (전체 화면 덮는 빨간 Image)

**사용법:** `VignetteEffect.instance.Flash()`

### PlayerAnimator.cs (Player 오브젝트에 부착)
코드로 구현한 핑퐁 방식 대기 애니메이션.

**주요 변수:**
- `frames`: Inspector에서 frame_1~frame_8 연결
- `fps`: 초당 프레임 수 (기본 8)

### MenuButton.cs (TitleScene 버튼 오브젝트에 부착)
타이틀 메뉴 버튼 인터랙션.

**주요 변수:**
- `icon`: 카타나 아이콘 RectTransform
- `buttonText`: 버튼 텍스트 TMP
- `fadePanel`: 씬 전환 페이드 패널
- `targetScene`: 전환할 씬 이름 (종료 버튼은 빈 문자열)
- `leftX`, `centerX`, `rightX`: 아이콘 이동 X 좌표

### TutorialManager.cs (GameManager에 부착)
튜토리얼 흐름 관리. 싱글톤 패턴.

**튜토리얼 단계:**
1. `DrawTrajectory`: 궤적 그리기 연습 → 궤적 한 번 그으면 완료
2. `ParryPractice`: 패링 연습 → 패링 성공하면 완료
3. `StaminaPractice`: 스태미나 체험 → (완료 조건 미구현)
4. `RealBattle`: 실전 전투 → (완료 조건 미구현)
5. `Clear`: 튜토리얼 완료

**현재 상태:** 2단계에서 마우스를 떼면 ExecuteJudgement() 호출하도록 수정 중.

### LetterboxScaler.cs (GameManager에 부착)
16:9 해상도 레터박스 처리. DontDestroyOnLoad 적용되어 씬 전환 시에도 유지.

---

## Hierarchy 구조 (BattleScene)

```
Scene
├── GameManager (빈 오브젝트)
│   ├── BattleManager
│   ├── JudgementSystem
│   ├── HitStop
│   ├── VignetteEffect
│   ├── TutorialManager
│   └── LetterboxScaler
├── Main Camera
│   └── CameraShake
├── Background (배경 스프라이트)
├── Player (빈 오브젝트)
│   ├── PlayerVisual (Sprite Renderer)
│   ├── PlayerStats
│   ├── TrajectoryDrawer
│   └── PlayerAnimator
├── Enemy (빈 오브젝트)
│   ├── EnemyVisual (Sprite Renderer)
│   ├── EnemyStats
│   └── EnemyAttack
└── Canvas
    ├── PlayerHUD
    │   ├── HPBarBorder
    │   ├── HPBarBackground
    │   ├── HPBarFill
    │   ├── StaminaBarBorder
    │   ├── StaminaBarBackground
    │   └── StaminaBarFill
    ├── EnemyHUD
    │   ├── EnemyHPBarBorder
    │   ├── EnemyHPBarBackground
    │   ├── EnemyHPBarFill
    │   ├── EnemyStaminaBarBorder
    │   ├── EnemyStaminaBarBackground
    │   └── EnemyStaminaBarFill
    ├── VignetteImage (전체 화면, 빨간 Image, 초기 투명)
    ├── TutorialPanel
    │   ├── PanelBackground (반투명 검정)
    │   └── TutorialText (TMP, 한글 폰트)
    └── ActionPanel (미구현, 행동 선택 UI 예정)
```

---

## 에셋 현황

### 플레이어 캐릭터
- 대기 애니메이션: frame_1.png ~ frame_8.png (핑퐁 방식, PlayerAnimator로 재생)
- 준비 자세 (설계 페이즈 진입 시): 1프레임 완성, 전체 애니메이션 미완성
- 쳐내기 애니메이션: 미완성

### 적 캐릭터
- 에셋 없음 (임시 스프라이트 사용 중)

### 배경
- BattleScene: 대나무 숲 배경 (달빛, 바닥 포함) 완성
- TitleScene: 황혼녘 성 배경 완성

---

## 현재 미완성 / 다음 작업 목록

### 급한 것 (데모 완성에 필수)
1. **TutorialManager 3단계 완료 조건 구현** — 적 스태미나 소진 시 CompleteStep() 호출
2. **TutorialManager 4단계 완료 조건 구현** — 적 체력 소진 시 CompleteStep() 호출
3. **튜토리얼 클리어 후 타이틀로 복귀** — SceneManager.LoadScene("TitleScene")
4. **행동 선택 UI** — 쳐내기/가드/회피 버튼 (ActionPanel)
5. **적 캐릭터 에셋** — 임시 스프라이트 교체
6. **스태미나 소진 시 행동 제한** — 스태미나 0이면 쳐내기 불가

### 나중에 해도 되는 것
- 플레이어 쳐내기/피격 애니메이션
- 궤적 패턴 분류 → 애니메이션 매핑 시스템
- 사운드
- 프리딜 페이즈 구현
- 무기 시스템

---

## 입력 시스템

**New Input System 사용** (Player Settings에서 Both로 설정)
- 마우스 입력: `Input.GetMouseButton()` 계열 사용 중 (Both 설정으로 가능)
- 키보드 입력: `Keyboard.current` 사용

---

## 주의사항

- `PlayerStats`, `EnemyStats`의 `Update()`에서 매 프레임 `UpdateUI()` 호출 중 → 테스트용. 나중에 필요한 시점에만 호출하도록 최적화 필요
- `EnemyAttack`의 `warningDuration`과 `BattleManager`의 `designPhaseTime`을 같은 값으로 맞춰야 함 (현재 둘 다 3f)
- Canvas의 모든 UI는 Image 컴포넌트 사용 (Sprite Renderer 아님)
- TMP 폰트는 한글 지원 폰트 별도 임포트 필요 (Noto Sans KR 사용 중)
- `LetterboxScaler`는 DontDestroyOnLoad 적용되어 있으므로 TitleScene에만 배치하면 됨
