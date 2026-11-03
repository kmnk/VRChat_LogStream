using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.Udon;

namespace CheckInstall
{
    // bin/check-install が使い捨てのプロジェクトに写して batchmode で呼ぶ。このリポジトリのプロジェクトではコンパイルしない（Assets の外に置く）
    // 確かめた値を CHECK_INSTALL_RESULT の JSON に書く。合否は bin/check-install が JSON から決める
    public static class Probe
    {
        const string Package = "Packages/dev.kmnk.log-stream";

        public static void Run()
        {
            var r = new Dictionary<string, object>();
            try
            {
                var scenePath = Environment.GetEnvironmentVariable("CHECK_INSTALL_SCENE");
                r["legacy_folder_exists"] = Directory.Exists("Assets/Kmnk/LogStream");
                r["core_folder_exists"] = Directory.Exists("Assets/Kmnk/Core");
                r["package_folder_exists"] = Directory.Exists(Package);
                r["core_script_path"] = AssetDatabase.GUIDToAssetPath("66db0cec9761ec243b5bc3c4200417ed");

                UdonSharpProgramAsset.CompileAllCsPrograms(true, true);
                r["udonsharp_error"] = UdonSharpProgramAsset.AnyUdonSharpScriptHasError();
                r["udonsharp_programs"] = UdonSharpProgramAsset.GetAllUdonSharpPrograms()
                    .Count(p => AssetDatabase.GetAssetPath(p).StartsWith(Package + "/"));

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var all = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToArray();
                r["scene"] = scenePath;
                r["missing_scripts"] = all.Sum(g => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(g));
                r["missing_prefabs"] = scene.GetRootGameObjects().Count(g => PrefabUtility.GetPrefabInstanceStatus(g) == PrefabInstanceStatus.MissingAsset);
                r["prefab_sources"] = scene.GetRootGameObjects().Where(PrefabUtility.IsAnyPrefabInstanceRoot)
                    .Select(g => AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(g)))
                    .Where(p => p.Contains("LogStream") || p.Contains("log-stream")).ToArray();
                var udons = all.SelectMany(g => g.GetComponents<UdonBehaviour>()).ToArray();
                r["udon_behaviours"] = udons.Length;
                r["udon_without_program"] = udons.Count(u => u.programSource == null);

                // 2.x の設定のクラス（Kmnk.LogStream.<名前>）と、移した印の _migrated
                var legacy = all.SelectMany(g => g.GetComponents<MonoBehaviour>())
                    .Where(c => c != null && c.GetType().FullName.StartsWith("Kmnk.LogStream.") && !c.GetType().FullName.StartsWith("Kmnk.LogStream.Udon."))
                    .Where(c => new SerializedObject(c).FindProperty("_migrated") != null)
                    .ToArray();
                r["legacy_settings"] = legacy.Length;
                r["legacy_migrated"] = legacy.Count(l => new SerializedObject(l).FindProperty("_migrated").boolValue);
                var joinLeave = all.SelectMany(g => g.GetComponents<UdonSharpBehaviour>()).FirstOrDefault(c => c.GetType().FullName == "Kmnk.LogStream.Udon.LogJoinLeave");
                r["join_log"] = joinLeave == null ? null : new SerializedObject(joinLeave).FindProperty("_joinLog")?.stringValue;
                r["ok"] = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                r["ok"] = false;
                r["exception"] = e.GetType().Name + ": " + e.Message;
            }
            var json = ToJson(r);
            Debug.Log("[check-install] " + json);
            File.WriteAllText(Environment.GetEnvironmentVariable("CHECK_INSTALL_RESULT"), json);
            EditorApplication.Exit(0);
        }

        static string ToJson(Dictionary<string, object> d)
        {
            return "{" + string.Join(",", d.Select(kv => Quote(kv.Key) + ":" + Value(kv.Value))) + "}";
        }

        static string Value(object v)
        {
            switch (v)
            {
                case null: return "null";
                case bool b: return b ? "true" : "false";
                case int i: return i.ToString();
                case string s: return Quote(s);
                case string[] a: return "[" + string.Join(",", a.Select(Quote)) + "]";
                default: return Quote(v.ToString());
            }
        }

        static string Quote(string s)
        {
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") + "\"";
        }
    }
}
