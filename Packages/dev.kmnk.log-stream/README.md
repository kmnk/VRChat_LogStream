# LogStream
Simple system to stream text logs to be placed in the VRChat world.

## Required
- VRChat SDK Worlds 3.10.6 or later (includes UdonSharp)
- Since v3.0.0, [kmnk/VRChat_Core](https://github.com/kmnk/VRChat_Core) is no longer used. An `Assets/Kmnk/Core` installed with v2 or earlier stays as it is for other gimmicks that use the same Core

## Install
### With VCC or ALCOM (recommended)
1. Open [Add to VCC](vcc://vpm/addRepo?url=https://kmnk.github.io/vpm-repos/index.json), or add `https://kmnk.github.io/vpm-repos/index.json` under Packages in the VCC or ALCOM settings
2. Add `LogStream` in Manage Project of your world project

You can add it to a project that has v2 or earlier from a unitypackage. The old `Assets/Kmnk/LogStream` is removed automatically with everything in it, and prefabs in your scenes keep working with the new version. If you keep your own scenes or files in `Assets/Kmnk/LogStream`, move them elsewhere under `Assets` before adding the package.

### With the unitypackage
1. Get the LogStream unitypackage from [GitHub Releases](https://github.com/kmnk/VRChat_LogStream/releases) or Booth
2. In a project that has v2 or earlier, delete `Assets/Kmnk/LogStream` in the Project tab first. Importing over it breaks script compilation (you can keep `Assets/Kmnk/Core`)
3. Import the unitypackage

## Usage
Drag and drop prefabs from `Packages/LogStream/Prefabs` into your scene and they work. You need one `LogStream` to display logs. Hover the cursor over a label in the Inspector to see what each setting does.

| Prefab | What it does | What to do after placing it |
| --- | --- | --- |
| `LogStream` | The board that displays logs | Nothing |
| `LogJoinLeave` | Streams Join/Leave of the instance to the log | Nothing. It can be anywhere |
| `LogInput` | Streams template buttons or typed text to the log | To change the templates, edit the text file in `Template Text` and check `Use Template Text` |
| `LogTriggerEnterExit` | Streams people entering and leaving an area to the log | Set the area with `Center` and `Size` of the `Box Collider` on the child `Udon` |
| `LogPomodoro` | Pomodoro timer | Nothing |
| `LogStreamViewer` | Displays the same log in another place | Nothing |
| `PomodoroVoiceZone` | While the Pomodoro timer is in a work period, makes people in an area unable to hear each other (does not stream logs) | Put `LogPomodoro` in the `Pomodoro` field, and set the area with `Center` and `Size` of the `Box Collider` |

- The settings of each prefab are held by its child `Udon` object. Selecting the parent object also shows the child's settings
- On the Pomodoro timer, the middle button starts and pauses, the left button resets, and the right button skips to the next. The lock button toggles whether only the instance master can operate it
- Check `Pickupable` on `LogInput` or `LogStreamViewer` to make it pickupable

### Mute voices during Pomodoro work
While the `LogPomodoro` timer is running a work period (not paused, on a break, or before it starts), `PomodoroVoiceZone` makes people inside the area unable to hear each other, and people inside and outside unable to hear each other. People who are both outside still hear each other. When the work period stops or someone leaves the area, voices go back to how they were.

- The area is checked at the feet. Extend the bottom of the `Box Collider` slightly below the floor. Add colliders to `Zones` to make the area the union of them (non-convex Mesh Colliders do not work)
- Uncheck `Mute Completely` to lower voices to `Reduced Voice Gain` instead of muting them
- Only voices change. Avatar audio is not changed
- It does not work correctly together with other gimmicks that change how voices are heard (UdonVoiceUtils and others)
- ClientSim cannot play voices. Check it with two VRChat clients (for example, two players in Build & Test)

### Place several boards in one world
1. Place as many `LogStream` as you need
2. In the `LogStreamCore` inside each `LogStream`, set `Id` in the `Core` box to a different number
3. Set `Id` on the other prefabs to the `Id` of the board they should stream to

Two `LogStreamCore` with the same `Id` do not work correctly.

### Stream your own logs
The `LogSample` prefab and `Packages/LogStream/Runtime/LogSample.cs` are a minimal example of streaming any log you want. Files in the package are replaced on update, so copy it under `Assets` before you edit it.

## Operation check
- I have confirmed that this works with Unity 2022.3.22f1, VRChat SDK Base 3.10.6, VRChat SDK Worlds 3.10.6

## License
MIT License
Copyright (c) 2026 KMNK

## Updates
See [CHANGELOG.md](CHANGELOG.md).

## Credit
- [ICOOON MONO](https://icooon-mono.com/)
- [効果音ラボ](https://soundeffect-lab.info/)
