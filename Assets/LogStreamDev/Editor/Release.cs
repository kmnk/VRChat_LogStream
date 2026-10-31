using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Kmnk.LogStreamDev
{
    // release の unitypackage を書き出す。bin/release が batchmode で呼ぶ。Windows の Editor ではメニューから呼べる
    public static class Release
    {
        // Assets/Kmnk を丸ごと渡す。LogStream と Core を別々に渡すと、親の Assets/Kmnk の項目が入らず、v2.2.3 と項目がそろわない
        static readonly string[] Roots = { "Assets/Kmnk" };

        public static void Export()
        {
            var output = Environment.GetEnvironmentVariable("LOGSTREAM_EXPORT_PATH");
            if (string.IsNullOrEmpty(output))
            {
                Debug.LogError("[LogStreamDev] LOGSTREAM_EXPORT_PATH is not set");
                EditorApplication.Exit(1);
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            AssetDatabase.ExportPackage(Roots, output, ExportPackageOptions.Recurse);
            Debug.Log("[LogStreamDev] exported " + output);
            EditorApplication.Exit(File.Exists(output) ? 0 : 1);
        }

        [MenuItem("Tools/LogStreamDev/Export Release Package")]
        static void ExportFromMenu()
        {
            var path = EditorUtility.SaveFilePanel("Export LogStream", "", "LogStream_vX_Y_Z.unitypackage", "unitypackage");
            if (string.IsNullOrEmpty(path)) { return; }
            AssetDatabase.ExportPackage(Roots, path, ExportPackageOptions.Recurse);
            EditorUtility.RevealInFinder(path);
        }
    }
}
