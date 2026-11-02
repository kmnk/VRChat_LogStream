using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UdonSharpEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

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
                    if (name == "LogPomodoro") { SplitPomodoroView(root); }
                    if (name == "LogInput") { LimitInputLength(root); }
                    RenameUiEvents(root);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    PrefabUtility.UnloadPrefabContents(root);
                    Debug.Log("[LogStreamDev] updated " + path);
                }

                var scene = EditorSceneManager.OpenScene(SampleScene, OpenSceneMode.Single);
                foreach (var root in scene.GetRootGameObjects()) { RenameUiEvents(root); }
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

        // UI から SendCustomEvent で呼ぶメソッドの 2.x の名前と 3.0.0 の名前。名前を _ で始めて network から呼べなくする
        static readonly Dictionary<string, string> UiEvents = new Dictionary<string, string>
        {
            ["ActivateInput"] = "_ActivateInput",
            ["DeactivateInput"] = "_DeactivateInput",
            ["AddMessage"] = "_Submit",
            ["AppendToInput"] = "_AppendToInput",
            ["Toggle"] = "_Toggle",
            ["TogglePomodoro"] = "_TogglePomodoro",
            ["SkipPomodoro"] = "_SkipPomodoro",
            ["ResetPomodoro"] = "_ResetPomodoro",
            ["ToggleOnlyMaster"] = "_ToggleOnlyMaster",
            ["ToggleAutoContinue"] = "_ToggleAutoContinue",
        };

        // UnityEvent の SendCustomEvent の呼び出しの文字列の引数を、3.0.0 の名前に置き換える
        static void RenameUiEvents(GameObject root)
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true).Where(c => c != null))
            {
                var so = new SerializedObject(component);
                var p = so.GetIterator();
                var changed = false;
                while (p.Next(true))
                {
                    if (p.name != "m_Calls" || !p.isArray) { continue; }
                    for (var i = 0; i < p.arraySize; i++)
                    {
                        var call = p.GetArrayElementAtIndex(i);
                        if (call.FindPropertyRelative("m_MethodName").stringValue != "SendCustomEvent") { continue; }
                        var argument = call.FindPropertyRelative("m_Arguments.m_StringArgument");
                        if (UiEvents.TryGetValue(argument.stringValue, out var renamed))
                        {
                            argument.stringValue = renamed;
                            changed = true;
                        }
                    }
                }
                if (changed) { so.ApplyModifiedPropertiesWithoutUndo(); }
            }
        }

        // Pomodoro の UI の参照を、新しい View の子の LogPomodoroView に写し、LogPomodoro._view に結ぶ
        static void SplitPomodoroView(GameObject root)
        {
            var pomodoro = root.GetComponentsInChildren<Component>(true).First(c => c != null && c.GetType().FullName == "Kmnk.LogStream.Udon.LogPomodoro");
            var pomodoroSo = new SerializedObject(pomodoro);
            if (pomodoroSo.FindProperty("_view").objectReferenceValue != null) { return; }

            var viewObject = new GameObject("View");
            viewObject.transform.SetParent(root.transform, false);
            var view = UdonSharpUndo.AddComponent(viewObject, Type("Kmnk.LogStream.Udon.LogPomodoroView"));
            var viewSo = new SerializedObject(view);
            var p = viewSo.GetIterator();
            p.NextVisible(true);
            while (p.NextVisible(false))
            {
                var source = pomodoroSo.FindProperty(p.name);
                if (source != null) { viewSo.CopyFromSerializedProperty(source); }
            }
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            pomodoroSo.FindProperty("_view").objectReferenceValue = view;
            pomodoroSo.ApplyModifiedPropertiesWithoutUndo();
        }

        // 入力欄の文字数の上限を、LogStreamCore が切る長さと同じにする
        static void LimitInputLength(GameObject root)
        {
            foreach (var input in root.GetComponentsInChildren<InputField>(true))
            {
                input.characterLimit = 256;
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
