using System;
using UdonSharp;
using UdonSharp.Compiler;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace Kmnk.LogStreamDev
{
    // bin/compile が batchmode で呼ぶ。C# のコンパイルは Unity が開くときに済んでいるので、UdonSharp のコンパイルを行う
    public static class Verify
    {
        public static void Compile()
        {
            try
            {
                UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions { CurrentBuildTarget = BuildTarget.StandaloneWindows64 });
                var hasError = UdonSharpProgramAsset.AnyUdonSharpScriptHasError();
                Debug.Log("[LogStreamDev] UdonSharp error: " + hasError);
                EditorApplication.Exit(hasError ? 1 : 0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        // bin/create-program-asset が呼ぶ。LOGSTREAM_SCRIPT の U# スクリプトの隣に UdonSharpProgramAsset を作り、コンパイルする
        public static void CreateProgramAsset()
        {
            var scriptPath = Environment.GetEnvironmentVariable("LOGSTREAM_SCRIPT");
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            if (script == null)
            {
                Debug.LogError("[LogStreamDev] script not found: " + scriptPath);
                EditorApplication.Exit(1);
                return;
            }
            var assetPath = System.IO.Path.ChangeExtension(scriptPath, ".asset");
            if (AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(assetPath) == null)
            {
                var asset = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
                asset.sourceCsScript = script;
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[LogStreamDev] created " + assetPath);
            }
            Compile();
        }
    }
}
