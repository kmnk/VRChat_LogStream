using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VRC.SDK3.ClientSim;
using VRC.SDKBase;
using VRC.Udon;

namespace Kmnk.LogStreamDev.Tests
{
    // LogStream に asmdef が無く型を参照できないので、型は名前で探し、Udon の値は UdonBehaviour の変数として読む。
    // 確かめる振る舞いとテストの対応は docs/development.md にある
    public class LogStreamTests
    {
        const string TempDir = "Assets/LogStreamDev/Temp";
        const string Prefabs = "Assets/Kmnk/LogStream/Prefabs/";
        const string TestWriter = "Assets/LogStreamDev/TestUdon/LogStreamTestWriter.asset";

        // LogType の値（Udon/LogType.cs）
        const int Message = 1;
        const int Notification = 2;

        static bool _playModeOptionsChanged;
        static bool _prevOptionsEnabled;
        static EnterPlayModeOptions _prevOptions;

        // テストが失敗して途中で抜けても、Play Mode を出て、ClientSim と Play Mode の設定を戻し、一時的な scene を消す
        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            if (Application.isPlaying) { yield return new ExitPlayMode(); }
            if (_playModeOptionsChanged)
            {
                ClientSimRuntimeLoader.EndUnityTesting();
                EditorSettings.enterPlayModeOptionsEnabled = _prevOptionsEnabled;
                EditorSettings.enterPlayModeOptions = _prevOptions;
                _playModeOptionsChanged = false;
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(TempDir);
        }

        // Editor の層: ビルドのときに CustomEditor が親の Inspector の値を子の UdonSharp のコンポーネントへ写す
        [Test]
        public void BuildProcessor_CopiesInspectorValuesToUdon()
        {
            GameObject core = null, joinLeave = null;
            var scene = NewSavedScene("EditorLayer", () =>
            {
                core = Instantiate(Prefabs + "LogStreamCore.prefab");
                var coreSettings = FindByTypeName(core, "Kmnk.LogStream.LogStreamCore");
                // prefab の既定は ON なので、OFF にして写ることを見る
                SetProp(coreSettings, "_soundEffectEnabled", p => p.boolValue = false);
                SetProp(coreSettings, "_soundEffectGain", p => p.floatValue = 3f);
                joinLeave = Instantiate(Prefabs + "LogJoinLeave.prefab");
                SetProp(FindByTypeName(joinLeave, "Kmnk.LogStream.LogJoinLeave"), "_joinLog", p => p.stringValue = "TEST JOIN {{name}}");
            });

            RunBuildProcessor(scene);

            var coreUdon = new SerializedObject(FindByTypeName(core, "Kmnk.LogStream.Udon.LogStreamCore"));
            Assert.IsFalse(coreUdon.FindProperty("_soundEffectEnabled").boolValue, "音の ON/OFF が写らない");
            Assert.AreEqual(3f, core.GetComponentInChildren<VRC.SDK3.Components.VRCSpatialAudioSource>().Gain, "音量が写らない");

            var joinLeaveUdon = new SerializedObject(FindByTypeName(joinLeave, "Kmnk.LogStream.Udon.LogJoinLeave"));
            Assert.AreEqual("TEST JOIN {{name}}", joinLeaveUdon.FindProperty("_joinLog").stringValue);
            Assert.AreSame(FindByTypeName(core, "Kmnk.LogStream.Udon.LogStreamCore"), joinLeaveUdon.FindProperty("_core").objectReferenceValue,
                "Id で LogStreamCore に結ばれない");
        }

        // 実行の層: ログを足すと配列の先頭に入り、古いものは後ろへずれ、_logLimit を超えると落ちる
        [UnityTest]
        public IEnumerator AddMessage_ShiftsAndDropsOldest()
        {
            var scene = NewSavedScene("PlayLayer", () =>
            {
                AddWorldAndCore(logLimit: 3);
                AddTestWriter();
            });
            return InPlayMode(scene, () =>
            {
                var core = FindUdon("LogStreamCore");
                foreach (var m in new[] { "m1", "m2", "m3", "m4" }) { Write(core, Message, m); }
                CollectionAssert.AreEqual(new[] { "m4", "m3", "m2" }, (string[])core.GetProgramVariable("_messages"));
            });
        }

        // 実行の層: Viewer は選んだ LogType のログだけを描き、None ではすべてを描く
        [UnityTest]
        public IEnumerator Viewer_FiltersByLogType()
        {
            var scene = NewSavedScene("ViewerLayer", () =>
            {
                AddWorldAndCore(logLimit: 10);
                Instantiate(Prefabs + "LogStreamViewer.prefab");
                AddTestWriter();
            });
            return InPlayMode(scene, () =>
            {
                var core = FindUdon("LogStreamCore");
                Write(core, Message, "m1");
                Write(core, Notification, "n1");
                Write(core, Message, "m2");

                var viewer = FindUdon("LogStreamViewer");
                SetType(viewer, Notification);
                CollectionAssert.AreEqual(new[] { "n1", "", "" }, DisplayedMessages(viewer, 3));
                SetType(viewer, Message);
                CollectionAssert.AreEqual(new[] { "m2", "m1", "" }, DisplayedMessages(viewer, 3));
                SetType(viewer, 0);
                CollectionAssert.AreEqual(new[] { "m2", "n1", "m1" }, DisplayedMessages(viewer, 3));
            });
        }

        static void AddWorldAndCore(int logLimit)
        {
            Instantiate("Packages/com.vrchat.worlds/Samples/UdonExampleScene/Prefabs/VRCWorld.prefab");
            var core = Instantiate(Prefabs + "LogStreamCore.prefab");
            SetProp(FindByTypeName(core, "Kmnk.LogStream.LogStreamCore"), "_logLimit", p => p.intValue = logLimit);
        }

        static void AddTestWriter()
        {
            var writer = new GameObject("TestWriter");
            writer.AddComponent<UdonBehaviour>().programSource = AssetDatabase.LoadAssetAtPath<AbstractUdonProgramSource>(TestWriter);
        }

        static void Write(UdonBehaviour core, int type, string message)
        {
            var writer = FindUdon("LogStreamTestWriter");
            writer.SetProgramVariable("core", core);
            writer.SetProgramVariable("type", type);
            writer.SetProgramVariable("message", message);
            writer.SetProgramVariable("playerName", "tester");
            writer.SendCustomEvent("Write");
        }

        // ChangeType は引数を取るので、絞り込みの変数を書いてから引数なしの描画の event を呼ぶ
        static void SetType(UdonBehaviour viewer, int type)
        {
            var t = viewer.GetProgramVariableType("_currentType");
            viewer.SetProgramVariable("_currentType", t.IsEnum ? Enum.ToObject(t, type) : Convert.ChangeType(type, t));
            viewer.SendCustomEvent("OnDisplayAllLogLines");
        }

        static string[] DisplayedMessages(UdonBehaviour viewer, int count) =>
            viewer.GetComponentsInChildren<UdonBehaviour>(true)
                .Where(u => u.programSource != null && u.programSource.name == "LogLine")
                .Take(count)
                .Select(u => ((Text)u.GetProgramVariable("_messageText")).text)
                .ToArray();

        // SDK 3.10.6 には ClientSim のテストの基底クラスが無いので、ClientSim を公開の入口で起こし、Domain Reload を切って Play Mode に入る。
        // 設定は static に持たれ、Domain Reload で消えるため。戻すのは RestoreEditor
        static IEnumerator InPlayMode(Scene scene, Action body)
        {
            RunBuildProcessor(scene);
            EditorSceneManager.SaveScene(scene);

            _prevOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            _prevOptions = EditorSettings.enterPlayModeOptions;
            _playModeOptionsChanged = true;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            ClientSimRuntimeLoader.BeginUnityTesting(new ClientSimSettings
            {
                enableClientSim = true, initializationDelay = 0, spawnPlayer = true, localPlayerIsMaster = true,
            });

            // Domain Reload を切ると、2 回目の Play Mode に入るときに、SDK の Texture2DDefaultTextureHolder が
            // 前の Play Mode で消えた Texture2D に触れて MissingReferenceException を出す。LogStream と関わらないので、入る間だけ失敗にしない
            LogAssert.ignoreFailingMessages = true;
            yield return new EnterPlayMode();
            LogAssert.ignoreFailingMessages = false;

            for (var i = 0; i < 300 && Networking.LocalPlayer == null; i++) { yield return null; }
            Assert.IsNotNull(Networking.LocalPlayer, "ClientSim のプレイヤーが出ない");

            // LogStreamCore は Start で配列を作るので、できるまで待つ
            var core = FindUdon("LogStreamCore");
            for (var i = 0; i < 600 && core.GetProgramVariable("_messages") == null; i++) { yield return null; }
            Assert.IsNotNull(core.GetProgramVariable("_messages"), "LogStreamCore の配列が初期化されない");
            yield return null;

            body();
        }

        static Component FindByTypeName(GameObject root, string fullName) =>
            root.GetComponentsInChildren<Component>(true).First(c => c != null && c.GetType().FullName == fullName);

        static UdonBehaviour FindUdon(string programName) =>
            UnityEngine.Object.FindObjectsOfType<UdonBehaviour>(true)
                .First(u => u.programSource != null && u.programSource.name == programName);

        static GameObject Instantiate(string path) =>
            (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));

        static void SetProp(Component c, string name, Action<SerializedProperty> set)
        {
            var so = new SerializedObject(c);
            set(so.FindProperty(name));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Scene NewSavedScene(string name, Action build)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            build();
            Directory.CreateDirectory(TempDir);
            EditorSceneManager.SaveScene(scene, $"{TempDir}/{name}.unity");
            return scene;
        }

        static void RunBuildProcessor(Scene scene)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("Kmnk.LogStream.LogStreamBuildProcessor"))
                .First(t => t != null);
            var processor = (IProcessSceneWithReport)Activator.CreateInstance(type, true);
            processor.OnProcessScene(scene, null);
        }
    }
}
