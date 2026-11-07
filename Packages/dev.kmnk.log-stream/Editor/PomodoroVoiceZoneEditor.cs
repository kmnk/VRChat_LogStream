using UnityEditor;
using UnityEngine;

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
                PomodoroField();
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

        // Pomodoro の欄は、prefab の root の LogPomodoro（子の Udon でもよい）を受け付け、子の Udon の LogPomodoro を _pomodoro に入れる。
        // 欄には root を出す。Udon は外側の MonoBehaviour を参照できないので、値は子の Udon を指したままにする
        private void PomodoroField()
        {
            var property = serializedObject.FindProperty("_pomodoro");
            MarkDrawn(property.name);
            var current = property.objectReferenceValue as Component;
            var label = new GUIContent("Pomodoro", "結ぶ LogPomodoro を入れます。この Pomodoro の作業のタイマーが動いている間だけ声を変えます");
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var dropped = EditorGUILayout.ObjectField(label, current == null ? null : DisplayObject(current), typeof(GameObject), true);
            EditorGUI.showMixedValue = false;
            if (!EditorGUI.EndChangeCheck()) { return; }
            if (dropped == null)
            {
                property.objectReferenceValue = null;
                return;
            }
            var resolved = ResolvePomodoro(dropped);
            if (resolved == null)
            {
                Debug.LogWarning($"[LogStream] {dropped.name} の中に LogPomodoro が見つかりません", dropped);
                return;
            }
            property.objectReferenceValue = resolved;
        }

        // 入れられた GameObject か component から、その下の Udon の LogPomodoro を探す。無ければ null
        public static Udon.LogPomodoro ResolvePomodoro(Object dropped)
        {
            var go = dropped as GameObject;
            if (go == null && dropped is Component c) { go = c.gameObject; }
            return go == null ? null : go.GetComponentInChildren<Udon.LogPomodoro>(true);
        }

        // 欄に出すもの。外側のコンポーネントを持つ root があればそれ、無ければ Udon の GameObject
        private static GameObject DisplayObject(Component pomodoro)
        {
            var root = pomodoro.GetComponentInParent<GimmickRoot>(true);
            return root != null ? root.gameObject : pomodoro.gameObject;
        }
    }
}
