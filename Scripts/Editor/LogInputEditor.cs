using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.Udon;

using Kmnk.Core;

namespace Kmnk.LogStream
{
    [CustomEditor(typeof(LogInput))]
    [CanEditMultipleObjects]
    class LogInputEditor : EditorBase<LogInput>
    {
        SerializedProperty _idProperty;
        SerializedProperty _pickupableProperty;
        SerializedProperty _templateMessagesProperty;
        SerializedProperty _templateButtonsTransformProperty;
        SerializedProperty _templateButtonOriginProperty;

        protected override void FindProperties()
        {
            _target = target as LogInput;
            _idProperty = serializedObject.FindProperty("_id");
            _pickupableProperty = serializedObject.FindProperty("_pickupable");
            _templateMessagesProperty = serializedObject.FindProperty("_templateMessages");
            _templateButtonsTransformProperty = serializedObject.FindProperty("_templateButtonsTransform");
            _templateButtonOriginProperty = serializedObject.FindProperty("_templateButtonOrigin");
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
                EditorGUILayout.PropertyField(_templateMessagesProperty);
            }

            EditorGUILayout.Space();

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("System", BoxTitleStyle());
                EditorGUILayout.PropertyField(_templateButtonsTransformProperty);
                EditorGUILayout.PropertyField(_templateButtonOriginProperty);
            }
        }

        internal override void ApplyModifiedProperties()
        {
            FindProperties();

            var core = LogStreamCoreEditor.GetCore(_idProperty.intValue);

            if (core == null) { return; }

            var templateMessages = Enumerable.Range(0, _templateMessagesProperty.arraySize)
                .Select(x => _templateMessagesProperty.GetArrayElementAtIndex(x))
                .Select(x => x.stringValue)
                .ToArray();

            var pickup = _target.GetComponentInChildren<VRCPickup>();
            pickup.pickupable = _pickupableProperty.boolValue;

            var udon = _target.GetComponentInChildren<Udon.LogInput>();
            var udonSerializedObject = new SerializedObject(udon);
            udonSerializedObject.FindProperty("_core").objectReferenceValue
                = core.GetComponentInChildren<Udon.LogStreamCore>();
            udonSerializedObject.ApplyModifiedProperties();

            if (IsActiveInHierarchy() && !IsInPrefabMode())
            {
                ResetTemplateButtons(
                    templateMessages,
                    _templateButtonsTransformProperty.objectReferenceValue as Transform,
                    _templateButtonOriginProperty.objectReferenceValue as GameObject
                );
            }
        }

        private void ResetTemplateButtons(
            string[] templateMessages,
            Transform templateButtonsTransform,
            GameObject templateButtonOrigin
        )
        {
            // clean current buttons
            foreach (var t in templateButtonsTransform.GetComponentsInChildren<UdonBehaviour>(true))
            {
                if (t.gameObject == templateButtonOrigin) { continue; }
                DestroyImmediate(t.gameObject);
            }

            // create new buttons
            foreach (var templateMessage in templateMessages)
            {
                var o = Instantiate(templateButtonOrigin);
                o.SetActive(true);
                o.transform.SetParent(templateButtonsTransform);
                o.transform.localScale = Vector3.one;
                o.transform.localPosition = Vector3.zero;
                o.transform.localRotation = Quaternion.identity;

                var buttonText = o.GetComponentInChildren<Text>();
                buttonText.text = templateMessage;
            }

            // hide original button
            templateButtonOrigin.SetActive(false);
        }
    }
}