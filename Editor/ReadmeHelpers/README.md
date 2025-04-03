# Readme Helpers

- By: Maarten R. Struijk Wilbrink
- For: Leiden University SOSXR
- Fully open source: Feel free to add to, or modify, anything you see fit.

## Installation

1. Open the Unity project you want to install this package in.
2. Open the Package Manager window.
3. Click on the `+` button and select `Add package from git URL...`.
4. Paste the URL of this repo into the text field and press `Add`. Make sure it ends with `.git`.

## Reasoning

Using these tools you can have a README that's in your Unity Assets folder, but still be shown on Github as the default
README. I found this to be a much easier way to keep the README up to date, as it's always in the same place as the rest
of the Unity project.

## How to use

### Creating a README

In Unity, go to `SOSXR > Create README.md file in Assets Folder` or Right Mouse in the Project view,
`Create > SOSXR > Create README.md file in Assets Folder`. This will create a README.md file in the Assets folder from
the template. You can then edit this file to your liking.
The template is called `README_template.md` and can be found in the Resources folder.

### Showing the README on Github as the default README

Go to your Terminal and run the following command to create a symbolic link from the `.github/README.md` to the one you
just created in the Assets folder. The symlink makes sure that the actual README.md lives inside the Assets folder, but
is still always displayed on Github as the default README.

```bash
  # From your repository root
  mkdir .github
  cd .github
  ln -s ../YOUR_UNITY_PROJECT_NAME/Assets/README.md README.md 
```

> Note that 'YOUR_UNITY_PROJECT_NAME' should be replaced by the current Unity Project name, so that the entire path to
> the README.md file is correct.

### Markdown Viewer

By default Markdown files render as plain text in Unity. The excellent and Open
Source [Markdown Viewer](https://github.com/mrstruijk/Markdownviewer) can alleviate this. Follow the instructions on
that repo on how to get it into your Unity Project.

### Showing Readme on starting Unity

To show the README as soon as you're starting Unity, the `ReadmeShower.cs` script is in the Editor folder. This script
will open the README.md file in the Inspector on opening Unity. It looks for the first file called `README.md` it can
find. If you have multiple READMEs throughout your project, the one that's directly in the Asset folder (compared to
those in subfolders) will be found.