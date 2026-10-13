# Mediacube submission report - round 1

Game: Fruit Salad Epic Simulator Saga 2D
Submitted: 2026-09-07
Portal verdict: SDK checks FAILED (static HTML/JS scan of the extracted bundle, no headless browser)

## Passed

SDK checks section, 8 of the listed items green:

| Check | Result | What it means for us |
|---|---|---|
| SDK loaded first | PASSED | The ytgame script tag precedes the Unity loader in index.html. |
| SDK script tag has only src | PASSED | No inline code, no extra attributes on the SDK tag. |
| window.ytgame not overridden | PASSED | Nothing in our JS reassigns or shadows the SDK global. |
| No externally hosted scripts | PASSED | Every script is inside the ZIP. |
| No external resources or links | PASSED | No CDN, fonts, analytics endpoints or outbound anchors. |
| Relative paths only | PASSED | Loader and asset URLs are relative, so the bundle works wherever it is mounted. |
| No Page Visibility API | PASSED | We do not use document.visibilitychange; pause/resume comes from the SDK. |
| No orientation lock | PASSED | No screen.orientation.lock call; portrait is handled by layout, not the browser API. |

## Failed

| Check | Result | Cause |
|---|---|---|
| bundle_precompressed_assets | FAILED | Build shipped `.data.br`, `.framework.js.br`, `.wasm.br`. YouTube serves ZIP contents as-is with no Content-Encoding, so the browser gets octet-stream and the game never leaves the loading screen. |

## Warnings

| Check | Result | Cause | Handled |
|---|---|---|---|
| unity_device_pixel_ratio_unclamped | WARNING | `config.devicePixelRatio` absent from index.html, render target scales with DPR. | Yes |
| unity_sdk_callback_race | WARNING | jslib calls `unityGameInstance.SendMessage` unguarded; the global is set after `createUnityInstance()` resolves. `loadData()` had no rejection handler. | Yes |
| individual_file_size_recommended | WARNING | `.data` and `.wasm` are each well over 512 KiB. | No - inherent to a single Unity payload; would need Addressables splitting. Left for a later round. |

## Actions taken (2026-09-07)

1. `ProjectSettings.asset`: `webGLCompressionFormat` 0 (Brotli) -> 2 (Disabled). Fixes the red check. Hub spec `CLAUDE.md` updated to match, since it prescribed Brotli.
2. `Assets/WebGLTemplates/Portrait/index.html`: added `devicePixelRatio: 1` to the loader config. Trade-off: softer image on 2x/3x phones. If real devices look too blurry and hold 60 fps, a clamp of 2 is the fallback.
3. `Assets/Plugins/UnityYTGameSDKLib.jslib`: new `$ytgameSend` helper retries every 50 ms until `unityGameInstance` exists (gives up with a console warning after 30 s); all six SendMessage sites route through it. `loadData()` now has a rejection handler that logs to `health.logError()` and answers the game with an empty save so it starts fresh instead of hanging.

## Follow-ups before round 2

- Rebuild and re-measure. The uncompressed `.wasm` and `.data` will be several times the Brotli sizes; the 30 MB budget applies to the files as zipped, so check both raw and ZIP size.
- Rezip from a clean `Builds/WebGL` so no stale `.br` files ride along.
- Save regression risk: on a transient `loadData()` failure the game now starts fresh. Confirm the save layer refuses to overwrite a newer cloud save with the fresh one before the first real save fires.
- Smoke-test the rebuilt bundle locally with the SDK stubbed and, if possible, in the portal preview.

