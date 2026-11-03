using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.Udon;

namespace Kmnk.LogStream
{
    [CustomEditor(typeof(Udon.LogInput))]
    [CanEditMultipleObjects]
    internal class LogInputEditor : UdonSettingsEditor
    {
        protected override void DrawSettings()
        {
            Box("Core", () => Field("_id"));
            Box("Option", () =>
            {
                RelatedField<VRCPickup>("pickupable", "Pickupable", "ボードのピックアップ ON/OFF を切り替えます");
                Field("_templateMessages");
                Field("_useTemplateText");
                Field("_templateText");
            });
            Box("Template Buttons", () =>
            {
                Field("_templateButtonsTransform");
                Field("_templateButtonOrigin");
            });
        }

        public override void ApplyRelated()
        {
            base.ApplyRelated();
            foreach (var t in targets)
            {
                RebuildTemplateButtons((Udon.LogInput)t, true);
            }
        }

        // テンプレートのメッセージの数だけ、元のボタンを複製して並べ直す。prefab の編集中と、無効な object では行わない
        public static void RebuildTemplateButtons(Udon.LogInput input, bool recordUndo)
        {
            if (!input.gameObject.activeInHierarchy) { return; }
            if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null) { return; }

            var so = new SerializedObject(input);
            var parent = so.FindProperty("_templateButtonsTransform").objectReferenceValue as Transform;
            var origin = so.FindProperty("_templateButtonOrigin").objectReferenceValue as GameObject;
            if (parent == null || origin == null) { return; }

            foreach (var button in parent.GetComponentsInChildren<UdonBehaviour>(true)
                         .Select(u => u.gameObject)
                         .Where(o => o != origin)
                         .Distinct()
                         .ToArray())
            {
                if (recordUndo) { Undo.DestroyObjectImmediate(button); }
                else { DestroyImmediate(button); }
            }

            foreach (var message in TemplateMessages(so))
            {
                var button = Instantiate(origin, parent);
                button.name = origin.name;
                button.SetActive(true);
                button.transform.localScale = Vector3.one;
                button.transform.localPosition = Vector3.zero;
                button.transform.localRotation = Quaternion.identity;
                button.GetComponentInChildren<Text>(true).text = message;
                if (recordUndo) { Undo.RegisterCreatedObjectUndo(button, "Rebuild template buttons"); }
            }

            origin.SetActive(false);
        }

        private static string[] TemplateMessages(SerializedObject so)
        {
            var text = so.FindProperty("_templateText").objectReferenceValue as TextAsset;
            if (so.FindProperty("_useTemplateText").boolValue && text != null)
            {
                return text.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            }
            var messages = so.FindProperty("_templateMessages");
            return Enumerable.Range(0, messages.arraySize)
                .Select(i => messages.GetArrayElementAtIndex(i).stringValue)
                .ToArray();
        }
    }
}
