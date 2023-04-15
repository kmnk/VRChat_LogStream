# LogStream
Simple system for input and share some log text in VRChat World

## Required
- [UdonSharp](vrchat-community/UdonSharp)
- [kmnk/VRChat_Core](https://github.com/kmnk/VRChat_Core)

## Usage
1. Import UdonSharp.
2. Import unitypackage of LogStream.
3. Place LogStream Prefab in Kmnk/LogStream/Prefabs to the scene.
4. Place additional Prefabs (see below) in the scene according to your needs.

### LogInput
This function allows you to input template text or arbitrary text logs.
Place a prefab where you want to place it.

### LogJoinLeave
LogJoinLeave is a function to log Join/Leave anyone to/from a world.
Place the prefab somewhere in the world and it will work.

### LogStreamViewer
This is a prefab with a function to stream the same logs that are streamed to LogStream.
While only one LogStream can be placed for each Id, any number of Viewers can be placed, so if you have multiple locations where you want to display logs, place a LogStreamViewer.

### LogTriggerEnter
This function notifies when a player enters an arbitrary area.
The message of `Enter Log Format`, `Leave Log Format` will be used for message. The `{0}` is replace by the name of the player.
The `Box Collider` component attached to the `Udon` object in the prefab is used as the detection area, so you can adjust the size of the collider.

### LogPomodoro
This function executes the pomodoro timer.
The Only Master checkbox determines whether or not only the Master can operate the timer.
Check the Sound Effect Enabled checkbox to specify whether or not a sound effect is played when the timer expires.
To change the message when the Pomodoro timer starts/stops, change the respective values in the Option block.
The `{0}` and `{1}` for Start and `{0}` for End are required, so be sure to include them.

### LogSample
This is a sample Prefab that implements the minimum functionality to stream your favorite logs.
By default, on Use logs are implemented in `Kmnk/LogStream/Udon/LogSample.cs`.
I have written a brief comment (in Japanese) so that you can refer to it when you stream some text by yourself.


## Notes
- I have confirmed that this works with Unity 2019.4.31f1, VRChat SDK Base 3.1.11, VRChat SDK Worlds 3.1.11, UdonSharp 1.1.7

## License
MIT License
Copyright (c) 2022 KMNK

## Updates
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
