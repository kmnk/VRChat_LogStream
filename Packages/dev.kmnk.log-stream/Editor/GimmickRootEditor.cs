using System.Linq;
using UdonSharp;
using UnityEditor;
using UnityEngine;

namespace Kmnk.LogStream
{
    // prefab の親に置く外側のコンポーネント（GimmickRoot）の Inspector。自分の値は出さず、tips を描いた後に、子の Udon の Inspector をそのまま描く
    [CustomEditor(typeof(GimmickRoot), true)]
    [CanEditMultipleObjects]
    internal class GimmickRootEditor : Editor
    {
        private Editor _udonEditor = null;

        protected virtual void OnDisable()
        {
            if (_udonEditor != null) { DestroyImmediate(_udonEditor); }
        }

        public override void OnInspectorGUI()
        {
            var udons = ChildUdons();
            if (udons.Length == 0)
            {
                EditorGUILayout.HelpBox("設定を持つ子の Udon が見つかりません", MessageType.Warning);
                return;
            }
            EditorGUILayout.HelpBox("設定は子の Udon オブジェクトが持ちます。ここでは子の Udon の設定を表示しています", MessageType.None);
            DrawTips(udons);
            // 型を渡さないと UdonSharp の既定の Inspector が作られるので、同じ名前の CustomEditor を明示する
            var editorType = typeof(GimmickRootEditor).Assembly.GetType("Kmnk.LogStream." + target.GetType().Name + "Editor");
            CreateCachedEditor(udons, editorType, ref _udonEditor);
            _udonEditor.OnInspectorGUI();
        }

        // 子の Udon の設定の前に描く tips。udons は選んだものの子の Udon で、targets と同じ順
        protected virtual void DrawTips(UdonSharpBehaviour[] udons)
        {
        }

        // 選んだものそれぞれの子の、外側と同じ名前の UdonSharpBehaviour
        protected UdonSharpBehaviour[] ChildUdons()
        {
            return targets
                .Select(t => ((GimmickRoot)t).GetComponentsInChildren<UdonSharpBehaviour>(true)
                    .FirstOrDefault(u => u.GetType().Name == t.GetType().Name))
                .Where(u => u != null)
                .ToArray();
        }
    }
}
