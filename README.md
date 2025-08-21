# Fast Setup
[![Unity Version](https://img.shields.io/badge/Unity-2022.3%2B-blue)](https://unity3d.com/get-unity/download)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Fast Setup for your Unity Project Folders and Packages.

## Features
- 🚀 Create new folders inside your project Assets following the structure you prefer
- 🛠️ Quickly add multiple packages at once
- ✍️ Overwrite your template packages manifest.json

## Designed to share
- 📱 Share your folder structure in an easy-to-read format
- 🍕 Share your favorite must-have packages with anyone
- 📩 Share your template packages manifest.json customized by game genre

## FastSetup add package example file
```
# openupm is added by default
# npm is added by default
# add new with format below
# registry <registryName> <url>

# add package from git repo
# git <url>
git https://github.com/dbrizov/NaughtyAttributes.git#upm
git https://github.com/mfragger/TagsAndLayersGenerator.git
git https://github.com/AnnulusGames/uPools.git?path=/Assets/uPools
git https://github.com/UnityCommunity/UnitySingleton.git

# add package from OpenUPM
openupm add jp.hadashikick.vcontainer
openupm add com.cysharp.unitask

# add package from NPM
npm i com.kyrylokuzyk.primetween
```

## FastSetup folder structure example file
```
__MyGame
  - Art
    - Animation
    - Material
    - Sprite
  - Audio
    - Music
    - Sound
  - Code
    - Editor
    - Runtime
      - _Common
      - Gameplay
      - UI
    - Shader
  - Design
    - Config
    - Prefab
    - Scene
```

## Installation
### Via Package Manager
1. Open Unity Package Manager (Window → Package Manager)
2. Click the `+` button in the top-left corner
3. Select `Add package from git URL...`
4. Enter the following URL:
   ```
   https://github.com/tungcheng/FastSetup.git
   ```

### Manual Installation

1. Download the latest release from [Releases](https://github.com/tungcheng/FastSetup/releases)
2. Extract the package to your project's `Packages` folder
3. Unity will automatically import the package

(You can customize sample files and copy-paste them when setting up a new project)

## Quick Start
### Sample
- Please check my template setup in the **"Sample"** folder. There are some good resources based on my experience. 
- You can also create your own files from these templates.

### Create Folders with predefined structure
1. Right-click on your folder structure .txt file
2. Select `FastSetup/Create Folder Structure from file`
3. Wait for the success dialog

### Import packages
1. Right-click on your packages list .txt file
2. Select `FastSetup/Import Packages from file`
3. Wait for the success dialog

### Overwrite packages
1. Right-click on your custom packages .json file
2. Select `FastSetup/Overwrite Packages manifest file`
3. Confirm that your existing packages setup will be overwritten
4. The packages manifest file will be overwritten

---
**Made with ❤️ for the Unity community**
