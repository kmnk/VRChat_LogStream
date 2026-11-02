using System;
using System.Collections.Generic;
using System.Linq;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace Kmnk.LogStream
{
    // LogStream の Udon の Inspector の基底。設定を見出しごとの枠に描き、残りの参照を System の折りたたみに描く。
    // 複数を選んだ編集では、serializedObject と、別のコンポーネントの SerializedObject がどちらも全ての選択を扱う
    internal abstract class UdonSettingsEditor : Editor
    {
        private readonly HashSet<string> _drawn = new HashSet<string>();
        private bool _showSystem = false;

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(targets)) { return; }

            serializedObject.Update();
            _drawn.Clear();
            EditorGUI.BeginChangeCheck();
            DrawSettings();
            var changed = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
            if (changed) { ApplyRelated(); }

            DrawSystem();
        }

        protected abstract void DrawSettings();

        // 設定が変わった後に、他の object へ反映する。既定では Id から LogStreamCore を結ぶ
        public virtual void ApplyRelated()
        {
            foreach (var t in targets)
            {
                LogStreamEditorUtility.LinkCore((UdonSharpBehaviour)t);
            }
        }

        // 見出しの枠の中に、名前を並べた順に描く
        protected void Box(string title, Action draw)
        {
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField(title, LogStreamEditorUtility.BoxTitleStyle());
                draw();
            }
            EditorGUILayout.Space();
        }

        protected void Field(string name)
        {
            _drawn.Add(name);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(name));
        }

        // 選んだ Udon の親の下にある T の property を、全ての選択についてまとめて描く
        protected void RelatedField<T>(string propertyName, string label, string tooltip) where T : Component
        {
            var components = targets
                .Select(t => LogStreamEditorUtility.Root((Component)t).GetComponentInChildren<T>(true))
                .Where(c => c != null)
                .ToArray();
            if (components.Length == 0) { return; }
            var so = new SerializedObject(components);
            so.Update();
            EditorGUILayout.PropertyField(so.FindProperty(propertyName), new GUIContent(label, tooltip));
            so.ApplyModifiedProperties();
        }

        private void DrawSystem()
        {
            _showSystem = EditorGUILayout.Foldout(_showSystem, "System");
            if (!_showSystem) { return; }
            using (new EditorGUI.IndentLevelScope())
            {
                serializedObject.Update();
                var p = serializedObject.GetIterator();
                p.NextVisible(true);
                while (p.NextVisible(false))
                {
                    if (_drawn.Contains(p.name)) { continue; }
                    EditorGUILayout.PropertyField(p, true);
                }
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
