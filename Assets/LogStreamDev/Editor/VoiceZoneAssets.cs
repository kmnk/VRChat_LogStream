using System;
using System.Linq;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kmnk.LogStreamDev
{
    // PomodoroVoiceZone の prefab を作り、sample の scene に置いて scene の LogPomodoro に結ぶ。資産ファイルをテキストで書き換えないため。
    // 既にあるものは作り直さず、既にある prefab の親に外側のコンポーネントが無ければ足すので、何度動かしても同じ結果になる。
    // 動かし方: bash -c 'source bin/lib/unity.sh; run_unity "$root/Logs/voice-zone-assets.log" -quit -executeMethod Kmnk.LogStreamDev.VoiceZoneAssets.Apply'
    public static class VoiceZoneAssets
    {
        public const string PrefabPath = "Packages/dev.kmnk.log-stream/Prefabs/PomodoroVoiceZone.prefab";
        const string SampleScene = "Packages/dev.kmnk.log-stream/Samples/LogStream.unity";
        const string ZoneType = "Kmnk.LogStream.Udon.PomodoroVoiceZone";
        const string RootType = "Kmnk.LogStream.PomodoroVoiceZone";

        public static void Apply()
        {
            try
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                {
                    CreatePrefab();
                    Debug.Log("[LogStreamDev] created " + PrefabPath);
                }
                if (AddRoot()) { Debug.Log("[LogStreamDev] added the root component to " + PrefabPath); }
                if (AddToSample()) { Debug.Log("[LogStreamDev] updated " + SampleScene); }
                AssetDatabase.SaveAssets();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        // 親の下の子 Udon に、PomodoroVoiceZone と isTrigger の BoxCollider（10 m × 3 m × 10 m、下の面は親より 0.5 m 下）を置く。
        // 範囲が VRChat の操作の raycast を遮らないよう、Udon は Ignore Raycast の layer にする
        static void CreatePrefab()
        {
            var root = new GameObject("PomodoroVoiceZone");
            var udon = new GameObject("Udon");
            udon.transform.SetParent(root.transform, false);
            udon.layer = LayerMask.NameToLayer("Ignore Raycast");
            var box = udon.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(10f, 3f, 10f);
            box.center = new Vector3(0f, 1f, 0f);
            var zone = UdonSharpUndo.AddComponent(udon, Type(ZoneType));
            var so = new SerializedObject(zone);
            var zones = so.FindProperty("_zones");
            zones.arraySize = 1;
            zones.GetArrayElementAtIndex(0).objectReferenceValue = box;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        // 親の GameObject に外側のコンポーネントが無ければ足す。足したら true
        static bool AddRoot()
        {
            var contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var type = Type(RootType);
                if (contents.GetComponent(type) != null) { return false; }
                contents.AddComponent(type);
                PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        // sample の scene に無ければ置く。LogPomodoro の水平の位置と、spawn の高さに置く
        static bool AddToSample()
        {
            var scene = EditorSceneManager.OpenScene(SampleScene, OpenSceneMode.Single);
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true)).Where(c => c != null).ToArray();
            if (all.Any(c => c.GetType().FullName == ZoneType)) { return false; }

            var pomodoro = all.First(c => c.GetType().FullName == "Kmnk.LogStream.Udon.LogPomodoro");
            var descriptor = all.First(c => c.GetType().FullName == "VRC.SDK3.Components.VRCSceneDescriptor");
            var spawns = new SerializedObject(descriptor).FindProperty("spawns");
            var floor = spawns.arraySize > 0 && spawns.GetArrayElementAtIndex(0).objectReferenceValue is Transform spawn
                ? spawn.position.y
                : descriptor.transform.position.y;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            var pomodoroRoot = pomodoro.transform.parent != null ? pomodoro.transform.parent : pomodoro.transform;
            instance.transform.position = new Vector3(pomodoroRoot.position.x, floor, pomodoroRoot.position.z);

            var zone = instance.GetComponentsInChildren<Component>(true).First(c => c != null && c.GetType().FullName == ZoneType);
            var so = new SerializedObject(zone);
            so.FindProperty("_pomodoro").objectReferenceValue = pomodoro;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return true;
        }

        static Type Type(string fullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(fullName))
                .First(t => t != null);
        }
    }
}
