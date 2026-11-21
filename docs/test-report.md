# Test report — 0.2.2-alpha (2026-11-21, Asia/Shanghai)

| Check | Result | Evidence |
|---|---|---|
| Layout defect reproduction | Confirmed | User screenshot: clipped second explanatory line and scene showing through panel |
| Layout-only fix build | PASS | net35/x86,0 warnings/errors; evidence/build-0.2.2.txt |
| Core/input/config behavior | Unchanged | Existing40 core /4 config tests and0.2.1 user functional checks preserved; test-report-0.2.1.md |
| Updated visual acceptance | PASS by user observation | User reloaded0.2.2 and confirmed all text complete / obstruction resolved; runtime layout metrics in evidence/runtime-menu-layout-0.2.2.txt |

Only UI height/spacing/background drawing changed. Text height is measured from actual GUIStyle/font at runtime; smaller resolutions scroll the event list instead of overlapping labels. No extra gameplay effects, pause policy, input ownership or settings behavior changed. The user-provided screenshot supplied actual visual evidence after assistant read-only capture was unavailable.
