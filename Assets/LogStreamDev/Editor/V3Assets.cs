using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kmnk.LogStreamDev
{
    // 3.0.0 の配る prefab と sample の scene を、Unity の Editor の API で書き換える。資産ファイルをテキストで書き換えないため。
    // 動かし方: bin/lib/unity.sh の run_unity で -executeMethod Kmnk.LogStreamDev.V3Assets.Apply
    public static class V3Assets
    {
        const string Prefabs = "Assets/Kmnk/LogStream/Prefabs/";
        const string SampleScene = "Assets/Kmnk/LogStream/Samples/LogStream.unity";

        // 入れ子の LogStream.prefab は、中の prefab を先に書き換えてから開く
        static readonly string[] PrefabOrder =
        {
            "LogStreamCore", "LogStreamViewer", "LogInput", "LogJoinLeave", "LogTriggerEnterExit", "LogPomodoro", "LogStream",
        };

        public static void Apply()
        {
            try
            {
                foreach (var name in PrefabOrder)
                {
                    var path = Prefabs + name + ".prefab";
                    var root = PrefabUtility.LoadPrefabContents(path);
                    MigrateSettingsKeepingFlag(root);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    PrefabUtility.UnloadPrefabContents(root);
                    Debug.Log("[LogStreamDev] updated " + path);
                }

                var scene = EditorSceneManager.OpenScene(SampleScene, OpenSceneMode.Single);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[LogStreamDev] updated " + SampleScene);

                AssetDatabase.SaveAssets();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        // prefab の 2.x の設定の値を子の Udon に写し、_migrated は false に戻す。
        // 新しく置いたインスタンスでも移す処理が 1 度走るが、同じ値を写すだけになる
        static void MigrateSettingsKeepingFlag(GameObject root)
        {
            var migrator = Type("Kmnk.LogStream.LegacySettingsMigrator");
            var migrate = migrator.GetMethod("Migrate", BindingFlags.Public | BindingFlags.Static);
            var legacyType = Type("Kmnk.LogStream.LegacySettings");
            foreach (var legacy in root.GetComponentsInChildren(legacyType, true))
            {
                migrate.Invoke(null, new object[] { legacy });
                var so = new SerializedObject(legacy);
                so.FindProperty("_migrated").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // LogStream に asmdef が無く参照できないので、型は名前で探す
        static Type Type(string fullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(fullName))
                .First(t => t != null);
        }
    }
}
