using System;
using UnityEditor;
using UnityEngine;

namespace Kmnk.LogStreamDev
{
    // LOGSTREAM_SETTLE=1 のとき、SDK がエディタの更新で行う設定の書き換えが落ち着くまで待ち、保存して終える
    [InitializeOnLoad]
    static class Settle
    {
        static int _quiet;
        static Settle()
        {
            if (Environment.GetEnvironmentVariable("LOGSTREAM_SETTLE") != "1") { return; }
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { _quiet = 0; return; }
            if (++_quiet < 600) { return; }
            EditorApplication.update -= Tick;
            AssetDatabase.SaveAssets();
            Debug.Log("[LogStreamDev] settled. defines=" + PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Standalone));
            EditorApplication.Exit(0);
        }
    }
}
