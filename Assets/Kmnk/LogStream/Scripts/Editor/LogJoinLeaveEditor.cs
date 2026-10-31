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
        SerializedProperty _joinLogProperty;
        SerializedProperty _leaveLogProperty;

        protected override void FindProperties()
        {
            _target = target as LogJoinLeave;
            _idProperty = serializedObject.FindProperty("_id");
            _joinLogProperty = serializedObject.FindProperty("_joinLog");
            _leaveLogProperty = serializedObject.FindProperty("_leaveLog");
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
                EditorGUILayout.PropertyField(_joinLogProperty);
                EditorGUILayout.PropertyField(_leaveLogProperty);
            }
        }

        internal override void ApplyModifiedProperties()
        {
            FindProperties();

            var core = LogStreamCoreEditor.GetCore(_idProperty.intValue);

            if (core == null) { return; }

            var udon = _target.GetComponentInChildren<Udon.LogJoinLeave>();
            var udonSerializedObject = new SerializedObject(udon);
            udonSerializedObject.FindProperty("_core").objectReferenceValue
                = core.GetComponentInChildren<Udon.LogStreamCore>();
            udonSerializedObject.FindProperty("_joinLog").stringValue
                = _joinLogProperty.stringValue;
            udonSerializedObject.FindProperty("_leaveLog").stringValue
                = _leaveLogProperty.stringValue;
            udonSerializedObject.ApplyModifiedProperties();
        }
    }
}