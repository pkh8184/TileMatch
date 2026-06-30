
---

## Controller 직접 검증 (실제 배치 모드 테스트 실행 증거)

이전 두 서브에이전트 모두 `-batchmode -runTests ... -quit` 조합을 사용했는데, `-quit`이 `-runTests`보다 먼저 종료를 트리거하는 레이스 컨디션이 있어 실제로는 테스트가 한 번도 실행되지 않았다(컴파일만 되고 "Exiting batchmode successfully now!"로 바로 종료, 결과 XML 파일 자체가 생성되지 않음). 컨트롤러가 `-quit`을 제거하고 직접 재실행하여 검증함:

```
/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity -batchmode -nographics -runTests -projectPath /Users/yegom/Documents/mywork/unity/TileMatch/TrumpTile -testPlatform EditMode -testResults /tmp/editmode-verify2.xml -logFile /tmp/editmode-verify2.log
```

결과 (exit code 0):
```
<test-run id="2" testcasecount="30" result="Passed" total="30" passed="30" failed="0" inconclusive="0" skipped="0" .../>
```

`AdManagerRewardedCloseOrderTests`의 4개 테스트 모두 `result="Passed"`:
- InvokeRevivedThenReloadAd_CallsOnClosedBeforeReloadAd: Passed
- InvokeRevivedThenReloadAd_PassesRewardEarnedValueToOnClosed: Passed
- InvokeRevivedThenReloadAd_StillCallsReloadAd_WhenOnClosedThrows: Passed
- InvokeRevivedThenReloadAd_DoesNotThrow_WhenOnClosedIsNull: Passed

`grep -i "error CS" /tmp/editmode-verify2.log` → 0건. 프로젝트 전체 EditMode 테스트(기존 26개 + 신규 4개 = 30개) 전부 통과.
