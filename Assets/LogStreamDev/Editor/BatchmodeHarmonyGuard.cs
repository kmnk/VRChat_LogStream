using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Kmnk.LogStreamDev
{
    // Apple silicon の Mac の batchmode では UdonSharp の Inspector のタイトルバーの Harmony の当て込みが
    // mprotect の EACCES で失敗し、エディタの更新ごとに例外を投げる。batchmode では Inspector を描かないので、当て込み済みとして扱う
    [InitializeOnLoad]
    static class BatchmodeHarmonyGuard
    {
        static BatchmodeHarmonyGuard()
        {
            if (!Application.isBatchMode) { return; }
            MarkPatched();
            AssemblyReloadEvents.afterAssemblyReload += MarkPatched;
            EditorApplication.delayCall += MarkPatched;
            EditorApplication.update += MarkPatched;
        }

        static FieldInfo _field;

        static void MarkPatched()
        {
            if (_field != null) { _field.SetValue(null, true); return; }
            var type = typeof(UdonSharpEditor.UdonSharpEditorUtility).Assembly.GetType("UdonSharpEditor.UdonSharpEditorManager");
            var field = type?.GetField("_inspectorTitlePatched", BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null) { Debug.LogWarning("[LogStreamDev] _inspectorTitlePatched not found"); return; }
            field.SetValue(null, true);
            _field = field;
        }
    }
}
