# Changelog

All notable changes to this project will be documented in this file.
The changelog format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)

## [0.3.2] In Progress
## Fixed
- Path in demo video 


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

### Removed

- IValidate
- Moved Attributes to [SeaShark](https://github.com/solo-fsw/sosxr-unity-seashark)
- Moved DrawGizmo to [SeaShark](https://github.com/solo-fsw/sosxr-unity-seashark)

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

