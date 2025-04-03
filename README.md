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

Quite a few of the components here are __not__ by my design. They're all Open Source, and where possible I tried to
attribute to the original author. If you see something that you believe is yours, please let me know and I'll be happy
to add you to the list of contributors.

## Overview

This is a collection of Editor Tools that I've found useful in my day-to-day work. They're all designed to be as
unobtrusive as possible, and to be as easy to use as possible. I've tried to keep the number of dependencies to a
minimum, and to keep the code as clean as possible. You can add or remove these as you wish, without them influencing
your game code in any way.

### Distinction between EditorSpice and SeaShark

When using [Spice](https://github.com/solo-fsw/sosxr-unity-editorspice) in your project, none of your actual 'game-code'
should be affected. These are tools to make the Editor behave in (marginally) more useful ways, but will not / should
not be embedded in your actual program. You should be able to delete the entire EditorSpice folder without affecting
your game in any way. I aim for zero null references, zero errors, and zero warnings.

[SeaShark](https://github.com/solo-fsw/sosxr-unity-seashark) however is a library of patterns and attributes that you
can use in your game. They're designed to be embedded in your game, and to be used in your game. They're designed to
make your game better, and to make your life easier. You cannot use SeaShark and then delete it without affecting your
game.

## Installation

1. Open the Unity project you want to install this package in.
2. Open the Package Manager window.
3. Click on the `+` button and select `Add package from git URL...`.
4. Paste the URL of this repo into the text field and press `Add`. Make sure it ends with `.git`.

# Tools

## AudioEditor

## AudioPreview (by Warped Imagination)

## AutoSave (by Tarodev)

## Build Helpers

### Build Info: Version Management

This package increments the last digit of the SemVer build number (found in Project Settings > Player > Version Number).
This feature is useful for tracking builds and debugging. The build number will be incremented with each successful
build and reset to 0 on the first build. It also updates the Android Bundle/Gradle version code, used in platforms like
ArborXR.

You can use TextMeshPro alongside the `ShowBuildInfo` script to display the build number in the game. Combine this with
a [DestroyInProductionBuild script](https://github.com/mrstruijk/BuildHelpers/blob/main/Runtime/DestroyInProductionBuild.cs)
to remove the build number from production builds.

The build number is logged to a CSV file stored at `Assets/SOSXR/Resources/build_info.csv`, including timestamps and
whether the build is production. It runs automatically at build start and increments the build number after a successful
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

Use the `DestroyInBuild` script to destroy GameObjects or the `DisableInBuild` script to disable them during production
builds. The `DisableInProductionBuild` script keeps GameObjects enabled in the editor and development builds but
disables them in production.

## ContextProperties (by Warped Imagination)

The ContextProperties class enhances Unity's Inspector by adding custom context menu options for Vector3 properties.
When you right-click on a Vector3 property in the Inspector, two additional menu items appear: "Zero Out" and "One".
Selecting "Zero Out" sets the Vector3 property to Vector3.zero, while "One" sets it to Vector3.one. This functionality,
inspired by a solution from Warped Imagination, provides a quick way to reset or set Vector3 properties to common values
directly from the context menu, streamlining the process of adjusting these properties.

## CreateMaterialsForTextures

The CreateMaterialsForTextures class is a Unity ScriptableWizard that facilitates the creation of materials for selected
textures. When invoked from the Unity menu, it opens a wizard allowing users to create materials using a specified
shader (defaulting to "SimpleLit"). The wizard processes selected textures in the Unity Editor's Project view, creating
a new material for each texture if a material does not already exist at the same path. The script ensures that asset
editing is handled properly, logging warnings if materials already exist and saving all changes at the end of the
operation. This tool simplifies the batch creation of materials, streamlining the workflow for applying textures to
materials.

## DefineSymbolManager

## EditorGUIHelpers

The EditorGUIHelpers class is an abstract base class for creating custom Unity editor windows and inspectors with
improved styling and functionality. It provides methods for setting up various GUI styles, creating common editor
controls like buttons, sliders, and fields, and managing inspector layouts. Key features include:

- Custom Inspector Toggles: Methods for switching between default and custom inspector views.
- GUI Style Initialization: Predefined styles for labels, boxes, and buttons to ensure consistent appearance.
- Inspector Fields: Helper methods to create and manage fields for different data types, including floats, ints,
  vectors, colors, and more.
- Layout Management: Utility methods for creating spaced and boxed layouts to organize inspector elements effectively.

This class is designed to streamline the creation of custom inspectors and editor windows by encapsulating common tasks
and providing reusable components.

## Essential Importer

- [ ] Create one list with Editor Tools, one list with code tools (e.g. SeaShark)

## Extended Inspectors

## GameObjectUtilityExtensions

The GameObjectUtilityExtension class provides a utility function to remove missing scripts from selected GameObjects in
Unity. It includes:

- RemoveMissingScripts() Method: This method, accessible from the Unity editor menu under "SOSXR/DANGER/Remove Missing
  Scripts", iterates over the selected GameObjects and removes any components that are missing from their respective
  GameObjects. It logs the number of GameObjects processed.
- RemoveMissingScripts(GameObject gameObject) Method: This method handles the removal of missing scripts from a single
  GameObject. It uses SerializedObject to find and delete elements in the components array that are null.

This script is useful for cleaning up GameObjects in a scene by removing references to non-existent scripts, which can
help maintain a clean project and avoid potential errors.

## HierarchyIconDisplay (by Warped Imagination)

The HierarchyIconDisplay class customizes the Unity hierarchy window by displaying icons for game objects based on their
components.
From Warped Imagination, this script enhances the hierarchy view by:

- Displaying icons for the most significant component (excluding Transform).
- Optionally keeping icons for prefabs.
- Drawing background color based on selection and hover state.

## HierarchyScriptDropHandler (by Warped Imagination)

## Layout Switcher Tool (by Warped Imagination)

## Markdown Helper

## MarkdownViewer (a fork of [gwaredd's work](https://github.com/gwaredd/UnityMarkdownViewer))

## MissingMonoBehaviourDetector

The MissingMonoBehaviourDetector class is a Unity editor script that detects and logs GameObjects with missing
MonoBehaviours in the scene. When run, it iterates over all GameObjects in the scene and checks for any components that
are null or missing. If a GameObject contains a missing MonoBehaviour, it logs a warning message in the console,
indicating the GameObject name and the missing component type. This script helps identify and address issues related to
missing scripts in the scene, ensuring that GameObjects are properly configured and functional.

## MonoBehavior Utility

## Package

### Package Is Installed

## Readme Helpers

### ReadmeShower

The ReadmeShower class is a Unity editor script that displays a README file in the Inspector window when attached to a
GameObject. When found, it reads the contents of a specified README file and displays them in a scrollable text area
within the Inspector upon starting Unity. This script is useful for providing documentation, instructions, or additional
information directly within the Unity Editor, making it easily accessible to developers or users working on the project.

## SetIconWindow (based on Warped Imagination)

The SetIconWindow class is a Unity editor window that allows you to set custom icons for selected GameObjects in the
scene. When opened, it displays a grid of icons that can be assigned to GameObjects, allowing you to visually
differentiate objects in the scene hierarchy. The window provides a simple interface for selecting and applying icons to
GameObjects, enhancing the organization and visual representation of the scene contents. This tool is useful for
customizing the appearance of GameObjects in the Unity Editor, making it easier to identify and manage objects within
the scene. Icons need to be tagged with 'scriptIcon' to be displayed in the grid. Select all scripts you want to change
the icon of, and hit Cmd+I to open the window.

## Setup Presets (from Warped Imagination)

## ToggleUsingHierarchyIcon (by Warped Imagination)

This class adds toggling a GameObject in the Hierarchy using it's icon. When the toggle state is changed, the script
records the change using Unity's undo system and marks the scene as dirty to ensure that the change is saved. This
functionality, based on a solution from Warped Imagination, streamlines the process of managing GameObject states
without having to access their properties through the Inspector.

## Validation

To validate any class, add the `IValidate` interface and implement the `OnValidate` method to return a list of
validation errors. Errors are displayed in the console during the build process. For example:

```csharp
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

# Other things

## Leiden University Logos

## URP Quest 3 Templates

## Presets



















