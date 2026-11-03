# LogStream
Simple system to stream text logs to be placed in the VRChat world.

## Required
- VRChat SDK Worlds 3.10.6 or later (includes UdonSharp)
- Since v3.0.0, [kmnk/VRChat_Core](https://github.com/kmnk/VRChat_Core) is no longer used. An `Assets/Kmnk/Core` installed with v2 or earlier stays as it is for other gimmicks that use the same Core

## Install
### With VCC or ALCOM (recommended)
1. Open [Add to VCC](vcc://vpm/addRepo?url=https://kmnk.github.io/vpm-repos/index.json), or add `https://kmnk.github.io/vpm-repos/index.json` under Packages in the VCC or ALCOM settings
2. Add `LogStream` in Manage Project of your world project

You can add it to a project that has v2 or earlier from a unitypackage. The old `Assets/Kmnk/LogStream` is removed automatically, and prefabs in your scenes keep working with the new version.

### With the unitypackage
1. Get the LogStream unitypackage from [GitHub Releases](https://github.com/kmnk/VRChat_LogStream/releases) or Booth
2. In a project that has v2 or earlier, delete `Assets/Kmnk/LogStream` in the Project tab first. Importing over it breaks script compilation (you can keep `Assets/Kmnk/Core`)
3. Import the unitypackage

## Usage
The steps to place a board in the world project that displays logs.

1. Install LogStream as above
2. Drag and drop the `LogStream` prefab under `Packages/LogStream/Prefabs` in the Project tab to the scene and place it wherever you want

## Preparing and using other features
- Basically, just drag and drop any prefab under `Packages/LogStream/Prefabs` into your scene
- For a detailed explanation of the adjustment fields, hover the cursor over each value label on the `Inspector` tab and tips will pop up

### Log Join/Leave to world instances
#### Preparation
1. Drag and drop the `LogJoinLeave` prefab into the scene (anywhere is fine)
2. Select the `LogJoinLeave` object and change the `Join Log` and `Leave Log` if you want to change the message
    - The `{{name}}` will be the name of the person who joined/leave

#### Usage
- It works automatically when you put it

### Send template messages and input messages to the log
#### Preparation
1. Drag and drop the `LogInput` prefab into your scene and place it wherever you want
2. If you want to change or add a template message, select the `LogInput` object, edit the text file set to `Template Text`, and check the `Use Template Text` checkbox
3. Check `Pickupable` if you want to make it portable

#### How to use
- Press the button with the text to add the text to the input field, and press the `SEND` button to send the message. Press `x` to cancel
- Press `Click To Input` to enter text directly and press `SEND` button to send any message

### Set up a board that displays the same log in various places
#### Preparation
1. Drag and drop the `LogStreamViewer` prefab into the scene and place it wherever you want
2. Check `Pickupable` if you want to make it portable

#### How to use
- Place it and it will work automatically

### Log people going in and out of a specified location
#### Preparation
1. Drag and drop the `LogTriggerEnterExit` prefab into the scene and place it anywhere you like
2. Select the `Udon` object in the `LogTriggerEnterExit` object you placed
3. Adjust the `Center` and `Size` of the `Box Collider` component that appears in the `Inspector` tab, and set the location where you want the incoming and outgoing messages to be logged
4. If you want to change the message, select the `LogTriggerEnterExit` object and change the `Enter Log` and `Exit Log`
    - The `{{name}}` is the name of the person who joined/leaved.

#### Usage
- Logs are automatically send when someone enters or exits the location specified in `3` area

### Using Pomodoro Timer
#### Preparation
1. Drag and drop the `LogPomodoro` prefab into the scene and place it anywhere you like
2. Select the `LogPomodoro` object and adjust the Pomodoro timer settings (in the `Pomodoro` field) to your liking
3. If you want to change the message, select the `LogPomodoro` object and adjust the value in the `Option` field
4. If you want to change the sound when the timer ends, select the `LogPomodoro` object and adjust the value in the `Sound Effect` field

#### How to use
- Press the middle button to start/pause the timer
- Press the left button to reset the timer and start from the beginning
- Press the right button to skip the current timer and move to the next state

### Set up a board that displays multiple types of logs in one world
1. Drag and drop `LogStream` prefabs into multiple scenes and place them wherever you want
2. Select `LogStreamCore` in the `LogStream` object and change the `Id` number in the `Core` column for each log you want to separate
    - Other prefabs have the same `Id` field, so set the same `Id` for each group
    - You cannot have more than one `LogStreamCore` with the same `Id`. Please note that this will not work as intended

### Others
- A sample that implements the minimum functionality to stream any log you want is implemented in the `LogSample` prefab and the script `Packages/LogStream/Runtime/LogSample.cs`. Files in the package are replaced on update, so copy it under `Assets` before you edit it
- The settings of each prefab are held by its child `Udon` object. Selecting the parent object shows the settings of the child `Udon` object
- `Only Master` of the Pomodoro timer lets only the instance master operate the timer. It can be toggled in-game with the lock button

## Operation check
- I have confirmed that this works with Unity 2022.3.22f1, VRChat SDK Base 3.10.6, VRChat SDK Worlds 3.10.6

## License
MIT License
Copyright (c) 2024 KMNK

## Updates
See [CHANGELOG.md](CHANGELOG.md).

## Credit
- [ICOOON MONO](https://icooon-mono.com/)
- [効果音ラボ](https://soundeffect-lab.info/)
