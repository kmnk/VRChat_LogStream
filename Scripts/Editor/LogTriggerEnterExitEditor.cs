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
        SerializedProperty _enterLogFormatProperty;
        SerializedProperty _exitLogFormatProperty;

        protected override void FindProperties()
        {
            _target = target as LogTriggerEnterExit;
            _idProperty = serializedObject.FindProperty("_id");
            _enterLogFormatProperty = serializedObject.FindProperty("_enterLogFormat");
            _exitLogFormatProperty = serializedObject.FindProperty("_exitLogFormat");
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
                EditorGUILayout.PropertyField(_enterLogFormatProperty);
                EditorGUILayout.PropertyField(_exitLogFormatProperty);
            }
        }

        internal override void ApplyModifiedProperties()
        {
            FindProperties();

            var logStream = LogStreamEditor.GetLogStream(_idProperty.intValue);

            if (logStream == null) { return; }

            var udon = _target.GetComponentInChildren<Udon.LogTriggerEnterExit>();
            var udonSerializedObject = new SerializedObject(udon);
            udonSerializedObject.FindProperty("_logStream").objectReferenceValue
                = logStream.GetComponentInChildren<Udon.LogStream>();
            udonSerializedObject.FindProperty("_enterLogFormat").stringValue
                = _enterLogFormatProperty.stringValue;
            udonSerializedObject.FindProperty("_exitLogFormat").stringValue
                = _exitLogFormatProperty.stringValue;
            udonSerializedObject.ApplyModifiedProperties();
        }

    }
}