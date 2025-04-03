# Asset Dependency

This repo is a combination of two excellent tools for Unity, [What Uses This](https://github.com/Facepunch/WhatUsesThis)
and [Asset Dependency Graph](https://github.com/Unity-Harry/Unity-AssetDependencyGraph). The goal is to combine the best
features of both tools into a single package.

## Install instructions

### Method 1

1. Open the Unity project you want to install this package in.
2. Open the Package Manager window.
3. Click on the `+` button and select `Add package from git URL...`.
4. Paste the URL of this repo into the text field and press `Add`. Make sure it ends with `.git`.

## Usage of Asset Dependency Graph

The Asset Dependency Graph Window can be opened via the `SOSXR > Asset Dependency > Asset Dependency Graph` file menu.

Once the window is open:

1. Select the root asset you want to inspect in the Project window
2. Click the `Explore Asset` button in the graph window

## Usage of What Uses This

The What Uses This Window can be opened via the `SOSXR > Asset Dependency > What Uses This` file menu after you have
selected an asset in the Project window. You can also check what the asset is using by going to
`SOSXR > Asset Dependency > What Does This Use`

You'll have to periodically update the dependency cache, you can do this by going to
`SOSXR > Asset Dependency > Rebuild Dependency Cache`.

