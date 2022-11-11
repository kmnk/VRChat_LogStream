using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components;

using Kmnk.Core;

namespace Kmnk.LogStream
{
    [CustomEditor(typeof(LogStream))]
    class LogStreamEditor : EditorBase<LogStream>
    {
        SerializedProperty _idProperty;
        SerializedProperty _soundEffectAudioClipProperty;
        SerializedProperty _soundEffectEnabledProperty;
        SerializedProperty _soundEffectGainProperty;
        SerializedProperty _soundEffectFarProperty;
        SerializedProperty _timeFormatProperty;
        SerializedProperty _logLimitProperty;
        SerializedProperty _initialMessagesProperty;
        SerializedProperty _initialNameProperty;

        protected override void FindProperties()
        {
            _target = target as LogStream;
            _idProperty = serializedObject.FindProperty("_id");
            _soundEffectAudioClipProperty = serializedObject.FindProperty("_soundEffectAudioClip");
            _soundEffectEnabledProperty = serializedObject.FindProperty("_soundEffectEnabled");
            _soundEffectGainProperty = serializedObject.FindProperty("_soundEffectGain");
            _soundEffectFarProperty = serializedObject.FindProperty("_soundEffectFar");
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
                EditorGUILayout.LabelField("Sound Effect", BoxTitleStyle());
                EditorGUILayout.PropertyField(_soundEffectAudioClipProperty);
                EditorGUILayout.PropertyField(_soundEffectEnabledProperty);
                EditorGUILayout.PropertyField(_soundEffectGainProperty);
                EditorGUILayout.PropertyField(_soundEffectFarProperty);
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

            var audioSource = _target.GetComponentInChildren<AudioSource>();
            audioSource.clip = _soundEffectAudioClipProperty.objectReferenceValue as AudioClip;

            var spatialAudioSource = _target.GetComponentInChildren<VRCSpatialAudioSource>();
            spatialAudioSource.Gain = _soundEffectGainProperty.floatValue;
            spatialAudioSource.Far = _soundEffectFarProperty.floatValue;

            var udon = _target.GetChildUdonBehaviour<Udon.LogStream>();
            udon.SetPublicVariable("_soundEffectEnabled", _soundEffectEnabledProperty.boolValue);
            udon.SetPublicVariable("_timeFormat", _timeFormatProperty.stringValue);
            udon.SetPublicVariable("_logLimit", _logLimitProperty.intValue);
            udon.SetPublicVariable("_initialMessages", initialMessages);
            udon.SetPublicVariable("_initialName", _initialNameProperty.stringValue);
        }
    }
}