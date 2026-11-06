# Changelog

All notable changes to LogStream are recorded here, newest first. The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/). Changes that are not released yet are under `[Unreleased]`.

## [Unreleased]

### Added

- Added the `PomodoroVoiceZone` prefab, which mutes (or lowers) the voices of people in an area while the Pomodoro timer is in a work period

## [3.0.0] - 2026-11-03

### Before you update

- Rebuilt for UdonSharp (VRChat SDK Worlds 3.10.6). This version has breaking changes
- LogStream is now a VPM package (`dev.kmnk.log-stream`) that you can add from VCC or ALCOM. It is placed in `Packages/dev.kmnk.log-stream`. When installed from VCC or ALCOM, the v2 folder `Assets/Kmnk/LogStream` is removed automatically, and prefabs in your scenes keep working with the new version
- If you use the unitypackage, delete `Assets/Kmnk/LogStream` before importing it. Importing over the old folder breaks script compilation
- `Assets/Kmnk/LogStream` is removed with everything in it. If you keep your own scenes or files there, move them elsewhere under `Assets` before installing

### Only if it applies to you

- Master Only of the Pomodoro timer now means only the instance master can operate it (in v2, the player who locked it could)
- Event names called from buttons now start with `_` (`_ActivateInput`, `_DeactivateInput`, `_Submit`, `_AppendToInput`, `_Toggle`, `_TogglePomodoro`, `_SkipPomodoro`, `_ResetPomodoro`, `_ToggleOnlyMaster`, `_ToggleAutoContinue`). If you unpacked the prefabs or call these events from your own UI, update the event names
- A log message is limited to 256 characters, and the log limit to 200. Pomodoro minutes and the long break interval are at least 1

### Other changes

- Settings are now held by the child `Udon` object of each prefab. Settings in v2 scenes are moved automatically when the scene is opened or built. You can still select the parent object to edit them
- Only the owner of `LogStreamCore` writes logs, and other players send their logs to the owner. Logs are no longer lost when several players post at the same time
- Log times and the Pomodoro timer use the network time instead of each device's clock
- `Time Format` of `LogStreamCore` now takes effect
- The bundled `Kmnk/Core` is no longer included. An existing `Assets/Kmnk/Core` is not removed
- The sample scene no longer contains a world ID. Uploading the sample creates a new world of your own

## [2.2.3] - 2024-06-14

- Add Pomodoro Pause and Resume message

## [2.2.2] - 2024-01-08

- Apply Unity and SDK new version changes

## [2.2.1] - 2023-10-15

- The button for switching Master Only and Auto Continue on the Pomodoro timer has been changed to an icon

## [2.2.0] - 2023-10-09

- Template messages for LogInput assets can now be read from text files

## [2.1.0] - 2023-10-08

- Pomodoro Timer Master Only and Auto Continue can now be toggled in-game

## [2.0.0] - 2023-05-03

- Refactored the base implementation so that it is no longer backward compatible. Changed log template variables from C# defaults to named ones

## [1.3.2] - 2023-04-15

- Master only mode is now displayed on button

## [1.3.1] - 2023-03-11

- Send message when resetting Pomodoro. Also, did some refactoring

## [1.3.0] - 2023-01-22

- Pomodoro timer function ( LogPomodoro ) added

## [1.2.0] - 2023-01-15

- Change to UdonSharp v1.1.0 dependent

## [1.1.0] - 2022-11-11

- Play sound effect on update log

## [1.0.1] - 2022-07-10

- Modify README

## [1.0.0] - 2022-07-10

- Released
