# Orbit Toss — Boccia VR prototype (Team THE MAN)

Single-player Boccia **practice** prototype for Meta Quest, with a simple space theme.

## Versions (verified on this PC, 2026-11-22)
| Item | Version |
|---|---|
| Unity Editor | 6000.3.9f1 (the `6000.3.9f1-x86_64` install — it has Android Build Support, SDK/NDK, OpenJDK) |
| Meta XR Core SDK | com.meta.xr.sdk.core 209.0.0 |
| Meta XR Interaction SDK | com.meta.xr.sdk.interaction 209.0.0 + com.meta.xr.sdk.interaction.ovr 209.0.0 |
| Unity OpenXR | com.unity.xr.openxr 1.19.0 (Meta Quest Support + Meta XR Feature enabled for Android) |
| Render pipeline | URP 17.3 |

Meta packages come from Meta's official scoped registry `https://npm.developer.oculus.com` (see `Packages/manifest.json`).
`com.meta.xr.sdk.all` (All-in-One) is **not** published on that registry (Asset Store only, needs sign-in), so the
individual packages it bundles that this game needs were installed instead.

**Rig names:** the old "OVRInteraction" prefab is now in the SDK's `Legacy` folder. The scene uses the current
official setup: `OVRCameraRig` + `OVRComprehensiveInteractionRig` (added with Meta's Quick Action
"Add OVR Comprehensive Interaction Rig").

## Open and run
1. Unity Hub → Open → `C:\Users\Raghad\My project0` with Unity **6000.3.9f1**.
2. Open scene `Assets/OrbitToss/Scenes/OrbitToss.unity`.
3. Headset: File → Build Profiles → Android → Build And Run (headset connected by USB, Developer Mode on), or
   menu **Orbit Toss → Build Quest APK** → `Builds/Android/OrbitToss.apk`, then
   `adb install -r Builds/Android/OrbitToss.apk`.
4. No headset: **Window → Meta → Meta XR Simulator → Activate**, then press Play.
   Keep **Meta → Meta XR Operator** deactivated (it is an optional AI-agent layer, not needed, and froze the simulator once).

## Controls (Quest controllers)
- **Grab** a planet ball from the tray beside you: hold **GRIP**. Swing gently towards the Jack and let go to throw.
- **Menus:** point the controller ray at a button and pull the **TRIGGER**.
- Hand tracking also works through the Interaction SDK rig.
- Playable seated; no walking. Tray height and left/right side can be changed in Settings.

## Game flow
Main menu (Play / Tutorial / Settings / Quit) → practice round with 6 planet balls → each ball settles → distance to
the Jack, feedback ([***] Perfect, [**] Good Throw, [*] Close, [~] Try Again, [X] Out) and practice points →
round summary → RESET or MENU.

**Prototype simplifications (not official rules):** the white Jack is fixed on the target cross; one side only;
"practice points" are motivational, not an official score; a very gentle release (< 0.4 m/s) counts as a drop and the
ball goes back to the tray; low rails outside the court stop balls rolling away.
Official two-side scoring (rule 10.16) is implemented in `BocciaScoring.ScoreEnd` for a future opponent mode.

## My Best Scores (single player)
- Main menu → **MY BEST SCORES** shows your 10 best practice rounds: rank, score, best ball distance, date and time.
- About 3 s after the 6th ball settles, a **Round complete** screen shows your score and best ball. The round is saved
  automatically if it is one of your 10 best ("NEW PERSONAL BEST!" for #1); the new row is marked `>` and `NEW`.
- Saved on the device in `orbittoss_leaderboard.json` (Application.persistentDataPath), no internet or accounts.
- Code: `LeaderboardService` (save/load/ranking) and `LeaderboardUI` (panels, built at start-up from the main menu's
  button/text styles, so no scene changes and colour themes apply automatically).

## Court (World Boccia Rules 2025-2028 v1.2.1, rule 3.1 + court layout page). 1 unit = 1 m
12.5 × 6 m court; six 1 m throwing boxes, 2.5 m deep; 10 m playing area; V-line 3 m past the throwing line at the
side lines and 1.5 m at the centre vertex; cross 5 m from the front line, centred; 35 × 35 cm target box; wide tape
5 cm and narrow tape 2 cm. Ball 275 g, 270 mm circumference. Origin = inside of the back line, centre of width, +Z towards the front line.
Geometry: `Scripts/Runtime/CourtDimensions.cs`.

## Project layout (`Assets/OrbitToss`)
- `Scripts/Runtime`: `GameManager` (round flow), `BocciaBall` (physics/settling), `BallGrabThrow` (Meta grab → throw,
  velocity clamp, haptics), `JackController`, `BocciaScoring`, `DistanceCalculator`, `CourtSetup`, `CourtDimensions`,
  `VRMenuController`, `SettingsManager`, `ThemeManager` / `ThemedGraphic`, `ThrowFeedbackUI`, `BallLabel`, `BallRack`,
  `AudioManager` (generated placeholder sounds).
- `Scripts/Editor`: `OrbitTossBuild` (one-click Quest APK build).
- `Prefabs` (Jack, PlanetBall), `Materials`, `Physics` (BallPhysics / CourtPhysics), `Scenes`.

Hierarchy groups: **XR Rig, Court, Balls, Lighting, Environment, UI, Game Managers**.

## Tuning in the Inspector
- Ball: `BocciaBall` (mass, damping, rolling resistance, settle thresholds). Throw: `BallGrabThrow` (multiplier, max speed 7 m/s, drop threshold).
- Friction/bounce: `Physics/BallPhysics` and `Physics/CourtPhysics`.
- Feedback thresholds and labels: `GameManager → Thresholds` (Perfect ≤ 15 cm, Good ≤ 40 cm, Close ≤ 80 cm).
- Themes: `ThemeManager` (Space, High Contrast, Colour-Blind Friendly).
- Calibration: a ~4.5 m/s release stops near the cross; 7 m/s reaches the front line.

## Replacing visuals later (no code changes needed)
- **Background / stars / planets:** everything under `Environment` is decoration. Delete or replace it, or add a skybox
  (Lighting window). Camera background colour comes from the theme.
- **Planet balls:** edit `Prefabs/PlanetBall.prefab`. Swap the mesh/material but keep the root `SphereCollider`
  radius (0.043 m) and the Rigidbody / Grabbable / BocciaBall / BallGrabThrow components. Side colour comes from
  `Materials/PlanetBall_SideA`.
- **Jack:** `Prefabs/Jack.prefab`. Keep it white.
- **Court look:** change `CourtSurface` / `CourtLines` materials. Don't change the geometry.
- **Sounds:** assign clips on `AudioManager`. They replace the generated tones.

Edit the scene directly in Unity - it is a normal scene (no generator).
