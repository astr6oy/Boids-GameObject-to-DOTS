# Boids GameObject to DOTS Porting Study

Unity GameObject 기반 Boids 시뮬레이션을 DOTS/ECS로 포팅하여 성능 비교 및 전환 방법론 연구

![go_game_view](Docs/go_game_view.gif)
<- 게임오브젝트 기반 ->

![dots_game_view](Docs/dots_game_view.gif)
<- DOTS 기반 ->

## Performance Comparison

| Boid Count | GameObject | DOTS/ECS |
|------------|-----------|----------|
| 500        | 43 FPS    | 390 FPS  |
| 1,000      | 19 FPS    | 340 FPS  |
| 5,000      | <0.5 FPS  | 180 FPS  |

## Features

- **동일 알고리즘 보장**: BoidFlockingMath 공유로 GameObject/DOTS 동일 동작
- **Spatial Hashing**: O(n) 이웃 검색 최적화
- **Burst Compilation**: SIMD 자동 벡터화
- **Job System**: 멀티스레드 병렬 처리

## Environment

- Unity 6.2
- Entities 1.4.3
- URP (Universal Render Pipeline)
- IL2CPP Backend

## Migration Notes

### Unity 6.2 API Changes

**LightProbes API 변경사항**
- `coefficients` (float[]) → `bakedProbes` (SphericalHarmonicsL2[])
- 변환 레이어 구현: `ProbePolisher.GetCoefficientsArray()` / `SetCoefficientsArray()`
- 영향 파일: ProbePolisher.cs, PolishableProbeMixer.cs, PolishableProbeEditor.cs

### Rendering Pipeline

**URP 전환 필수**
- Entities Graphics는 SRP(URP/HDRP) 전용
- BatchRendererGroup API 사용

**Scene View 제한사항**
- Wireframe 모드: Entities 렌더링 미지원
- Shaded 모드에서만 정상 표시

### DOTS Setup

**필수 패키지**
- com.unity.entities
- com.unity.burst
- com.unity.collections
- com.unity.mathematics
- com.unity.entities.graphics

**프로젝트 설정**
- Scripting Backend: IL2CPP
- API Level: .NET Standard 2.1
- Allow unsafe Code: Enable

## Original Project

- Author: Keijiro Takahashi
- License: MIT
- Source: https://github.com/keijiro/Boids
