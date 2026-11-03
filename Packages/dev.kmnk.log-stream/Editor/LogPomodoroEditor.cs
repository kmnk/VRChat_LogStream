using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components;

namespace Kmnk.LogStream
{
    [CustomEditor(typeof(Udon.LogPomodoro))]
    [CanEditMultipleObjects]
    internal class LogPomodoroEditor : UdonSettingsEditor
    {
        protected override void DrawSettings()
        {
            Box("Core", () => Field("_id"));
            Box("Pomodoro", () =>
            {
                Field("_pomodoroMinutes");
                Field("_shortBreakMinutes");
                Field("_longBreakMinutes");
                Field("_longBreakInterval");
                Field("_autoContinue");
                Field("_autoContinueIntervalSeconds");
            });
            Box("Sound Effect", () =>
            {
                RelatedField<AudioSource>("m_audioClip", "Sound Effect Audio Clip", "タイマーが 0 になった時に流す効果音を指定します");
                Field("_soundEffectEnabled");
                RelatedField<VRCSpatialAudioSource>("Gain", "Sound Effect Gain", "タイマーが 0 になった時の効果音の音量を指定します");
                RelatedField<VRCSpatialAudioSource>("Far", "Sound Effect Far", "タイマーが 0 になった時の効果音が届く範囲を指定します");
            });
            Box("Option", () =>
            {
                Field("_onlyMaster");
                Field("_pomodoroName");
                Field("_breakName");
                Field("_startTimerLog");
                Field("_endTimerLog");
                Field("_skipTimerLog");
                Field("_resetTimerLog");
                Field("_pauseTimerLog");
                Field("_resumeTimerLog");
            });
        }
    }
}
