using UnityEditor;
using UnityEngine;

using Kmnk.Core;

namespace Kmnk.LogStream
{
    [CustomEditor(typeof(LogTriggerEnterExit))]
    [CanEditMultipleObjects]
    class LogTriggerEnterExitEditor : EditorBase<LogTriggerEnterExit>
    {
        SerializedProperty _idProperty;
        SerializedProperty _enterLogProperty;
        SerializedProperty _exitLogProperty;

        protected override void FindProperties()
        {
            _target = target as LogTriggerEnterExit;
            _idProperty = serializedObject.FindProperty("_id");
            _enterLogProperty = serializedObject.FindProperty("_enterLog");
            _exitLogProperty = serializedObject.FindProperty("_exitLog");
        }

        protected override void LayoutGUI()
        {
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Core", BoxTitleStyle());
                EditorGUILayout.PropertyField(_idProperty);
            }

            EditorGUILayout.Space();

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Option", BoxTitleStyle());
                EditorGUILayout.PropertyField(_enterLogProperty);
                EditorGUILayout.PropertyField(_exitLogProperty);
            }
        }

        internal override void ApplyModifiedProperties()
        {
            FindProperties();

            var core = LogStreamCoreEditor.GetCore(_idProperty.intValue);

            if (core == null) { return; }

            var udon = _target.GetComponentInChildren<Udon.LogTriggerEnterExit>();
            var udonSerializedObject = new SerializedObject(udon);
            udonSerializedObject.FindProperty("_core").objectReferenceValue
                = core.GetComponentInChildren<Udon.LogStreamCore>();
            udonSerializedObject.FindProperty("_enterLog").stringValue
                = _enterLogProperty.stringValue;
            udonSerializedObject.FindProperty("_exitLog").stringValue
                = _exitLogProperty.stringValue;
            udonSerializedObject.ApplyModifiedProperties();
        }

    }
}