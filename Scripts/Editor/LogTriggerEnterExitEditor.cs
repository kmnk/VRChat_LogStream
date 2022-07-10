using System.Linq;
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

            var logStream = GetLogStream(_idProperty.intValue);

            if (logStream == null) { return; }

            var udon = _target.GetChildUdonBehaviour<Udon.LogTriggerEnterExit>();
            udon.SetPublicVariable("_logStream", logStream.GetChildUdonBehaviour<Udon.LogStream>());
            udon.SetPublicVariable("_enterLogFormat", _enterLogFormatProperty.stringValue);
            udon.SetPublicVariable("_exitLogFormat", _exitLogFormatProperty.stringValue);
        }

        private static LogStream GetLogStream(int id)
        {
            return Resources.FindObjectsOfTypeAll<LogStream>()
                .Where(x => AssetDatabase.GetAssetOrScenePath(x).EndsWith(".unity"))
                .Where(x => x.GetId() == id)
                .FirstOrDefault();
        }
    }
}