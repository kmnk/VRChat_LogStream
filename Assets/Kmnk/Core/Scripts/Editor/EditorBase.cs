using UnityEditor;
using UnityEngine;

namespace Kmnk.Core
{
    abstract class EditorBase<T> : Editor where T : MonoBehaviour
    {
        protected T _target;

        protected bool _hasOnceApplied = false;

        public override void OnInspectorGUI()
        {
            FindProperties();

            serializedObject.Update();

            LayoutGUI();

            if (!serializedObject.ApplyModifiedProperties() && _hasOnceApplied) { return; }

            ApplyModifiedProperties();

            if (!_hasOnceApplied) { _hasOnceApplied = true; }
        }

        protected abstract void FindProperties();

        protected abstract void LayoutGUI();

        internal abstract void ApplyModifiedProperties();

        protected bool IsInPrefabMode()
        {
            return UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null;
        }

        protected bool IsActiveInHierarchy()
        {
            if (_target == null) { return false; }
            return _target.gameObject.activeInHierarchy;
        }

        static GUIStyle _boxTitleStyle = null;
        internal static GUIStyle BoxTitleStyle()
        {
            if (_boxTitleStyle == null)
            {
                _boxTitleStyle = new GUIStyle()
                {
                    fontStyle = FontStyle.Bold,
                };
                _boxTitleStyle.normal.textColor = Color.white;
            }
            return _boxTitleStyle;
        }
    }
}