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

        protected override void FindProperties()
        {
            _target = target as LogStreamViewer;
            _idProperty = serializedObject.FindProperty("_id");
            _pickupableProperty = serializedObject.FindProperty("_pickupable");
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
            }
        }

        internal override void ApplyModifiedProperties()
        {
            FindProperties();

            var core = LogStreamCoreEditor.GetCore(_idProperty.intValue);

            if (core == null) { return; }

            var pickup = _target.GetComponentInChildren<VRCPickup>();
            pickup.pickupable = _pickupableProperty.boolValue;

            var udon = _target.GetComponentInChildren<Udon.LogStreamViewer>();
            var udonSerializedObject = new SerializedObject(udon);
            udonSerializedObject.FindProperty("_core").objectReferenceValue
                = core.GetComponentInChildren<Udon.LogStreamCore>();
            udonSerializedObject.ApplyModifiedProperties();
        }
    }
}