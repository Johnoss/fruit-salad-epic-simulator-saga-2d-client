# Fruit Salad Epic Simulator Saga 2D - YouTube Playables port

Upstream: `git@github.com:Johnoss/fruit-salad-epic-simulator-saga-2d-client`
Origin: weekend game-jam job-application build. Match-chain 2D tile game, MVC + Zenject + UniRx + DOTween.

**Status: gameplay feature complete on Unity 6. Not submittable.** The platform abstraction is an
ads-only slice; saves, lifecycle, audio state, the finite-content signal and a measured build are all
outstanding. See Compliance status.

---

## Port audit (as cloned, 2026-08-22)

### Already in our favour

- **Portrait is already the intent.** `defaultScreenOrientation: 4` (auto-rotate) with only `allowedAutorotateToPortrait: 1` set. Effectively portrait-locked.
- ~~**Brotli already selected.** `webGLCompressionFormat: 0`.~~ Reversed 2026-09-07: YouTube serves
  the ZIP byte-for-byte with no Content-Encoding, so a pre-compressed payload arrives as
  octet-stream and never executes. Compression is now Disabled (`webGLCompressionFormat: 2`).
- **Exception support already correct.** `webGLExceptionSupport: 1` = Explicitly Thrown Exceptions Only, which is exactly what the spec asks for.
- **2D-only game.** No 3D physics, nav, or terrain in actual use, so the module strip can be aggressive and safe.
- **Small source footprint.** 27 MB of `Assets/`, and 6.7 MB of that is `Plugins/`, mostly editor-only DLLs and docs that never ship.

### Fixed

- **Dangling `_boardConfig` reference in `Assets/Features/ConfigInstaller.asset`** (2026-08-22). Pointed at guid `989c8fdd7732854469d90cccf7d30525`, but `BoardConfig.asset.meta` actually carries `73db026751c14d24dac69d8c1b63858e`. The .meta guid was regenerated at some point and the installer was never repointed.

  Symptom was a misleading `ZenjectException: Received null prefab during bind command` at `NodeInstaller.cs:21`, because Unity resolves the bad guid to a missing object, `BindInstance` binds it regardless, and `NodeInstaller` then reads `.NodePrefab` off that missing object and gets null - so the failure surfaces one layer away from its cause, and as an assert rather than a NullReferenceException.

  **This predates the Unity 6 port.** `ConfigInstaller.asset` was unmodified since clone, so the break exists at upstream HEAD. Fixed by repointing the guid. A full sweep of every YAML reference in `Assets/` against the guid index found no other dangling references.

### Blocking gaps

1. **No save system worth the name.** The only persistence is `Assets/Scripts/Score/ScoreModel.cs:26` and `:31` - a single `PlayerPrefs.SetFloat("HighScore", ...)`. Cloud saves are a certification requirement, and the spec demands a version-stamped, backward-compatible, never-regress-on-older-data format. This is a from-scratch build, not a patch.

2. **No finite-content signal, because there is no progression.** `Assets/Scripts/Board/LevelConfig.cs` holds one board config plus one tile config. `Assets/Scripts/Score/GoalConfig.cs` holds a single `_tilesCollectedGoal`. There is no level list, no end state, nothing that can say "you have reached the end of the content". Certification requires that signal explicitly. This is a design change, not just code.

3. **No SDK integration of any kind.** No `firstFrameReady`, `gameReady`, pause/resume, audio-state handling, ads, or `IPlatformServices` abstraction.

4. **Zenject + UniRx against IL2CPP High stripping is the main technical risk.** Both lean on reflection and generic instantiation the stripper cannot see. Expect missing-ctor and `ExecutionEngineException` failures at runtime in the WebGL build, not at compile time. Budget real time for a `link.xml`, and test the *stripped release build*, never just the editor.

5. **Audio is set to worst case for WebGL.** `main_theme.wav.meta` is Vorbis (good) but `quality: 1` (100%, maximum) and `loadType: 0` (Decompress On Load), which expands the whole 3 MB track in memory at load. Should be roughly 0.5-0.7 quality, Streaming load type, and mono unless the stereo image genuinely earns its place. The new `big_bowl.mp3` and `starfruit.wav` import with the same quality 1 / Decompress On Load defaults and want the same treatment.

6. **Dead engine modules still in the manifest.** `Packages/manifest.json` carries ai, ai.navigation, cloth, terrain, terrainphysics, vehicles, vr, xr, umbra, wind, video, director, androidjni, physics (3D), screencapture. Also `com.unity.modules.unityanalytics`, which the hub spec bans outright. All of it should go.

7. **Web canvas is landscape.** `defaultScreenWidthWeb: 960`, `defaultScreenHeightWeb: 600`. Needs a portrait ratio.

8. **`runInBackground: 0`.** Interacts with `onPause` / `onResume`. Decide the intended behaviour rather than leaving it at the default.

### Unity version (resolved)

Cloned at `2022.3.12f1`, which was installed on neither machine. **Migrated to `6000.0.60f1`.**
Unity 6 is the better WebGL target: smaller builds and better mobile-webview behaviour.

The migration itself was clean. The `ZenjectException` that surfaced straight after it was a
pre-existing asset break, not migration fallout - see Fixed above. Zenject and UniRx both still
compile with only obsolete-API warnings (`FindObjectsOfType`, `PrefabUtility.GetPrefabType`),
none of which are errors. The IL2CPP stripping risk in gap 4 is unchanged and still unproven.

---

## Boosters (rewarded-ad monetization)

Two player-initiated rewarded placements. Rewarded rather than interstitial because both are
player-initiated, which is the best eCPM case and the one the platform guidance favours.

**Bigger Bowl** adds `BoosterConfig.BowlIncrease` (default 50) to the bowl capacity, and scales the
bowl up to `GoalView._enlargedBowlScale` so the purchase is visible rather than just numeric. It is
one per run: `BoosterController.GetCanRequest` refuses a second one and `BoosterView` drives button
interactability from that same call, so the rule lives in one place and the button cannot disagree
with what the controller will actually do. The guard is in the request path too, not only the button,
because the failure it prevents is spending a player's ad view on a bowl that is already full size.
It re-enables on restart for free, since `IsBowlEnlarged` is derived from the goal.

The scale is driven off `GoalModel.IsBowlEnlarged`, derived from the goal rather than tracked
separately, so restarting resets the bowl for free - `RestartGoal` already resets the goal, and the
view follows. `PunchTweener` is the trap here, twice over. It caches the scale it punches from and reapplies that
cache at the *start* of every `Play()`:

- The punch used to fire from `UpdateProgress`, which is subscribed to both `CollectedTiles` and
  `TilesGoal` - so buying the bowl punched it, and the punch reset `localScale` in the same frame the
  grow tween was starting, stomping the tween's captured start value. The bowl appeared not to grow
  at all. The punch is feedback for *collecting*, so it now hangs off `CollectedTiles` alone, and is
  skipped entirely while the grow tween is running.
- The cache also has to be rebased (`PunchTweener.SetDefaultScale`) when the bowl resizes, or the
  next collect would snap the bowl back to its old size, silently undoing what the player paid for.
Bowl capacity moved out of `GoalConfig` and into `GoalModel.TilesGoal` as reactive state, and
`GoalReached` is now `CombineLatest(collected, goal)` rather than derived from collected alone.
A side effect worth knowing: because `GoalReached` drives the game-over panel, enlarging the bowl
while the panel is up flips it back to false and resumes the run. The same button therefore works
as a mid-run boost and as a "continue" offer on the game-over screen, with no extra code.

**Starfruit** replaces `BoosterConfig.StarfruitPerReward` board tiles (default 5) with wildcards
that match anything. Each is placed before the next is picked, so later placements see the earlier
ones and spread out from them.

- Wildcards are flagged per `TileSetting` (`IsWildcard`), not by type comparison, so the matching
  rules stay dependency-free.
- `TileConfig.SpawnableTileSettings` excludes wildcards from the refill pool. A wildcard falling in
  for free would undercut the thing the player just watched an ad for.
- **A Starfruit is a bridge, not just a filler.** A wildcard at the head of the chain lets the next
  tile be any fruit, so a chain can change type partway through: apple into orange in one connect.
  Only the tile immediately after the wildcard is free - once a real fruit lands, the chain locks to
  it again, so a direct apple into orange is still refused.
- The chain's type is therefore the *most recent* fruit in it, not the first. That is what drives
  the line colour, so the line follows the latest fruit through each switch. A wildcard never sets
  the type, which is what keeps the chain open across it.
- Backing a chain out re-derives the type from what is still selected
  (`SelectionController.RefreshSelectedType`), rather than leaving whatever the last added tile set.
  Retreating past a switch turns an orange chain back into an apple chain; retreating to nothing but
  Starfruits leaves no fruit to define it and reopens the chain to any type. Without this, opening on
  a Starfruit, adding an apple, then backing off to the lone Starfruit would leave the chain
  apple-only with no apple in it, and the wildcard would have quietly stopped being wild.
- Retreating onto an already-selected tile is checked *before* the match test, not after. Now that a
  chain can change type, an already-selected tile can legitimately fail that test - in
  `[apple, starfruit, orange]` the apple no longer matches the chain's current type - and backing
  onto it must never be blocked by that.
- `BoardController.CheckTypeMatches` is wildcard-aware too. It is not just about matching: that
  method feeds `ValidateMovesExist`, which calls `RepopulateBoard` and wipes every tile. Without
  wildcard handling, a board holding a paid Starfruit could read as "no moves left" and destroy it.
- Placement is Gaussian around the board centre (`BoosterConfig.StarfruitSpread`), by rejection
  sampling rather than clamping. Clamping would fold the whole outer tail onto the border and make
  edge placements *more* likely than the ones just inside them, inverting the intended bias.
  Centre bias matters because a centre tile has eight neighbours to an edge tile's three, so a
  wildcard is only worth an ad view if the player can actually build a chain through it.
- Tiles that are mid-chain or already wildcards are excluded as targets.
- The batch is staggered: each Starfruit lands after its own random delay up to
  `BoosterConfig.StarfruitSpawnDelayMax` (0.5s), so five arrive as a scatter rather than a single
  pop. The sound plays once for the reward, not once per tile.
- Each Starfruit flies from its icon to the square it will occupy, and the tile is only replaced
  when the flight lands (`StarfruitFlightView` -> `StarfruitPlacer.CommitStarfruit`). Input is never
  blocked for it. Two consequences fall out of deferring the placement:
  - **Squares are reserved on launch.** Placement used to be instant, so two Starfruits could not
    collide. Now that a square is claimed a quarter-second before it is filled, `_reservedCoordinates`
    keeps the rest of the batch off it.
  - **The landing takes the tile over**, even if the player chained it during the flight, and drops
    it from the selection first (`SelectionController.DropSelectionAt`). Leaving it selected would
    leave the chain holding a disposed tile, and releasing the chain would then remove whatever now
    sits on that node - the player's own chain eating the Starfruit that just landed in it.
  - With no flight view wired, `Subject.HasObservers` is false and the Starfruit lands immediately.
    The reward can never depend on the effect being connected. Each one picks its target *at spawn time*, not when
  the batch was scheduled, which is what lets it avoid both the player's live chain and the siblings
  that landed ahead of it. Because placement is now asynchronous, `TryPlaceStarfruits` reports that
  the batch *started*; only a board with nowhere to put a single Starfruit counts as a denial.
- Starfruits are kept apart, but as a preference rather than a ban: pass one samples only spots with
  no wildcard in the surrounding eight cells, pass two drops that requirement, and a deterministic
  scan ranking isolation over proximity catches the rest. Clustering wastes the booster, since a
  wildcard beside a wildcard adds far less reach than one in fresh territory, but on a crowded board
  placing adjacent still beats refusing to place.

Measured over 20k simulated grants on the 7x7 board at the default spread of 1.2:

| | centre cell | ring 1 | ring 2 | ring 3 (edge) |
|---|---|---|---|---|
| share per cell | 4.16% | 3.99% | 2.83% | 0.78% |

The avoidance pass takes the mean number of starfruits with a starfruit neighbour from **3.89 to
0.178**, with 92.9% of grants placing all five with no adjacency at all, and never fails to place.

Ads go through `IPlatformServices` (`Assets/Scripts/Platform/`), the first slice of the interface
the hub spec requires. Game code never sees an SDK type. `EditorStubPlatform` simulates latency and
has a `Simulate No Fill` toggle on the installer, because "no fill" is the path certification cares
about and is otherwise hard to reproduce on demand. Nothing is consumed on a denied booster, so the
run continues untouched and the player can retry.

Board input is suppressed during an ad via `InputModel.IsBlocked` rather than `timeScale`, which is
also where the SDK's `onPause` should hook in later.

**Booster audio.** `SoundView` subscribes to `BoosterModel.OnBoosterGranted` the same way it already
subscribes to the goal and selection models, and plays one clip per reward. An earlier version fired
the Starfruit clip per placed tile, which stacked five overlapping copies across the spawn stagger
and read as noise rather than feedback. Pitch variation comes from the existing
`ClipSetting.RandomPitchOffset`, set to 0.05 to match the plops.

`BoosterModel` is injected with `[InjectOptional]`. A hard dependency would fail injection on
`SoundView` and take the entire `SceneContext` down whenever `MonetizationInstaller` is absent, which
is not a failure mode worth having for a sound effect.

### Still to wire in the editor

- [x] Create a `BoosterConfig` asset (Assets > Create > Create BoosterConfig).
- [x] `SoundConfig` entries for `BiggerBowl` (big_bowl.mp3) and `Starfruit` (starfruit.wav), pitch
      offset 0.05, delay 0. **Leave `RandomDelaySeconds` at 0 on the Starfruit clip**: the spawn
      stagger already provides the delay, and a non-zero value routes the clip through
      `_oneShotDisposer`, which `PlayCollectClip` clears - so collecting a chain mid-batch would
      silently cancel pending Starfruit sounds.
- [x] Add `MonetizationInstaller` to the scene, assign the `BoosterConfig`, and add it to the
      SceneContext installers list.
- [x] Add the two buttons plus a `BoosterView` component, and assign them
      (`btn_reward_big_bowl`, `btn_reward_starfruit` on `view_rewards`).
- [ ] Confirm the Starfruit tile setting imported correctly (TileConfig entry 7, bottom-left sprite,
      IsWildcard ticked). It was written directly to `TileConfig.asset`.

## Intro screen

The intro is a chain of `DOTweenAnimation` components under `canvas_splash` that hand off through
`onComplete`, and it ends by calling `SetActive(false)` on its own canvas. Skipping is that same end
state reached early, so `IntroView.Skip()` does not invent a teardown, it just performs the one the
intro already performs.

Two things make it more than a `SetActive` call:

- **The tween chain has to be killed first.** One link re-triggers `SoundView.PlaySplash` from an
  `onPlay` callback. Left running after a skip it would restart the sting that was just faded out,
  over a canvas the player already dismissed. `IntroView` kills every `DOTweenAnimation` under the
  root, inactive ones included, since later links have not been enabled yet.
- **The fade is owned by `SoundView`, not `IntroView`.** The intro canvas is deactivated on skip, so
  a tween owned by anything under it would be running on a dismissed object.

The sting is a one-shot on the shared effects source, and a one-shot cannot be stopped or faded on
its own. `SoundView` therefore takes an optional dedicated `_splashAudioSource`: assigned, the sting
plays as a normal clip on its own channel and fades cleanly; unassigned, it falls back to the old
shared one-shot and the fade briefly ducks that source instead. Either way the volume is restored
after the fade, or every later sound on that source would stay silent.

Board input cannot leak through the intro: selection runs on `GraphicRaycaster` pointer events, so a
full-screen raycast target blocks it while the intro is up.

**Certification note:** this is where `gameReady()` should eventually fire. The call must happen only
at true interactability, never while a splash is still on screen, so both the skip path and the
natural end of the intro need to converge on it.

### Still to wire in the editor

- [x] Skip Button on `canvas_splash`. It sits on the splash's own full-screen `img_background`,
      which has `Raycast Target` on.
- [x] `IntroView` assigned: intro root `canvas_splash`, the skip button, and `SoundView`.
- [x] Dedicated `AudioSource` for the sting, cloned from effects channel 1 so it keeps the same
      mixer group, `Play On Awake` off, assigned to `SoundView._splashAudioSource`.

## Game over pacing

The panel used to appear on the frame the player lifted their finger off the winning chain.
`GoalConfig.GameOverDelaySeconds` (1.2s) now sits between the run ending and the panel arriving, and
board input is blocked for the whole pause.

The split is `GoalReached` (the run is over) versus `GoalModel.IsGameOverVisible` (the panel should
be on screen). `GoalController` owns the gap: it cuts input the instant the goal is reached, then
shows the panel after the delay. Blocking at the *start* rather than when the panel appears is the
point - otherwise the pause is a free window to keep matching and bank score into a finished run.

The delay is held as a disposable and cancelled if `GoalReached` goes false during it, so enlarging
the bowl mid-pause resumes the run instead of letting the panel drop in over it.

The game over sting follows `IsGameOverVisible` too, so it lands with the panel rather than a second
ahead of it, and a cancelled panel takes the sound with it. Anything else that should read as "the
run is over" belongs on that property rather than on `GoalReached`.

**Input blocking became reason-based** to make this safe. `InputModel.SetIsBlocked` now takes an
`InputBlockReason` and keeps a set. With the previous single bool, buying a Starfruit on the game
over screen would have ended with the ad's unblock clearing the game over block, handing the board
back to the player mid-panel.

## Collected fruit flight

Every collected tile flies into the bowl (`CollectedFruitFlightView` on `view_goal`), carrying its
own fruit's sprite. Independent of the fruit goal's flight, so a collected goal fruit sends two
sprites: one to the bowl for the run, one to the bar icon for the goal.

Purely presentational. The bowl count still commits on collection, unlike the goal bar which waits
for its arrival - moving the bowl to arrival-commit would push `GoalReached` later and shift the
game over pacing, which is tuned separately. Worth revisiting only if the two bars reading
differently becomes noticeable.

## Fruit goal

A rolling side objective: collect `FruitGoalConfig.RequiredCount` (default 15) of one named fruit,
shown as a slider with the fruit's icon beside it. Completing it spawns one Starfruit and rolls to
the next fruit.

**The bar commits on arrival, not on collection.** Collecting goal fruit sends one flying sprite per
tile towards the icon, and only landing increments the count. Flight duration is derived from
distance over `FruitGoalView._flyingSpeed` (screen pixels per second), so fruit from the far corner
takes longer than fruit beside the icon rather than every sprite taking the same time.

**Gotcha: `canvas_board` and `canvas_ui` are Screen Space *Camera*, not Overlay.** That makes
`RectTransform.position` world units - the whole board is about 2.5 units across, not 2500 pixels.
Dividing that by a speed in pixels per second gave a duration of 0.0015s, so every sprite finished
the frame it started, released itself back to the pool and appeared as a pile of disabled clones
under the icon. `FlyingSpritePool.GetScreenDistance` converts through
`RectTransformUtility.WorldToScreenPoint` first, so a speed in px/sec means the same thing whatever
the render mode is. The Starfruit flight never hit this because its duration is fixed.

That split is why the controller tracks `_fruitsInFlight`. The count is only committed on arrival,
so without it a chain of twenty apples against a goal of fifteen would land five surplus arrivals
*after* the goal rolled over, quietly crediting them to whatever fruit came next. Spawns are clamped
to what is still outstanding, in-flight included.

**The opening goal is the board's most populated fruit**, not a shuffled draw, so the first goal is
always one the player can immediately make progress on. Ties are broken randomly rather than by enum
order, which on a fresh 7x7 board would otherwise open on the same fruit far too often. Applies to
restarts too, since the board repopulates before the fruit goal resets.

Picked when the board reports populated rather than in the constructor: `FruitGoalController` can be
built before `BoardController` has laid any tiles, and the subscription covers both orders. It goes
*through* the bag (`FruitBag.Take`) rather than around it, so the opener is consumed from the current
bag and recorded as the last draw - otherwise it could come round again immediately or repeat across
the refill seam.

**Everything after the opener is a shuffled bag, not a random pick.** Every fruit comes up once before any repeats.
Within a bag repeats are impossible; the only seam is bag-to-bag, so on refill the head is swapped
away if it matches the last draw - always valid, since every entry in a bag is distinct. Verified
over 60k draws: zero consecutive repeats, every bag a complete permutation, uniform 16.67% each.
Re-verified with the forced opener in place: 20k boards all opened on a genuinely most-populated
fruit, zero consecutive repeats across 4k runs of 14 goals, and the opener never reappeared inside
its own bag.

Other decisions worth knowing:

- Wildcards do not count towards the goal. A Starfruit matches anything but is not the fruit, and
  with the bridge rule a single chain can contain several fruit types, so counting by actual type is
  the only reading that stays predictable.
- The Starfruit reward goes through the same `StarfruitPlacer` as the rewarded one, so it inherits
  the centre bias, the spread-apart rule and the mid-chain exclusion.
- Positions stay in the view. The model publishes board *coordinates*; `FruitGoalView` converts them
  through `BoardModel`. Nodes outlive the tiles on them, so the position is still valid even though
  the collected tile is being removed in the same frame.
- With no flying prefab assigned the view registers arrivals immediately. The bar is progression,
  not decoration, so it has to keep working even when unwired.

### Still to wire in the editor

- [x] `FruitGoalConfig` asset created.
- [x] `FruitGoalInstaller` on the SceneContext, config assigned, in the installers list.
- [x] Bar UI built: `FruitGoalView` on `view_bar` with slider and icon assigned.
- [x] `prefab_flying_fruit` made and assigned, `Image` on the root.
- [x] `StarfruitFlightView` added and assigned.

## Sound toggle

One button muting effects and music together, swapping its sprite with the state.

`SoundController` sets `AudioListener.volume`, which is what makes it a single switch: every source
in the scene feeds the one listener, so the music, both one-shot channels and the intro sting are all
covered without the controller knowing any of them exist. Volume rather than `AudioListener.pause`,
so the music keeps running and comes back in place instead of freezing mid-bar.

**The state is a combination, not a mute flag.** `SoundModel.IsAudioEnabled` is
`IsPlatformAudioEnabled && !IsMutedByPlayer`. Certification requires the platform's audio state to be
the source of truth, so the in-game toggle may only ever subtract from it: if YouTube says audio is
off, no button press can turn it back on. `SetIsPlatformAudioEnabled` is the seam
`ytgame.system.onAudioEnabledChange` reports into. Nothing drives it yet, so it stays true and the
toggle is currently the only input - but the shape is already correct, and wiring the SDK to it is
one subscription rather than a rework.

The icon follows the combined state rather than the player's toggle alone, so it always shows whether
there is actually any sound, including when the platform muted us.

**Not persisted.** The mute resets each session. It belongs in the save once the save exists, rather
than adding another `PlayerPrefs` key to the one already being torn out (see gap 1).

### Still to wire in the editor

- [x] `SoundInstaller` on the SceneContext GameObject and appended to its installers list.
- [x] `SoundToggleView` on `btn_music`, with the button, the contained `img_music` Image, and
      `Icon_Music_On` / `Icon_Music_Off` from Sweet Land GUI. The icon is the child Image, not the
      button's own graphic - `btn_music` keeps its own background and Color Tint transition, so the
      two never fight.

## Compliance status (verified 2026-08-23)

Gameplay is feature complete. Everything below was checked against the project, not against notes.

### Certification blockers - will fail submission

- [x] **Cloud saves implemented** - versioned, migration-ready, never overwrites newer data, saves
      on change and on pause. `sendScore` derives from the same field, so the two cannot disagree.
- [x] **`firstFrameReady()` / `gameReady()` wired** via `GameLifecycleController`. Both the intro's
      natural end and the skip converge through `IntroView.OnDisable`.
- [x] **`onPause` / `onResume` handled** - timeScale, `AudioListener.pause` and
      `InputBlockReason.PlatformPause`.
- [x] **Audio state driven by the platform** - seeded from `IsAudioEnabled`, then following
      `AudioEnabledChanged`.
- [x] **Finite-content signal is real** (2026-09-12). Superseded the unreachable-score formality
      entirely: `ScoreConfig.FinalContentScore` and `ScoreModel.IsContentComplete` are deleted.
      The game now has 100 authored levels (`Assets/Scripts/Level/LevelSet.cs`), and finishing the
      last one sets `SaveData.HasCompletedAllLevels` and raises `LevelModel.IsContentComplete`.
      That flag is sticky and saved, so the claim survives a replay and a restart rather than
      flashing past once.
      **Still to wire in the scene:** `GoalView._contentCompleteMessage` must point at a visible
      object saying something unambiguous - "You have finished all 100 levels". The code path is
      done; with the field empty nothing is communicated, which is the whole point of the rule.
      After level 100 the preview wraps to level 1 so there is still something to play, and the
      message stays up from then on to mark it as a replay.
- [x] **Measured uncompressed, which is the figure that counts: 20.92 MB wasm of 30 MB**
      (2026-09-12). The limit is per extracted file and the wasm is the binding one, at **70% of
      budget**. Data is 10.26 MB, framework and loader 0.47 MB, 30.18 MB across all files.
      The old 10.44 MB / 6.43 MB wasm figure was **Brotli** and is not comparable - compression is
      Disabled now, so nothing shrinks in transit as far as the limit is concerned.
      Settings verified at the same time: stripping High (`managedStrippingLevel WebGL: 3`),
      `il2cppCodeGeneration WebGL: 1` (OptimizeSize), exceptions Explicitly Thrown Only,
      `stripEngineCode: 1`, `webGLDebugSymbols: 0`, non-development. The size is what the project
      honestly costs, not a misconfiguration.
      **Headroom if it is ever needed:** `additionalIl2CppArgs` is empty, so LTO is off. Turning it
      on is the single biggest remaining lever on the wasm, at the cost of much slower builds.
- [x] **Web canvas is portrait**: 1080x1920, with a custom 9:16 `Portrait` WebGL template that
      letterboxes on landscape desktop windows and fills a phone viewport.

### Build configuration

- [x] Compression **Disabled** (`webGLCompressionFormat: 2`, was Brotli until the round-1 Mediacube
      scan failed `bundle_precompressed_assets` on 2026-09-07 - YouTube serves files as-is, no
      Content-Encoding, so .br never executes). Wasm linker target, threads off, exceptions set to
      Explicitly Thrown Only, `stripEngineCode: 1`, .NET Standard 2.1. **Size budget must be
      re-measured on the uncompressed build**; the 10.46 MB figure was Brotli.
- [x] IL2CPP - not a task after all: WebGL only supports IL2CPP, so the empty `scriptingBackend` is
      not a risk.
- [x] **Managed stripping level is High for WebGL** (`managedStrippingLevel WebGL: 3`), with
      `Assets/link.xml` covering the reflection-reached types. Superseded the note below, kept for
      the reasoning: it previously had no WebGL entry, so it fell back to the default rather than
      High. Deliberately left alone: at 10.46 MB of 30 MB the saving is not needed, and High is
      precisely what breaks Zenject and UniRx reflection at runtime in a release build.
- [x] **Dead modules purged**, including the banned `unityanalytics`. `physics` and the
      unitywebrequest modules stay: UniRx references both. No measurable size change, since
      `stripEngineCode` was already keeping them out of the payload.
- [~] **Every audio clip imports at `quality: 1` (100%) with Decompress On Load** - `main_theme.wav`,
      `big_bowl.mp3`, `starfruit.wav`, `splash.ogg`, `tada.ogg`. The music alone is 3 MB and gets
      fully expanded in memory at load.
- [~] **TextMesh Pro is 6.8 MB in-repo**, the single heaviest folder, mostly editor docs and font
      atlases that should not ship.
- [x] `webGLDecompressionFallback: 0`. Moot now that compression is Disabled; nothing to decompress.

### Verified clean

- [x] **No external network calls.** No `UnityWebRequest`, no `http(s)://`, no `WWW` anywhere in
      `Assets/Scripts`.
- [x] No IAP or purchase paths; ads only, behind `IPlatformServices`.
- [x] No sharing prompts, no external links, no extra user agreements in-game.
- [x] Rewarded placements are player-initiated with a graceful no-fill path.
- [x] **Interstitial every 5 levels finished** (2026-09-12), fired by `LevelController` when the
      level preview appears, never on launch. `IPlatformServices.ShowInterstitialAd` returns only
      whether the SDK accepted the request: the wrapper reports a status synchronously and has no
      completion callback, so there is nothing to await and a refusal is just ad pacing. Nothing in
      the game waits on it or branches on the result.

### Submission housekeeping

- [x] `companyName` set to **SecondOpinionPractice**.
- [x] `bundleVersion` set to **1.0.0**.
- [~] Thumbnails generated in `Submission/Thumbnails/` at all three required ratios (1:1, 5:7,
      16:9), in both a titled and a text-free set. **Submitting the titled set is a deliberate risk**:
      Google's design requirements forbid branding or logos in thumbnails. The text-free set exists
      so a rejection costs a re-upload, not a redesign. Source art is only 687x1024, so 16:9 upscales
      ~1.86x and looks soft; re-exporting the original at 2000px+ would fix that.
- [x] `health.logError` / `logWarning` wired via `PlatformHealthReporter`, which forwards Unity's
      log stream to the platform. Errors, exceptions and asserts always; warnings only if the
      installer's toggle is on, since the API is rate-limited and warnings crowd out real errors.
      Guarded against recursion (reporting an error can itself log) and capped at 50 reports a
      session, because one broken frame can log the same exception every Update.
- [x] `runInBackground` set to **1**. Decided rather than defaulted: with `onPause` wired, YouTube
      is the single authority on pausing. Leaving Unity's focus-loss pause enabled would have given
      two independent pause paths that can disagree, and focus events are unreliable inside a
      webview.

## Certification plan

Ordered so that each step either de-risks or measures the ones after it. One step at a time.

### 1. Baseline WebGL build and size report - DONE (2026-08-23)

`Assets/Scripts/Editor/WebGLBuildScript.cs`, runnable from `Tools > Fruit Salad` or headless via
`-executeMethod PlayablesEditor.WebGLBuildScript.BuildWebGL`. Exits non-zero on build failure or
budget overrun, so it drops straight into CI. It changes no player settings by design: a baseline has
to measure the project as it stands.

First build: **6m15s, zero errors, 10.44 MB against the 30 MB budget - 19.56 MB under.**

| file | size | |
|---|---|---|
| `WebGL.wasm.br` | 6.43 MB | 62% - engine and game code |
| `WebGL.data.br` | 3.90 MB | 37% - scenes, textures, audio, fonts |
| framework + loader + html + TemplateData | 0.11 MB | negligible |

Three conclusions:

- **IL2CPP survives Zenject and UniRx.** The single biggest technical risk on the project, half
  retired. High stripping is still unproven, but conversion itself is fine.
- **The weight is code, not content.** Audio reimport and the TextMesh Pro trim only ever touch the
  3.90 MB data file; stripping and the module purge touch the 6.43 MB wasm. Worth knowing before
  spending effort on either.
- **The size pass is no longer a blocker.** At roughly a third of budget it becomes optimisation, and
  the cost/benefit of stripping High has inverted - see Open decisions.

Also observed: the build used Unity's stock WebGL template, so `index.html` and `TemplateData/` still
carry Unity branding. That has to become the Playables template in step 8.

### 2. YouTube wrapper in, `YTGamePlatform` - DONE (2026-08-23)

From `github.com/google/web-game-samples`, `Unity/GoogleYTGameWrapper.unitypackage`. Extracted rather
than imported, so the files could be read before landing them: `Assets/Scripts/YTGameSDK/` and
`Assets/Plugins/UnityYTGameSDKLib.jslib`, Apache 2.0, LICENSE kept alongside.

`IPlatformServices` is now the full surface and is shaped after the wrapper as it actually is, not as
the JS docs describe it. Five things would have been wrong if guessed:

- The jslib calls `unityGameInstance.SendMessage(...)`, a **global the page must set**. Without it
  pause, resume, audio changes, save loads and rewarded results are all silently dropped.
  The page sets it only when `createUnityInstance()` resolves, which is after C# has started, so a
  callback in that window used to throw on the undefined global. Since 2026-09-07 every send goes
  through a `$ytgameSend` helper in the jslib that retries every 50 ms until the instance exists,
  and `loadData()` has a rejection handler that answers with an empty save instead of hanging.
- The receiving GameObject must be named **exactly `YTGameWrapper`** - the name is hardcoded in JS.
- The page must load `https://www.youtube.com/game_api/v1`, or `ytgame` is undefined and every
  wrapper call quietly no-ops. That external script is the platform API rather than a game asset, so
  it does not conflict with the no-external-calls rule.
- **`InPlayablesEnv()` returns true in the editor**, so it cannot be the only thing selecting the
  implementation. `MonetizationInstaller` guards on `UNITY_WEBGL && !UNITY_EDITOR` as well, which
  also keeps a plain browser build on the stub.
- Score is an **int**; `ScoreModel` holds a float. Conversion belongs at the boundary in step 6.

Rewarded ads come back as a single bool with no way to tell a dismissal from an unfilled request, so
both collapse to `NoFill` - which callers already treat identically. `EditorStubPlatform` grew to
match the same surface, saving to `PlayerPrefs` and deferring `LoadData` by a frame so callers cannot
accidentally depend on a synchronous answer the real SDK will never give.

Wired in the scene: a `YTGameWrapper` root GameObject, assigned to `MonetizationInstaller`.

### 3. `firstFrameReady()` and `gameReady()` - DONE (2026-08-23)

`GameLifecycleController`, bound NonLazy alongside `IPlatformServices`.

`firstFrameReady` fires at the end of the first frame, once the intro canvas has actually rendered,
so the claim that something is on screen is true rather than merely imminent.

`gameReady` fires when the intro is finished with. This game has no main menu, so "interactable"
means the intro has cleared and the board is live. The intro ends two ways - the tween chain
deactivates its canvas from the final `onComplete`, or the player skips - and **both end with
`IntroView`'s GameObject being disabled**, so `OnDisable` catches both without the two paths having
to agree on anything. `Initialize` also reports immediately if the root is already inactive.

Two deliberate choices:

- **Sending gameReady also sends firstFrameReady first**, in case the intro is skipped on the opening
  frame. The platform should not hear the game is ready before it hears there was a frame.
- **A missing gameReady warns rather than self-heals.** Firing it on a timer would clear the spinner
  over a splash nobody can interact with, turning a wiring mistake into the exact certification
  failure the rule exists to prevent. It logs after 20s instead.

### 4. `onPause` / `onResume` and platform audio state - DONE (2026-08-23)

**Pause** is handled in `GameLifecycleController`: `Time.timeScale = 0`, `AudioListener.pause = true`,
and a new `InputBlockReason.PlatformPause`.

timeScale rather than a bespoke flag, because the requirement is that *timers* stop, not just input.
Everything on UniRx's default scheduler and every DOTween tween follows it, so the whole game freezes
together without each system needing its own pause path. Audited, and the split is deliberate:

| freezes on pause | keeps running |
|---|---|
| Starfruit spawn stagger | stubbed ad timer |
| game over delay | intro sting fade |
| delayed one-shot sounds | ad-unavailable message |
| game-ready warning timer | |

The three that keep running are all `MainThreadIgnoreTimeScale` or `SetUpdate(true)` on purpose: an
ad resolving, a fade completing and a message clearing should not be held hostage by a pause.

The previous timeScale is captured rather than restored to a hardcoded 1, and `AudioListener.pause`
freezes audio in place instead of silencing it - separate from the mute toggle, which works on
listener volume.

**Audio state** is seeded and followed in `SoundController`: `IsAudioEnabled` at startup, then
`AudioEnabledChanged` into `SoundModel.SetIsPlatformAudioEnabled`. Reading it at startup matters as
much as following changes - assuming audio begins enabled would break the case where the player
already had YouTube muted before the game loaded.

### 5. Progression and the finite-content signal - REVERSED AND DONE (2026-09-12)

The endless-with-an-unreachable-threshold decision was overturned by publisher feedback: the game
needed real progression to be approved. It now has **100 levels** in `Assets/Scripts/Level/LevelSet.cs`,
authored in code as an ordered list of `LevelDefinition` (board resolution, spawn pool, starting
Starfruits, up to three fruit objectives).

What changed as a result:

- `LevelController` owns the level loop. Nothing else starts a level; the board and the fruit
  objectives no longer build themselves in their own constructors.
- **The board resizes per level.** `BoardModel.GridResolution` is the source of truth and is
  reactive; `BoardConfig` only supplies the starting value. Cells are square and capped so a board
  is never taller than it is wide, which is what lets levels be 9x3 strips or 4x9 letterboxes.
- **The game over screen became the level preview**, shown on launch as well as between levels:
  level number, grand total, and the next level's objectives.
- **The bowl stopped being a limit.** It now measures the level's objectives added together, and
  fills as goal fruit lands in it. Nothing ends a level but finishing its objectives; there is
  still no lose condition.
- **High score was replaced by a grand total** (`SaveData.GrandTotalScore`), which is what
  `sendScore` sends. It only ever grows, so the certification rule that the score sent matches the
  score saved holds by construction.
- **Bigger Bowl is retired.** With the bowl measuring objectives there is no capacity to buy, so
  `BoosterController` refuses it. Starfruit is the only rewarded placement left, plus a natural
  interstitial slot between levels that did not exist before.
- The save now holds `CurrentLevelIndex`, `GrandTotalScore` and `HasCompletedAllLevels` alongside
  the tutorial flag. All additive, no schema bump - every default is the right reading of an older
  payload.

### 6. Save system and score consistency - DONE (2026-08-23)

`Assets/Scripts/Save/`: `SaveData` (versioned payload), `SaveMigrator`, `SaveModel`, `SaveController`.
`PlayerPrefs` is gone from game code - it survives only inside `EditorStubPlatform`, where local
storage is the point.

**Version-stamped from the first shipped build**, with the migration mechanism in place even though
there is nothing to migrate yet. Retrofitting a version onto saves already in the wild is guesswork,
and the migrator has a guard against a migration that fails to advance the version, which would
otherwise spin forever.

**Never overwrites newer data with older.** A payload whose schema is newer than this build's is
loaded read-only and saving is refused outright: fields this build does not know about are already
lost from the in-memory copy, so writing would destroy them permanently for a player who ran a newer
version elsewhere.

**Score consistency is structural, not maintained.** `sendScore` is derived from the same
`SaveData.HighScore` field the save persists, so the two cannot drift. `SaveModel.SetHighScore` is
monotonic, so no worse run can lower a best score.

**Saving is driven by data, not call sites.** `SaveController` persists off `SaveModel.Changed`,
debounced 0.5s, so anything added to the payload later is saved without hunting for every place that
changes it. `onPause` skips the debounce entirely, because the platform gives only a short window
before eviction.

One race worth naming: a run can finish before `loadData` returns. `ApplyLoaded` merges rather than
assigns, taking the higher score, so a score set in that window survives instead of being overwritten
by the stored one.

Verified by simulation: fresh, same-schema, older-schema, newer-schema, corrupt and
finished-before-load all behave; only the newer-schema case refuses to save; the high score is
monotonic; and the score sent matches the saved score in every case.

### 7. Size pass - REOPENED 2026-09-07, the 30 MB limit is per extracted file

The portal rejected the round-2 ZIP outright: `Build/WebGL.wasm exceeds the 30 MB limit (37.7 MB)`.
The ZIP itself was 15.5 MB. So the budget is **per file, on the extracted, uncompressed content**,
and everything below that argued from the 10.46 MB Brotli figure is void. The wasm alone has to
come under 30 MB, and comfortably, because content growth all lands in the same file.

Levers pulled (all size-only, no gameplay change):

- `managedStrippingLevel` WebGL 1 (Low) -> 3 (High), with `Assets/link.xml` preserving
  `Assembly-CSharp`, `Zenject`, `Zenject-usage`, `UniRx`, `DOTween`, `DOTweenPro`, `DemiLib`
  whole. The stripper then only cuts UnityEngine and the BCL, which is where the bulk is, and the
  reflection risk in Zenject and UniRx is fenced off. Residual risk: BCL types UniRx builds by
  reflection (`MakeGenericType` on System types). Test the stripped build, not the editor.
- `il2cppCodeGeneration` WebGL -> 1 (Faster (smaller) builds). Shares generics harder; typically
  20-30% off the wasm on its own.
- **Editor-side, not in ProjectSettings:** Build Profiles > Web > Code Optimization must be set to
  **Disk Size with LTO**. It lives in `EditorUserBuildSettings`, so it has to be clicked.

Result (2026-09-07, Disk Size with LTO + smaller builds + stripping High): **wasm 37.7 -> 19.92 MB,
data 10.9 -> 9.77 MB, ZIP 12.7 MB.** Per-file limit cleared with 10 MB of headroom. Stripped build
smoke-tested locally and through the portal preview, cloud save section passed.

**Missed for round 2, do next build:** the Unity splash screen is still on and its logo texture is
2.7 MB, the single largest asset in the data file. Unity 6 no longer requires it. Also queued from
the build report: `garden_background.jpg` max size 2048 -> 1024 (1.5 MB), drop LiberationSans and
EmojiOne from TextMesh Pro (1.7 MB, repoint `prefab_node.prefab` first), `main_theme.wav` quality
0.5 + streaming (0.5 MB). The 512 KiB per-file warning stays; no Unity build can clear it.

Two scan-only follow-ups from the same round, both handled by `PlayablesPostBuild.cs` and the
template: Unity's runtime contains `screen.orientation.lock` (dead for us, now neutralised after
every WebGL build), and the portal greps HTML comments for `unityGameInstance.SendMessage` too.
See `Submission/MEDIACUBE-REPORT-2.md`.

#### Previous conclusion (2026-08-23, superseded)

**Purged 16 dependencies** (2026-08-23): `unityanalytics`, ai, androidjni, cloth, director,
screencapture, terrain, terrainphysics, umbra, vehicles, video, vr, wind, xr, plus the
`ai.navigation` and `timeline` packages.

**Kept, because plugins need them.** UniRx references `UnityWebRequest` in `ObservableWWW`, and
`Collision`/`Collider` in its collision and trigger observables. Removing the physics module broke
the build outright; the web request modules would have too. The lesson generalises: **audit
`Assets/Plugins`, not just `Assets/Scripts`** - the game not using a module says nothing about
whether a plugin does.

**The purge saved nothing measurable: 10.43 MB before, 10.46 MB after.** `stripEngineCode` was
already on, so unused modules were never reaching the payload in the first place. They were dead
weight in the project, not in the build. Worth having done for `unityanalytics`, which the hub spec
bans regardless of size, but the size argument for the rest was wrong.

That result also settles the remaining size work: **skip it.**

- **Stripping High** would only touch managed code, and it is exactly what breaks Zenject and UniRx
  reflection at runtime in a release build. Real risk for a saving we do not need at a third of
  budget.
- **Audio reimport and the TextMesh Pro trim** only ever touch the 3.90 MB data file. Best case is a
  couple of megabytes against 19.5 MB of headroom.

Revisit only if content growth pushes the payload past roughly 22 MB.

Stripping is the risky half: it is where Zenject and UniRx reflection finally gets tested, and it
wants a stripped release build to verify, not the editor.

### 8. Submission prep

`companyName`, version, thumbnail to spec, WebGL template, `health.logError` wired to the exception
handler, `system.getLanguage` for localisation, then the full release-candidate testing checklist.

## Open decisions

1. ~~**Unity version.**~~ Settled: migrated to Unity 6 (`6000.0.60f1`).
2. ~~**Progression design.**~~ Settled 2026-09-12: a fixed ladder of 100 levels, ending in a real
   end-of-content signal. See step 5.
3. ~~**Session-depth model.**~~ Settled 2026-09-12: the level ladder is the meta layer. The
   between-levels preview screen is also the natural interstitial slot.
4. **Ad placements after Bigger Bowl.** Retiring it left Starfruit as the only rewarded slot. The
   interstitial half is now done - one every 5 levels, on the preview screen
   (`LevelController.LevelsPerInterstitial`). Still open: a second rewarded placement to replace
   Bigger Bowl, most obviously "double this level's score into your grand total" on completion.
   Rewarded is the better eCPM case and there is currently only one.
5. **Level pacing is unplayed.** The 100 levels are authored against reasoned density estimates,
   not playtesting. The long ones (60, 80, 96, 100) are the most likely to need trimming.
