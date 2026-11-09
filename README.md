# EditorSpice

- By: Maarten R. Struijk Wilbrink
- For: Leiden University SOSXR
- Fully open source: Feel free to add to, or modify, anything you see fit.

> [!NOTE]
> "The spice extends life. The spice expands consciousness. The spice is vital to Unity development." ~ Frank Herbert,
> Dune

## Attribution

A huge thanks goes go two channels who are creating incredible content:

- [Warped Imagination](https://www.youtube.com/@WarpedImagination)
- [git-amend](https://www.youtube.com/@git-amend) on YouTube and also
  his [repo](https://github.com/adammyhre/Unity-Utils)
  A lot of the good work shown in this repo is either a direct copy of their work, or a heavily 'inspired' version of
  it.

Quite a few of the components here are **not** by my design. They're all Open Source, and where possible I tried to
attribute to the original author. If you see something that you believe is yours, please let me know and I'll be happy
to add you to the list of contributors.

## Overview

This is a collection of Editor Tools that I've found useful in my day-to-day work. They're all designed to be as
unobtrusive as possible, and to be as easy to use as possible. I've tried to keep the number of dependencies to a
minimum, and to keep the code as clean as possible. You can add or remove these as you wish, without them influencing
your game code in any way.

### Distinction between EditorSpice and SeaShark

When using [EditorSpice](https://github.com/solo-fsw/sosxr-unity-editorspice) in your project, none of your actual 'game-code'
should be affected. These are tools to make the Editor behave in (marginally) more useful ways, but will not / should
not be embedded in your actual program. You should be able to delete the entire EditorSpice folder without affecting
your game in any way. I aim for zero null references, zero errors, and zero warnings.

[SeaShark](https://github.com/solo-fsw/sosxr-unity-seashark) however is a library of patterns and attributes that you
can use in your game. They're designed to be embedded in your game code, and to be used in your game. They're designed to
make your game better, coding more straightforward, and to make your life easier. You cannot first use SeaShark and then delete it without affecting your
game.

## Installation

1. Open the Unity project you want to install this package in.
2. Open the Package Manager window.
3. Click on the `+` button and select `Add package from git URL...`.
4. Paste the URL of this repo into the text field and press `Add`. Make sure it ends with `.git`.

### Requirements

This tool depends on [EnhancedLogger](https://github.com/solo-fsw/sosxr-unity-enhancedlogger), which needs to be installed in your project.

### Samples

Samples live in the `Samples/` folder. Import them via Package Manager > EditorSpice > Samples. They are optional and can be removed without affecting the tools.

## Tests

The EditMode tests live in `Tests/Editor`. Run them via Window > General > Test Runner > EditMode.

# Tools

## Attribution Helper

Paste FlatIcon HTML directly to Markdown. Right-click on a markdown file, to paste in the HTML attribution you get from FlatIcon like:

```html
<a href="https://www.flaticon.com/free-icons/bored" title="bored icons"
  >Bored icons created by abdanbagus - Flaticon</a
>
```

It is then amended at the end of the markdown file like so:

```markdown
- [Bored icons created by abdanbagus - Flaticon](https://www.flaticon.com/free-icons/bored "bored icons")
```

This way, you can easily use icons in your porject, while keeping a clear and consistent attribution-file.

## AudioEditor

Editor tool for audio asset management and preview. This tool makes it easier to audition, configure, and inspect audio clips without leaving the Unity editor.

### Features

- Audio preview: audition audio clips directly in the editor
- Audio import settings: adjust sample rate, channels, compression, and platform overrides
- Audio clip inspection: view length, channels, bitrate, and asset path

### How to Use

- Open Window > EditorSpice > AudioEditor to display the panel
- Select an AudioClip to preview or modify import settings in the Inspector
- Use the Preview toggle to listen without triggering full import

### Configuration

- Settings live under Preferences > EditorSpice > AudioEditor (toggle previews, default presets, and panel layout)

### Related Classes

- AudioPreviewer.cs

## AudioPreview (by Warped Imagination)

When clicking on an audio file in the editor, the Audio Preview allows you to click on it to hear it real time. By default, the Unity Editor hides this function.

## AutoSave (by Tarodev)

Save your Unity scene every X seconds / minutes. Check the Project Settings to adjust the frequency of the auto save.

- Only scenes that already have a path are saved. Untitled scenes are skipped, so no Save As popup appears.
- The config is found by type. If none exists, one is created after load. An existing config is never overwritten.
- Saving pauses while entering or in Play Mode.
- The frequency is at least 1 minute.

## Build Helpers

### Build Info: Version Management

This package increments the last digit ('patch') of the SemVer build number (found in Project Settings > Player > Version Number).
This feature is useful for tracking builds and debugging. The build number will be incremented with each successful
build, including the first one. If the version number has an unexpected format, it resets to the Initial SemVer set in the
`BuildInfoDetails` asset. It also updates the Android Bundle/Gradle version code, used in platforms like
ArborXR.

You can use TextMeshPro alongside the `ShowBuildInfo` script to display the build number in the game. Combine this with
a [DestroyInProductionBuild script](https://github.com/mrstruijk/BuildHelpers/blob/main/Runtime/DestroyInProductionBuild.cs)
to show the build info in a scene in a Development build, but have it automatically stripped from production builds.

The build number is logged to a CSV file stored at `Assets/SOSXR/Resources/build_info.csv`, including timestamps and
whether the build is production / development. It runs automatically at build start and increments the build number after a successful
build.

You must manually increment the first and second positions of the SemVer numbers. The package only increments the
final ('patch') position:

- **Automatically incremented:**
  - 1.0.0 → 1.0.1
  - 1.0.1 → 1.0.2
  - 1.0.2 → 1.0.3
- **Manually incremented:**
  - 1.0.3 → 1.1.0
  - 1.1.0 → 1.2.0
  - 1.2.0 → 2.0.0

## Destroy / Disable in (Production) Build

Use the `DestroyInBuild` script to destroy GameObjects in a build (thereby they're only active in the Editor). Use the `DisableInBuild` script to disable them in any
builds. The `DisableInProductionBuild` script keeps GameObjects enabled in the editor and development builds but
disables them in production.

## ContextProperties (by Warped Imagination)

Adds "Zero Out" and "One" context menu items to Vector3 properties in the Inspector.

## CreateMaterialsForTextures

ScriptableWizard that batch-creates materials for selected textures using a specified shader (default: `SimpleLit`). Skips textures that already have a material at the same path.

## DefineSymbolManager

Adds `SOSXR_EDITORTOOLS_INSTALLED` to the project's scripting define symbols on load. Removes it automatically when the package is deleted. Symbols are appended, so other define symbols stay untouched.

`SetScriptingDefineSymbols` adds `EDITORSPICE` the same way.

## EditorGUIHelpers

Abstract base class for custom editor windows and inspectors. Derive from it to get pre-built field helpers (float, int, vector, color, etc.), boxed layouts, and a toggle between default and custom inspector views.

## Essential Importer

Installs the Essential packages in one Package Manager request. A domain reload during the install does not stop it halfway.

## Extended Inspectors

## GameObjectUtilityExtensions

Removes missing script components from selected GameObjects. Available under `SOSXR/DANGER/Remove Missing Scripts`.

## HierarchyIconDisplay (by Warped Imagination)

Displays the most significant component icon next to GameObjects in the Hierarchy, with optional prefab icon retention and selection/hover background highlighting.

## HierarchyScriptDropHandler (by Warped Imagination)

Allows dropping script assets onto empty space in the Hierarchy to create a GameObject with that script. Dropping a script onto an existing GameObject uses Unity's default behaviour and adds the component.

## Layout Switcher Tool (by Warped Imagination)

Editor tool for quickly switching between different editor layouts.

### Features

- Save and load editor layouts
- Keyboard shortcuts for layout switching

### How to Use

- Open Window > EditorSpice > Layout Switcher Tool to display the panel
- Save the current layout to a named slot
- Use Load Layout to switch, or use configured keyboard shortcuts (e.g., Ctrl/Cmd + 1..9)

### Configuration

- Preferences > EditorSpice > LayoutSwitcher (shortcuts, default layouts, and behavior)

### Related Classes

- LayoutSwitcherTool.cs (by Warped Imagination)

## Markdown Helper

Utilities for working with Markdown files in the editor.

### Features

- Markdown preview: render Markdown in-editor
- Formatting helpers: quick formatting shortcuts and templates

### How to Use

- Open Window > EditorSpice > MarkdownHelper
- Open a Markdown asset to preview or apply formatting helpers

### Configuration

- Settings under Preferences > EditorSpice > MarkdownHelper (preview size, templates)

### Related Classes

- MarkdownViewer.cs (fork of gwaredd's work)

## MarkdownViewer (a fork of [gwaredd's work](https://github.com/gwaredd/UnityMarkdownViewer))

## MissingMonoBehaviourDetector

Scans all scene GameObjects and logs console warnings for any missing MonoBehaviour components.

## MonoBehavior Utility

Checks for missing script count on a `MonoBehaviour`. Used by `MissingMonoBehaviourDetector`.

## Readme Helpers

Creates a `README.md` from a template at the project root. Available via `Assets/Create/SOSXR/Create README.md file in Assets folder` or `SOSXR/Setup/Create README.md file in Assets folder`.

### ReadmeShower

Displays a README asset in a scrollable text area in the Inspector when attached to a GameObject. It picks the `README.md` closest to the Assets root.

## SetIconWindow (based on Warped Imagination)

Assigns custom icons to selected GameObjects. Select scripts, press `Ctrl + i / Cmd + i` to open. Icons must be tagged `scriptIcon` to appear in the grid.

## Setup Presets (from Warped Imagination)

## ToggleUsingHierarchyIcon (by Warped Imagination)

Toggles a GameObject's active state by clicking its Hierarchy icon. Supports undo and marks the scene dirty.

## Validation

To validate any class, add the `IValidate` interface and implement the `OnValidate` method to return a list of
validation errors. Errors are displayed in the console during the build process. For example:

```cs
public class ThisIsAValidationClass : MonoBehaviour, IValidate
{
    public GameObject AnotherGameObject;

    public bool IsValid { get; private set; }

    public void OnValidate()
    {
        IsValid = AnotherGameObject != null;
    }
}
```

This setup will show an error if `AnotherGameObject` is not set, preventing the build until resolved. You can mass-add
the `IValidate` interface to all MonoBehaviours in your project using the `AddValidateInterfaceToMonoBehaviours` menu
item under the SOSXR menu. The `SceneBuildValidation` script performs the validation checks.

## VideoEditor

Editor tool for video asset management.

### Features

- Video preview: audition video clips in the editor
- Video import settings: configure resolution, framerate, and encoding
- Video clip inspection: metadata, duration, and source path

### How to Use

- Open Window > EditorSpice > VideoEditor
- Select a VideoClip to preview and adjust import settings in the Inspector
- Inspect clip properties and export options from the panel

### Configuration

- Preferences > EditorSpice > VideoEditor (import presets and panel behavior)

### Related Classes

- VideoEditor.cs (WIP)

Note: This tool is currently Work in Progress. Refer to the WIP folder for latest changes and status.

# Other things

## Leiden University Logos

## URP Quest 3 Templates

## Presets
