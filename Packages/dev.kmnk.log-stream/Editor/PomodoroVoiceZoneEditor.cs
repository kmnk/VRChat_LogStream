using UnityEditor;

namespace Kmnk.LogStream
{
    [CustomEditor(typeof(Udon.PomodoroVoiceZone))]
    [CanEditMultipleObjects]
    internal class PomodoroVoiceZoneEditor : UdonSettingsEditor
    {
        protected override void DrawSettings()
        {
            Box("Pomodoro", () =>
            {
                Field("_pomodoro");
                Field("_zones");
            });
            Box("Voice", () =>
            {
                Field("_muteCompletely");
                var mute = serializedObject.FindProperty("_muteCompletely");
                using (new EditorGUI.DisabledScope(mute.boolValue && !mute.hasMultipleDifferentValues))
                {
                    Field("_reducedVoiceGain");
                }
            });
        }

        // ログを書かないので、LogStreamCore を結ばない
        public override void ApplyRelated()
        {
        }
    }
}
