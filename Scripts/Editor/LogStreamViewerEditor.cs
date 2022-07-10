using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components;

using Kmnk.Core;

namespace Kmnk.LogStream
{
    [CustomEditor(typeof(LogStreamViewer))]
    [CanEditMultipleObjects]
    class LogStreamViewerEditor : EditorBase<LogStreamViewer>
    {
        SerializedProperty _idProperty;
        SerializedProperty _pickupableProperty;
        SerializedProperty _timeFormatProperty;

        protected override void FindProperties()
        {
            _target = target as LogStreamViewer;
            _idProperty = serializedObject.FindProperty("_id");
            _pickupableProperty = serializedObject.FindProperty("_pickupable");
            _timeFormatProperty = serializedObject.FindProperty("_timeFormat");
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
                EditorGUILayout.PropertyField(_pickupableProperty);
                EditorGUILayout.PropertyField(_timeFormatProperty);
            }
        }

        internal override void ApplyModifiedProperties()
        {
            FindProperties();

            var logStream = GetLogStream(_idProperty.intValue);

            if (logStream == null) { return; }

            var pickup = _target.GetComponentInChildren<VRCPickup>();
            pickup.pickupable = _pickupableProperty.boolValue;

            var udon = _target.GetChildUdonBehaviour<Udon.LogStreamViewer>();
            udon.SetPublicVariable("_logStream", logStream.GetChildUdonBehaviour<Udon.LogStream>());
            udon.SetPublicVariable("_timeFormat", _timeFormatProperty.stringValue);
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