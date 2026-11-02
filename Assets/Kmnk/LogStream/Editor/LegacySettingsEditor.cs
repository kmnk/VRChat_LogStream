using System.Linq;
using UdonSharp;
using UnityEditor;
using UnityEngine;

namespace Kmnk.LogStream
{
    // prefab の親に残した 2.x の設定のクラスの Inspector。自分の値は出さず、2.x の値を移した後、子の Udon の Inspector をそのまま描く
    [CustomEditor(typeof(LegacySettings), true)]
    [CanEditMultipleObjects]
    internal class LegacySettingsEditor : Editor
    {
        private Editor _udonEditor = null;

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

        private void OnDisable()
        {
            if (_udonEditor != null) { DestroyImmediate(_udonEditor); }
        }

        public override void OnInspectorGUI()
        {
            var udons = targets
                .Select(t => ((LegacySettings)t).GetComponentsInChildren<UdonSharpBehaviour>(true)
                    .FirstOrDefault(u => u.GetType().Name == t.GetType().Name))
                .Where(u => u != null)
                .Cast<Object>()
                .ToArray();
            if (udons.Length == 0)
            {
                EditorGUILayout.HelpBox("設定を持つ子の Udon が見つかりません", MessageType.Warning);
                return;
            }
            EditorGUILayout.HelpBox("設定は子の Udon オブジェクトが持ちます。ここでは子の Udon の設定を表示しています", MessageType.None);
            // 型を渡さないと UdonSharp の既定の Inspector が作られるので、同じ名前の CustomEditor を明示する
            var editorType = typeof(LegacySettingsEditor).Assembly.GetType("Kmnk.LogStream." + target.GetType().Name + "Editor");
            CreateCachedEditor(udons, editorType, ref _udonEditor);
            _udonEditor.OnInspectorGUI();
        }
    }
}
