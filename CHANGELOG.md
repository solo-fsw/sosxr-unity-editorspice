# Changelog

All notable changes to this project will be documented in this file.
The changelog format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)

## [0.8.2] 23-11-2026

Fixes from the audit of older EditorSpice code (2026-11-17).

### Fixed

- `MissingReferencesFinder` threw a `NullReferenceException` at the first missing script: it called `GetType()` on the null component.
- "Search in all scenes" opened each scene without asking to save, so unsaved changes were lost. It now asks first, stops on Cancel, and reopens the scenes that were open before.
- `SceneCameraSync` was on by default and overwrote the Scene view's camera settings every frame. It is now off by default (a setting you changed yourself is kept).
- Essential Importer: DOTween, Editor Console Pro and Asset Hunter PRO had each other's publisher folders, so they were never found. The paths are corrected, and a package that is not in the expected folder is searched for in the whole Asset Store cache. Linux now uses `~/.local/share/unity3d`, and the custom cache location from Preferences is used on every platform.
- Extended inspectors (Canvas, VideoPlayer, XR interactors and interactables): multi-object editing showed "Multi-object editing not supported". With several objects selected, the built-in inspector is now shown without the SOSXR tools. When the built-in editor type is not found, the default inspector is shown instead of an empty one.
- `EditorGUIHelpers.CreateNestedEditor` created a new Editor on every GUI event. The nested editors are now cached and destroyed in `OnDisable`.
- `CanvasExtendedEditor` assigned `Camera.main` to every World Space Canvas without a camera as soon as it was shown, without Undo and without marking the scene dirty, and logged a warning on every repaint when there was no Main Camera. It is now a button, with Undo.
- `LockInspector` (Ctrl/Cmd+L) toggled Constrain Proportions on the inspected Transforms without Undo. It now records Undo and marks them dirty.
- `PivotUtilities`: Create and Delete Pivot had no Undo. Delete Pivot deleted any selected object, also one without children. It now refuses an object without children, asks first when the object has other components, and supports Undo. A pivot for a root object is created in that object's scene.
- `CreateMaterialsForTextures` used `Shader.Find("SimpleLit")`, which finds nothing, and then threw. It now uses the render pipeline's default shader, or `Standard`.
- "Extract Component to Child" gave the child the parent's local position, rotation and scale again, so it was offset. The child now sits on its parent. Extracting a Transform does nothing.
- `ContextProperties` (Vector3 Zero Out / One) used the `SerializedProperty` after the menu closed; it now uses a copy.
- UnityEvent "Invoke" buttons threw for events inside serializable classes, structs and lists, and only invoked the first selected object. They now follow the property path and invoke every selected object.
- `SetIconWindow`: an exception during the icon import left asset importing locked. The scaled preview textures are now destroyed with the window.
- `PackageIsInstalled.PackageInstalled` returned false for packages installed before it was first used. It now asks the Package Manager and caches the answer until packages change.
- `Package.IsInstalled` only listed direct dependencies and never cleared its cache.
- `MissingMonoBehaviourDetector` skipped inactive GameObjects, and removed missing scripts without Undo. It now uses `GameObjectUtility.RemoveMonoBehavioursWithMissingScript` with Undo.
- `CreateCustomGameObject` had no Undo and did not mark the scene dirty.
- XR debug buttons invoked the events with `null` args, so listeners that read the args threw.
- `VideoPlayerExtendedEditor` showed two-digit milliseconds for clips of a minute or longer.
- AutoSave: an exception while saving ended the auto-save task until the next domain reload. It is now logged and auto-save continues.

### Added

- `UnityEventButtonBase.GetEvents<T>(SerializedProperty)` and `UnityEventButtonBase.TryGetValueAtPath(object, string, out object)`.
- `EditorGUIHelpers.InternalEditorTypeMissing`.
- EditMode tests: `OlderCodeFixTests`.
- README: Extended Inspectors, Pivot Utilities, Scene Camera Sync.

### Changed

- `MissingMonoBehaviourDetector` and `MissingReferencesFinder` are static classes instead of `MonoBehaviour`s. They are in an Editor-only assembly, so as components they would be missing in builds.

### Not changed

- `DefineSymbolManager` still adds `SOSXR_EDITORTOOLS_INSTALLED` (only when it is missing): other projects may check it. The README now says that removing it on uninstall is not guaranteed.
- `ReadmeShower` still selects the README once per Editor session, by design.

## [0.8.1]

### Added

- `AudioPreviewer`: select an AudioClip in the Project window and press Space to play it. Press Space again to stop. Ignored while a text field is being edited or when Shift, Ctrl, Alt or Cmd is held.
- `AudioPreviewer.ShouldPlay`, `AudioPreviewer.IsSpacebarPreviewEvent` and `AudioPreviewer.TogglePreview`, with EditMode tests: `AudioPreviewerTests`.
- `BatchPackageUpdater` shows the version a Git package updates to (`0.7.0 → 0.8.0  (abc1234 → def5678)`). It reads `package.json` at the remote commit with a shallow, blobless fetch into a temporary folder.
- `PackageUpdateMath.GetPackageJsonPath`, `ParsePackageJsonVersion` and `FormatGitUpdateMessage`, with EditMode tests.

### Changed

- `AudioPreviewer` remembers the last played clip by reference instead of by instance or entity id.

### Fixed

- `AudioPreviewer.IsPreviewClipPlaying` returns `false` instead of throwing when Unity's internal `AudioUtil` method is missing.
- `BatchPackageUpdater` no longer logs "Update failed:" with an empty error after a successful update. An update that reloads the domain (for example of EditorSpice itself) left a stale request behind. The window now remembers the pending updates in `SessionState`, and after the refresh it checks each package's installed commit or version and logs which packages updated.

## [0.8.0] 17-11-2026

### Added

- `AudioEditor` window: trim, fade, preview, and save an AudioClip as `<name>_edited.wav`. Menu: `SOSXR/Audio Editor`, `Assets/SOSXR/Edit Audio Clip`.
- `AudioClipProcessor` static helpers for trim, fade, waveform peaks, marker interval, and time formatting.
- `WavUtility.FromSamples` to encode float samples as 16-bit PCM .wav data. Samples outside -1 to 1 are clipped.
- `VideoEditor` window: set a trim range on a `VideoClip`, preview it in Play Mode and record it to `<clip>_Trimmed.mp4`. Menu: `SOSXR/Video Editor`, `Assets/SOSXR/Edit Video Clip`.
- `VideoTrimPlayer` plays a trimmed range into a RenderTexture via a temporary, unsaved GameObject.
- `VideoTrimRecorder` records the trimmed range with the Unity Recorder (`com.unity.recorder` 5.0.0 or newer, gated by `EDITORSPICE_RECORDER`).
- `VideoTrimMath` helpers for trim clamping, frame index, even render sizes, and output paths.
- EditMode tests: `AudioClipProcessorTests`, `WavUtilityTests`, `AudioEditorTests`, `VideoTrimMathTests`, `VideoEditorTests`.
- `BatchPackageUpdater` window: check the project's Git (and optionally registry) packages for updates, select several, and update them in one Package Manager request. Menu: `SOSXR/Batch Package Updater`.
- `PackageUpdateMath` helpers for package ids, Git URLs, `git ls-remote` output and SemVer comparison. EditMode tests: `PackageUpdateMathTests`.
- `PackageUpdateMath.GetLsRemotePatterns(string)` and `PackageUpdateMath.ParseLsRemoteHash(string output, string revision)`.

### Changed

- `package.json` minimum Unity is now `6000.4` (was `6000.0`). The package already used `EntityId` APIs without version guards: `AssetCreationEndAction` and `EditorApplication.hierarchyWindowItemByEntityIdOnGUI` first appear in 6000.4.
- README: Hierarchy tool sections say which Hierarchy window they work in.
- `BuildInfoManager` sets each build's SemVer and Android bundle version code to the last successful build plus one. Failed, cancelled and unfinished builds no longer consume or undo version numbers.
- `BuildInfoDetails` stores `LastSuccessfulSemVer` and `LastSuccessfulBundleVersionCode`. Existing projects seed them once from PlayerSettings.
- `AudioEditor` moved from `WIP/AudioEditor.cs` to `Editor/AudioEditor/`. Assembly: `SOSXR.EditorSpice.EditorScripts`.
- `VideoEditor` changed from a WIP MonoBehaviour (`WIP/VideoEditor.cs` + `VideoEditorEditor.cs`) to an EditorWindow in `Editor/VideoEditor/`. Assembly: `SOSXR.EditorSpice.EditorScripts`.
- Sample scene `Demo_Scene_VideoEditor` no longer contains the `VideoEditor` component.
- `BuildInfoManager.RecordSuccessfulBuild` now takes a second argument, `bool androidBuild`.

### Fixed

- `HierarchyScriptDropHandler` opened a legacy Hierarchy window when the new Hierarchy window (default in Unity 6.6) was in use, and the new GameObject was not renamed. It now uses Unity's internal "frame and rename new GameObject" hook, which both Hierarchy windows handle. Fallback: send `Rename` to an already open Hierarchy window; never opens one.
- Sample `ToggleUsingHierarchyIcon` did nothing in the new Hierarchy window. From Unity 6.5 it also hooks `HierarchyWindow.BindViewItem` and toggles when the pointer goes down on the item icon. The legacy Hierarchy path is unchanged.
- Sample `ToggleUsingHierarchyIcon` had a pre-6000.3 branch that could not compile (handler takes `EntityId`). Removed.
- `WavUtility` wrote the header over the first 44 bytes of sample data. The header now comes before the samples.
- `WavUtility` wrote an incorrect RIFF chunk size. The size now covers all data after the size field.
- Build Helpers: a successful non-Android build no longer overwrites the stored Android bundle version code, so the next Android build cannot get a lower code.
- Build Helpers: numbers and dates in `build_info.csv` are written with the invariant culture, so a decimal comma no longer adds a column.
- Build Helpers: an empty or multi-character development/production indicator no longer throws.
- Build Helpers: Unity Test Runner player builds no longer change the version, the counters or the CSV.
- Batch Package Updater: a branch or tag name is now matched exactly, so `main` no longer matches `feature/main`.
- Batch Package Updater: registry packages that are up to date can no longer be selected, so "All" cannot downgrade a package.
- Markdown Viewer `package.json`: removed a samples entry that pointed to a missing folder.

### Removed

- `BuildInfoManager.ChangeSemVer` and `BuildInfoManager.ChangeAndroidVersion` (replaced by `GetNextVersion` and `RecordSuccessfulBuild`)
- `WIP/` folder, including `WIP/AudioEditor.cs`, `WIP/VideoEditor.cs`, and `WIP/VideoEditorEditor.cs`.

## [0.7.0] 09-11-2026

### Added

- EditMode tests in `Tests/Editor` (asmdef `SOSXR.EditorSpice.Tests`)
- `DefineSymbolUtility` for whole-symbol define list handling

### Changed

- Samples moved back from `Samples~/` to visible `Samples/`
- AutoSave starts after load, finds its config by type, and saves only dirty scenes that have a path
- Essential Importer installs packages in one request
- `DefineSymbolManager` removal triggers when the package root folder or its package.json is deleted
- `HierarchyScriptDropHandler` leaves drops on GameObjects to Unity
- `ReadmeShower` picks the README closest to the Assets root

### Fixed

- `SetScriptingDefineSymbols` appends `EDITORSPICE` instead of replacing the whole define list
- Define symbol checks match whole symbols, not substrings
- `AssetDependencyGraph` no longer recurses forever on cyclic references and no longer dereferences null nodes
- UnityEvent visualiser works with GameObject and ScriptableObject senders and with four-argument events
- UnityEvent listeners are no longer added on every rebuild and are removed on clear
- `ProjectPresets` no longer skips later presets
- `BuildInfoManager` saves its counters and first build advances the version
- AutoSave no longer overwrites an existing config, no longer pops Save-As for untitled scenes, and skips playmode transitions
- `VideoPlayerExtendedEditor` restores background colour and stops its update monitor
- `HierarchyIconDisplay` sample no longer reopens a closed Hierarchy
- Markdown editors dispose web requests and textures
- `UNITY_MAC` replaced with `UNITY_EDITOR_OSX`

## [0.4.0] In Progress

### Added

- Dependent on 'EditorCoroutines' package
- `[DisableEditing]` attribute for EditorSpice

### Changed

- Moved AudionSourceEditor to the Mutable Sample folder, since it can conflict with [Bro Audio](https://github.com/man572142/Bro_Audio)
- Improved the BuildInfo generator: now includes scripting backend (Mono/IL2CPP) and only updates SemVer on successful builds

### Fixed

### Removed

- SettingsProvider. This now has it's own package
- Moved some classes that were not ready yet into a Work In Progress (WIP) folder.

## [0.3.2] 22-07-2025

### Added

- SettingsProvider
- FlatIcon to markdown attribution fixer

### Changed

- Links to Essential Packages is in Project Settings now. Choose between Dev or Stable.

### Fixed

- Path in demo video
- Unity Version "6000.0"

## [0.3.1] 02-04-2025

> ### Package Numbering Change
> #### Package will now be numbered starting with 0, to better reflect the current status in development (see the official semver information [here](https://semver.org/#spec-item-4)).
>
> If any issues arise when updating from previous (and higher numbered versions), please delete the old version before updating to this version.

### Changed

- Now called `EditorSpice` instead of boring `EditorTools`

### Added

- Build Info Details editorscript that shows the build info in the editor
- PackageName to build info
- [ReadMeHelpers v2.0.0](https://github.com/solo-fsw/sosxr-unity-readmehelpers)
- [Markdown Viewer v1.2.1](https://github.com/solo-fsw/sosxr-unity-markdownviewer), which is a fork
  of [gwaredd's work](https://github.com/gwaredd/UnityMarkdownViewer)
- [Asset Dependency v2.0.0](https://github.com/solo-fsw/sosxr-unity-assetdependency), which in itself is a combination
  between [What Uses This](https://github.com/Facepunch/WhatUsesThis)
  and [Asset Dependency Graph](https://github.com/Unity-Harry/Unity-AssetDependencyGraph).
- Icons that were in SeaShark before
- Essential Importer
- Video Player
- Roadmap file
- [UnityEvent Visualizer](https://github.com/MephestoKhaan/UnityEventVisualizer?tab=readme-ov-file)
- [Pivot Utilities](https://gist.github.com/talecrafter/519e260d93dbf236484acfe625faa1dc)
- [Missing References finder](https://github.com/liortal53/MissingReferencesUnity)
-

### Removed

- IValidate
- Moved Attributes to [SeaShark](https://github.com/solo-fsw/sosxr-unity-seashark)
- Moved DrawGizmo to [SeaShark](https://github.com/solo-fsw/sosxr-unity-seashark)
- ASMDF of Tarodev, Markdownviewer and others

### Fixed

- Directory structure from the FileHelpers
- BuildInfoToUI wasn't showing the correct information
-

### Updated

- Updated the README.md

## [2.2.0] - 2025-02-10

### Added

- [Tarodev's AutoSave](https://www.youtube.com/watch?v=q0ZDlhPs8mU)
- Build Helpers

## [2.1.1] - 2025-02-05

### Changed

- Names
- Detecting that BroWar collective might be installed
- New samples with some 'Mutable' files

## [2.1.0] - 2025-01-31

### Added

- Added samples as a separate thing: download them through the 'Samples' button in the package manager

## [2.0.0] - 2025-01-31

### Changed

- Changed from GNU GPL 3 license to MIT license

## [1.7.1] - 2025-01-22

### Fixed

- UnityEvent drawer
- Demo scenes
- Iconography

## [1.7.0] - 2025-01-21

### Added

- Create custom GameObject

## [1.6.0] - 2025-01-08

### Added

- Audio Clip tool

## [1.5.2] - 2025-01-07

### Fixed

- Adding dependency to package.json
- Minor fixes

## [1.5.1] - 2024-12-09

### Added

- Buttons for float, int, string UnityEvents

### Fixed

- Button on UnityEvent
- Speling

### Removed

- Removed not working Buttons

## [1.4.0] - 2024-11-25

### Added

- Required attribute
- UnityEvent with buttons for easy debugging
- Suffix attribute

## [1.3.0] - 2024-11-23

### Added

- Editor define symbols manager
- Updated TagSelector
- Added DrawGizmo class + editor. Is now in one class, and you can select what type of Gizmo it should represent.

## [1.2.0] - 2024-11-22

### Added

- Editor for button extended with a button

### Changed

- Only use HierarchyIcon and Toggle in Hierarchy when BroWar's EditorToolBox is not installed

### Removed

- Removed DescriptionAttribute since it was not working.

## [1.1.0] - 2024-09-20

### Changed

- Creating markdown files with the option to rename instantly.

## [1.0.0] - 2024-09-13

### Added

- Package initialization

