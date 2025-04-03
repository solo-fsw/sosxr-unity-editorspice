# Changelog

All notable changes to this project will be documented in this file.
The changelog format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)

## [2.0.0] - 2025-01-31

### Changed

- Changed from GNU GPL 3 license to MIT license

## [1.0.2] - 2024-12-06

### Fixed

- Explicit call to (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var assetGuid, out long _) in
  AssetDependencyGraph
- Name of icon

### Added

- Shortcuts
    - Command Shift Alt D for What Uses This
    - Command Alt D for What Does This Use
    - Command Shift Alt G for Dependency Graph

## [1.0.1] - 2024-09-19

### Added

- Namespace SOSXR.AssetDependencyGraph for easy find-ability. Not implying SOSXR created the original package.
- Harry Rose name + github on each class to make clear who designed this beaut.
-

### Fixed

-

### Changed

- Minimum supported Unity is now 2019
- Folder structure to better match Package requirements
- Menu item to be under SOSXR/Asset Dependency Graph for easy find-ability

### Removed

-
