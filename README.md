# Orbit-Toss
VR version of the famous game Boccia: a single-player Boccia practice prototype for Meta Quest with a simple space theme (Team THE MAN).

## Open the project
1. Install **Unity 6000.3.9f1** with **Android Build Support** (incl. OpenJDK + Android SDK & NDK) via Unity Hub.
2. Clone this repo, then Unity Hub → **Add project from disk** → select the cloned folder.
   The first open takes a while: Unity downloads the packages (including Meta XR SDK v209 from Meta's registry) and rebuilds `Library/`.
3. Open the scene **`Assets/OrbitToss/Scenes/OrbitToss.unity`**.

## Run on a Quest
- Headset in Developer Mode, connected by USB → **File → Build Profiles → Android → Build And Run**, or menu **Orbit Toss → Build Quest APK**.
- No headset: **Window → Meta → Meta XR Simulator → Activate**, then press Play.

Controls: hold **GRIP** to pick up a planet ball, swing and release to throw; point at menu buttons and pull the **TRIGGER**.

Full documentation (court geometry, scripts, how to replace visuals): [`Assets/OrbitToss/README.md`](Assets/OrbitToss/README.md).
Deferred work: [`Assets/OrbitToss/TODO.md`](Assets/OrbitToss/TODO.md). Progress screenshots: [`ProgressReport_Screenshots/`](ProgressReport_Screenshots).
