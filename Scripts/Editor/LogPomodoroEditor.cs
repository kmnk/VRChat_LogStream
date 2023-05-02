using UnityEditor;
using UnityEngine;
using VRC.SDK3.Components;

using Kmnk.Core;

namespace Kmnk.LogStream
{
    [CustomEditor(typeof(LogPomodoro))]
    [CanEditMultipleObjects]
    class LogPomodoroEditor : EditorBase<LogPomodoro>
    {
        SerializedProperty _idProperty;
        SerializedProperty _pomodoroMinutesProperty;
        SerializedProperty _shortBreakMinutesProperty;
        SerializedProperty _longBreakMinutesProperty;
        SerializedProperty _longBreakIntervalProperty;
        SerializedProperty _autoContinueProperty;
        SerializedProperty _autoContinueIntervalSecondsProperty;
        SerializedProperty _soundEffectAudioClipProperty;
        SerializedProperty _soundEffectEnabledProperty;
        SerializedProperty _soundEffectGainProperty;
        SerializedProperty _soundEffectFarProperty;
        SerializedProperty _onlyMasterProperty;
        SerializedProperty _pomodoroNameProperty;
        SerializedProperty _breakNameProperty;
        SerializedProperty _startTimerLogProperty;
        SerializedProperty _endTimerLogProperty;
        SerializedProperty _skipTimerLogProperty;
        SerializedProperty _resetTimerLogProperty;

        protected override void FindProperties()
        {
            _target = target as LogPomodoro;
            _idProperty = serializedObject.FindProperty("_id");
            _onlyMasterProperty = serializedObject.FindProperty("_onlyMaster");
            _pomodoroMinutesProperty = serializedObject.FindProperty("_pomodoroMinutes");
            _shortBreakMinutesProperty = serializedObject.FindProperty("_shortBreakMinutes");
            _longBreakMinutesProperty = serializedObject.FindProperty("_longBreakMinutes");
            _longBreakIntervalProperty = serializedObject.FindProperty("_longBreakInterval");
            _autoContinueProperty = serializedObject.FindProperty("_autoContinue");
            _autoContinueIntervalSecondsProperty = serializedObject.FindProperty("_autoContinueIntervalSeconds");
            _soundEffectAudioClipProperty = serializedObject.FindProperty("_soundEffectAudioClip");
            _soundEffectEnabledProperty = serializedObject.FindProperty("_soundEffectEnabled");
            _soundEffectGainProperty = serializedObject.FindProperty("_soundEffectGain");
            _soundEffectFarProperty = serializedObject.FindProperty("_soundEffectFar");
            _pomodoroNameProperty = serializedObject.FindProperty("_pomodoroName");
            _breakNameProperty = serializedObject.FindProperty("_breakName");
            _startTimerLogProperty = serializedObject.FindProperty("_startTimerLog");
            _endTimerLogProperty = serializedObject.FindProperty("_endTimerLog");
            _skipTimerLogProperty = serializedObject.FindProperty("_skipTimerLog");
            _resetTimerLogProperty = serializedObject.FindProperty("_resetTimerLog");
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
                EditorGUILayout.LabelField("Pomodoro", BoxTitleStyle());
                EditorGUILayout.PropertyField(_pomodoroMinutesProperty);
                EditorGUILayout.PropertyField(_shortBreakMinutesProperty);
                EditorGUILayout.PropertyField(_longBreakMinutesProperty);
                EditorGUILayout.PropertyField(_longBreakIntervalProperty);
                EditorGUILayout.PropertyField(_autoContinueProperty);
                EditorGUILayout.PropertyField(_autoContinueIntervalSecondsProperty);
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
                EditorGUILayout.PropertyField(_onlyMasterProperty);
                EditorGUILayout.PropertyField(_pomodoroNameProperty);
                EditorGUILayout.PropertyField(_breakNameProperty);
                EditorGUILayout.PropertyField(_startTimerLogProperty);
                EditorGUILayout.PropertyField(_endTimerLogProperty);
                EditorGUILayout.PropertyField(_skipTimerLogProperty);
                EditorGUILayout.PropertyField(_resetTimerLogProperty);
            }
        }

        internal override void ApplyModifiedProperties()
        {
            FindProperties();

            var core = LogStreamCoreEditor.GetCore(_idProperty.intValue);

            if (core == null) { return; }

            var audioSource = _target.GetComponentInChildren<AudioSource>();
            audioSource.clip = _soundEffectAudioClipProperty.objectReferenceValue as AudioClip;

            var spatialAudioSource = _target.GetComponentInChildren<VRCSpatialAudioSource>();
            spatialAudioSource.Gain = _soundEffectGainProperty.floatValue;
            spatialAudioSource.Far = _soundEffectFarProperty.floatValue;

            var udon = _target.GetComponentInChildren<Udon.LogPomodoro>();
            var udonSerializedObject = new SerializedObject(udon);
            udonSerializedObject.FindProperty("_core").objectReferenceValue
                = core.GetComponentInChildren<Udon.LogStreamCore>();
            udonSerializedObject.FindProperty("_pomodoroMinutes").intValue
                = _pomodoroMinutesProperty.intValue;
            udonSerializedObject.FindProperty("_shortBreakMinutes").intValue
                = _shortBreakMinutesProperty.intValue;
            udonSerializedObject.FindProperty("_longBreakMinutes").intValue
                = _longBreakMinutesProperty.intValue;
            udonSerializedObject.FindProperty("_longBreakInterval").intValue
                = _longBreakIntervalProperty.intValue;
            udonSerializedObject.FindProperty("_autoContinue").boolValue
                = _autoContinueProperty.boolValue;
            udonSerializedObject.FindProperty("_autoContinueIntervalSeconds").intValue
                = _autoContinueIntervalSecondsProperty.intValue;
            udonSerializedObject.FindProperty("_soundEffectEnabled").boolValue
                = _soundEffectEnabledProperty.boolValue;
            udonSerializedObject.FindProperty("_onlyMaster").boolValue
                = _onlyMasterProperty.boolValue;
            udonSerializedObject.FindProperty("_pomodoroName").stringValue
                = _pomodoroNameProperty.stringValue;
            udonSerializedObject.FindProperty("_breakName").stringValue
                = _breakNameProperty.stringValue;
            udonSerializedObject.FindProperty("_startTimerLog").stringValue
                = _startTimerLogProperty.stringValue;
            udonSerializedObject.FindProperty("_endTimerLog").stringValue
                = _endTimerLogProperty.stringValue;
            udonSerializedObject.FindProperty("_skipTimerLog").stringValue
                = _skipTimerLogProperty.stringValue;
            udonSerializedObject.FindProperty("_resetTimerLog").stringValue
                = _resetTimerLogProperty.stringValue;

            udonSerializedObject.ApplyModifiedProperties();
        }
    }
}