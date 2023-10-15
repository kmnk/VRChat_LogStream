# LogStream
Simple system to stream text logs to be placed in the VRChat world.

## Required
- [UdonSharp](vrchat-community/UdonSharp)
- [kmnk/VRChat_Core](https://github.com/kmnk/VRChat_Core)

## Usage
The steps to place a board in the world project that displays logs.

0. Prepare LogStream unitypackage from Booth or github repository zip file
1. Import UdonSharp into any your Unity project
2. Import LogStream unitypackage into the same Unity project
3. Drag and drop `LogStream` prefabs under `Kmnk/LogStream/Prefabs` in the Project tab to the scene and place them wherever you want

## Preparing and using other features
- Basically, just drag and drop any prefab under `Kmnk/LogStream/Prefabs` into your scene
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
3. Check `Pickapable` if you want to make it portable

#### How to use
- Press the button with the text to add the text to the input field, and press the `SEND` button to send the message. Press `x` to cancel
- Press `Click To Input` to enter text directly and press `SEND` button to send any message

### Set up a board that displays the same log in various places
#### Preparation
1. Drag and drop the `LogStreamViewer` prefab into the scene and place it wherever you want
2. Check `Pickapable` if you want to make it portable

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
- A sample that implements the minimum functionality to stream any log you want is implemented in the `LogSample` prefab and the script `Kmnk/LogStream/Udon/LogSample.cs`

## Operation check
- I have confirmed that this works with Unity 2019.4.31f1, VRChat SDK Base 3.1.13, VRChat SDK Worlds 3.1.13, UdonSharp 1.1.7

## License
MIT License
Copyright (c) 2023 KMNK

## Updates
- 2023/10/15 v2.2.1 The button for switching Master Only and Auto Continue on the Pomodoro timer has been changed to an icon
- 2023/10/09 v2.2.0 Template messages for LogInput assets can now be read from text files
- 2023/10/08 v2.1.0 Pomodoro Timer Master Only and Auto Continue can now be toggled in-game
- 2023/05/03 v2.0.0 Refactored the base implementation so that it is no longer backward compatible. Changed log template variables from C# defaults to named ones
- 2023/04/15 v1.3.2 Master only mode is now displayed on button
- 2023/03/11 v1.3.1 Send message when resetting Pomodoro. Also, did some refactoring
- 2023/01/22 v1.3.0 Pomodoro timer function ( LogPomodoro ) added
- 2023/01/15 v1.2.0 Change to UdonSharp v1.1.0 dependent
- 2022/11/11 v1.1.0 Play sound effect on update log
- 2022/07/10 v1.0.1 Modify README
- 2022/07/10 v1.0.0 Released

## Credit
- [ICOOON MONO](https://icooon-mono.com/)
- [効果音ラボ](https://soundeffect-lab.info/)
