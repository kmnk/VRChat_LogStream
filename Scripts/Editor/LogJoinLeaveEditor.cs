using System.Linq;
using UnityEditor;
using UnityEngine;

using Kmnk.Core;

namespace Kmnk.LogStream
{
    [CustomEditor(typeof(LogJoinLeave))]
    [CanEditMultipleObjects]
    class LogJoinLeaveEditor : EditorBase<LogJoinLeave>
    {
        SerializedProperty _idProperty;
        SerializedProperty _joinLogFormatProperty;
        SerializedProperty _leaveLogFormatProperty;

        protected override void FindProperties()
        {
            _target = target as LogJoinLeave;
            _idProperty = serializedObject.FindProperty("_id");
            _joinLogFormatProperty = serializedObject.FindProperty("_joinLogFormat");
            _leaveLogFormatProperty = serializedObject.FindProperty("_leaveLogFormat");
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
                EditorGUILayout.PropertyField(_joinLogFormatProperty);
                EditorGUILayout.PropertyField(_leaveLogFormatProperty);
            }
        }

        internal override void ApplyModifiedProperties()
        {
            FindProperties();

            var logStream = GetLogStream(_idProperty.intValue);

            if (logStream == null) { return; }

            var udon = _target.GetChildUdonBehaviour<Udon.LogJoinLeave>();
            udon.SetPublicVariable("_logStream", logStream.GetChildUdonBehaviour<Udon.LogStream>());
            udon.SetPublicVariable("_joinLogFormat", _joinLogFormatProperty.stringValue);
            udon.SetPublicVariable("_leaveLogFormat", _leaveLogFormatProperty.stringValue);
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