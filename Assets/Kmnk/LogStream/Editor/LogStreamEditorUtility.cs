using System.Linq;
using UdonSharp;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kmnk.LogStream
{
    // LogStream の Editor のコードが共有する処理
    internal static class LogStreamEditorUtility
    {
        private static GUIStyle _boxTitleStyle = null;

        // prefab の親の GameObject。設定の Udon は親の下の Udon という子にあり、音や pickup のコンポーネントも親の下にある
        public static Transform Root(Component udon)
        {
            return udon.transform.parent != null ? udon.transform.parent : udon.transform;
        }

        // scene の中から、id の一致する LogStreamCore を探す。他の scene は見ない
        public static Udon.LogStreamCore GetCore(Scene scene, int id)
        {
            if (!scene.IsValid() || !scene.isLoaded) { return null; }
            return scene.GetRootGameObjects()
                .SelectMany(o => o.GetComponentsInChildren<Udon.LogStreamCore>(true))
                .FirstOrDefault(c => c.GetId() == id);
        }

        // _id と _core を持つ Udon の _core を、同じ scene の同じ id の LogStreamCore に結ぶ
        public static void LinkCore(UdonSharpBehaviour udon)
        {
            var so = new SerializedObject(udon);
            var id = so.FindProperty("_id");
            var core = so.FindProperty("_core");
            if (id == null || core == null) { return; }
            var found = GetCore(udon.gameObject.scene, id.intValue);
            if (found == null || core.objectReferenceValue == found) { return; }
            core.objectReferenceValue = found;
            so.ApplyModifiedProperties();
        }

        public static GUIStyle BoxTitleStyle()
        {
            if (_boxTitleStyle == null)
            {
                _boxTitleStyle = new GUIStyle { fontStyle = FontStyle.Bold };
                _boxTitleStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            }
            return _boxTitleStyle;
        }
    }
}
