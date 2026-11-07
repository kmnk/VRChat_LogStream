using UnityEditor;

namespace Kmnk.LogStream
{
    // prefab の親に残した 2.x の設定のクラスの Inspector。開いたときに 2.x の値を子の Udon へ移す。描くものは GimmickRootEditor と同じ
    [CustomEditor(typeof(LegacySettings), true)]
    [CanEditMultipleObjects]
    internal class LegacySettingsEditor : GimmickRootEditor
    {
        private void OnEnable()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { return; }
            foreach (var t in targets)
            {
                if (LegacySettingsMigrator.Migrate((LegacySettings)t))
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(((LegacySettings)t).gameObject.scene);
                }
            }
        }
    }
}
