using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components;

namespace Kmnk.LogStream
{
    [CustomEditor(typeof(Udon.LogStreamCore))]
    [CanEditMultipleObjects]
    internal class LogStreamCoreEditor : UdonSettingsEditor
    {
        protected override void DrawSettings()
        {
            Box("Core", () => Field("_id"));
            Box("Sound Effect", () =>
            {
                RelatedField<AudioSource>("m_audioClip", "Sound Effect Audio Clip", "ログが更新された時に流す効果音を指定します");
                Field("_soundEffectEnabled");
                RelatedField<VRCSpatialAudioSource>("Gain", "Sound Effect Gain", "ログが更新された時の効果音の音量を指定します");
                RelatedField<VRCSpatialAudioSource>("Far", "Sound Effect Far", "ログが更新された時の効果音が届く範囲を指定します");
            });
            Box("Option", () =>
            {
                Field("_timeFormat");
                Field("_logLimit");
            });
            Box("Initial Messages", () =>
            {
                Field("_initialMessages");
                Field("_initialName");
            });
        }

        // LogStreamCore は他の LogStreamCore に結ばれない
        public override void ApplyRelated()
        {
        }
    }
}
