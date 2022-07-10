using System.Linq;
using UnityEditor;
using UnityEngine;

using Kmnk.Core;

namespace Kmnk.LogStream
{
    [CustomEditor(typeof(LogStream))]
    class LogStreamEditor : EditorBase<LogStream>
    {
        SerializedProperty _idProperty;
        SerializedProperty _timeFormatProperty;
        SerializedProperty _logLimitProperty;
        SerializedProperty _initialMessagesProperty;
        SerializedProperty _initialNameProperty;

        protected override void FindProperties()
        {
            _target = target as LogStream;
            _idProperty = serializedObject.FindProperty("_id");
            _timeFormatProperty = serializedObject.FindProperty("_timeFormat");
            _logLimitProperty = serializedObject.FindProperty("_logLimit");
            _initialMessagesProperty = serializedObject.FindProperty("_initialMessages");
            _initialNameProperty = serializedObject.FindProperty("_initialName");
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
                EditorGUILayout.PropertyField(_timeFormatProperty);
                EditorGUILayout.PropertyField(_logLimitProperty);
            }

            EditorGUILayout.Space();

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Initial Messages", BoxTitleStyle());
                EditorGUILayout.PropertyField(_initialMessagesProperty);
                EditorGUILayout.PropertyField(_initialNameProperty);
            }
        }

        internal override void ApplyModifiedProperties()
        {
            FindProperties();

            var initialMessages = Enumerable.Range(0, _initialMessagesProperty.arraySize)
                .Select(x => _initialMessagesProperty.GetArrayElementAtIndex(x))
                .Select(x => x.stringValue)
                .ToArray();

            var udon = _target.GetChildUdonBehaviour<Udon.LogStream>();
            udon.SetPublicVariable("_timeFormat", _timeFormatProperty.stringValue);
            udon.SetPublicVariable("_logLimit", _logLimitProperty.intValue);
            udon.SetPublicVariable("_initialMessages", initialMessages);
            udon.SetPublicVariable("_initialName", _initialNameProperty.stringValue);
        }
    }
}