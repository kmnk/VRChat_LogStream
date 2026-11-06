using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
    public partial class LogStreamTests
    {
        const string TempDir = "Assets/LogStreamDev/Temp";
        const string Prefabs = "Packages/dev.kmnk.log-stream/Prefabs/";
        const string SampleScene = "Packages/dev.kmnk.log-stream/Samples/LogStream.unity";
        const string VRCWorld = "Packages/com.vrchat.worlds/Samples/UdonExampleScene/Prefabs/VRCWorld.prefab";
        const string TestWriter = "Assets/LogStreamDev/TestUdon/LogStreamTestWriter.asset";
        const string PomodoroTestListener = "Assets/LogStreamDev/TestUdon/PomodoroTestListener.asset";

        // LogType の値（Runtime/LogType.cs）
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

        // 実行の層: 絞り込みのボタンを押すと Viewer がその LogType に絞り込み、全てのボタンに今の絞り込みが伝わる
        [UnityTest]
        public IEnumerator ToggleTypeButton_FiltersAndNotifiesButtons()
        {
            var scene = NewSavedScene("ToggleLayer", () =>
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

                var viewer = FindUdon("LogStreamViewer");
                var buttons = viewer.GetComponentsInChildren<UdonBehaviour>(true)
                    .Where(u => u.programSource != null && u.programSource.name == "LogStreamToggleTypeButton")
                    .ToArray();
                var notificationButton = buttons.First(b => Convert.ToInt32(b.GetProgramVariable("_type")) == Notification);
                var messageButton = buttons.First(b => Convert.ToInt32(b.GetProgramVariable("_type")) == Message);
                notificationButton.SendCustomEvent(ToggleTypeEvent);
                CollectionAssert.AreEqual(new[] { "n1", "" }, DisplayedMessages(viewer, 2));

                // 別のボタンを押すと、先に押したボタンは Viewer からの通知で外れる
                messageButton.SendCustomEvent(ToggleTypeEvent);
                CollectionAssert.AreEqual(new[] { "m1", "" }, DisplayedMessages(viewer, 2));
                foreach (var b in buttons)
                {
                    Assert.AreEqual(b == messageButton, b.GetProgramVariable("_isActive"), "ボタンに絞り込みが伝わらない");
                }
            });
        }

        // UI から呼ぶ event の名前
        const string ToggleTypeEvent = "_Toggle";

        // owner でない人から owner に送られたログを、owner が足す
        [UnityTest]
        public IEnumerator AddMessage_OwnerReceivesNetworkEvent()
        {
            var scene = NewSavedScene("OwnerEvent", () =>
            {
                AddWorldAndCore(logLimit: 3);
                AddTestWriter();
            });
            return InPlayMode(scene, OwnerEventBody);
        }

        static IEnumerator OwnerEventBody()
        {
            var core = FindUdon("LogStreamCore");
            var writer = FindUdon("LogStreamTestWriter");
            writer.SetProgramVariable("core", core);
            writer.SetProgramVariable("type", Message);
            writer.SetProgramVariable("message", "from remote");
            writer.SetProgramVariable("playerName", "Remote");
            writer.SendCustomEvent("SendToOwner");
            for (var i = 0; i < 120 && ((string[])core.GetProgramVariable("_messages"))[0] != "from remote"; i++) { yield return null; }
            Assert.AreEqual("from remote", ((string[])core.GetProgramVariable("_messages"))[0], "owner への event で足されない");
            Assert.AreEqual("Remote", ((string[])core.GetProgramVariable("_names"))[0]);
        }

        // owner でないときは、自分では配列を書かず所有権も取らない
        [UnityTest]
        public IEnumerator AddMessage_NonOwnerDoesNotWriteLocally()
        {
            var scene = NewSavedScene("NonOwner", () =>
            {
                AddWorldAndCore(logLimit: 3);
                AddTestWriter();
            });
            return InPlayMode(scene, NonOwnerBody);
        }

        static IEnumerator NonOwnerBody()
        {
            var core = FindUdon("LogStreamCore");
            ClientSimMain.SpawnRemotePlayer("Remote");
            VRCPlayerApi remote = null;
            for (var i = 0; i < 300 && remote == null; i++)
            {
                remote = VRCPlayerApi.AllPlayers.FirstOrDefault(p => !p.isLocal);
                yield return null;
            }
            Assert.IsNotNull(remote, "リモートのプレイヤーが出ない");
            Networking.SetOwner(remote, core.gameObject);
            Assert.IsFalse(Networking.IsOwner(core.gameObject), "所有権が移らない");

            Write(core, Message, "local");
            for (var i = 0; i < 30; i++) { yield return null; }
            Assert.AreNotEqual("local", ((string[])core.GetProgramVariable("_messages"))[0], "owner でないのに自分で書く");
            Assert.IsFalse(Networking.IsOwner(core.gameObject), "ログを足すと所有権を取る");
        }

        // 本文は MaxMessageLength（256 文字）で切られる
        [UnityTest]
        public IEnumerator AddMessage_TruncatesLongMessage()
        {
            var scene = NewSavedScene("Truncate", () =>
            {
                AddWorldAndCore(logLimit: 3);
                AddTestWriter();
            });
            return InPlayMode(scene, () =>
            {
                var core = FindUdon("LogStreamCore");
                Write(core, Message, new string('a', 300));
                Assert.AreEqual(256, ((string[])core.GetProgramVariable("_messages"))[0].Length);
            });
        }

        // 実行の層は毎フレーム動く処理を持たない
        [Test]
        public void Runtime_HasNoUpdate()
        {
            var withUpdate = Directory.GetFiles("Packages/dev.kmnk.log-stream/Runtime", "*.cs", SearchOption.AllDirectories)
                .Where(f => System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(f), @"void\s+(Update|LateUpdate|FixedUpdate)\s*\("))
                .ToArray();
            Assert.IsEmpty(withUpdate, string.Join(", ", withUpdate));
        }

        // VRChat SDK がビルドの前に求める Layer と衝突の行列の設定が、ProjectSettings に入っている。
        // SDK を上げると求める配置が変わることがあり、そのときは VRChat SDK の画面の Builder の
        // Setup Layers for VRChat と Set Collision Matrix で直して ProjectSettings を commit する
        [Test]
        public void ProjectSettings_HasVRChatLayers()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch (System.Reflection.ReflectionTypeLoadException) { return new Type[0]; } })
                .First(t => t.Name == "UpdateLayers");
            const System.Reflection.BindingFlags Static = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
            Assert.IsTrue((bool)type.GetMethod("AreLayersSetup", Static).Invoke(null, null), "VRChat の Layer が設定されていない");
            Assert.IsTrue((bool)type.GetMethod("IsCollisionLayerMatrixSetup", Static).Invoke(null, null), "VRChat の衝突の行列が設定されていない");
        }

        // サンプルの scene は、git で追跡するファイルにワールドの ID を持たない（ID は SampleBlueprintId が UserSettings/ に置く）
        [Test]
        public void SampleScene_HasNoBlueprintId()
        {
            var ids = Regex.Matches(File.ReadAllText(SampleScene), @"^\s*blueprintId:(.*)$", RegexOptions.Multiline);
            Assert.AreEqual(1, ids.Count, "PipelineManager が 1 つでない");
            Assert.AreEqual("", ids[0].Groups[1].Value.Trim(), "サンプルの scene にワールドの ID が入っている");
        }

        // 開くと置いてある ID が入り、保存したファイルからは外れ、開いている scene には残る。SDK が振った新しい ID は保存のときに置き場へ書く
        [Test]
        public void SampleBlueprintId_KeepsIdOutOfSavedScene()
        {
            var prevScene = SampleBlueprintId.ScenePath;
            var prevStore = SampleBlueprintId.StorePath;
            var path = $"{TempDir}/Sample.unity";
            try
            {
                SampleBlueprintId.ScenePath = path;
                SampleBlueprintId.StorePath = "Temp/LogStreamDevTests/SampleBlueprintId.txt";
                NewSavedScene("Sample", () => Instantiate(VRCWorld));
                SampleBlueprintId.Store("wrld_test-old");

                var scene = EditorSceneManager.OpenScene(path);
                Assert.AreEqual("wrld_test-old", BlueprintId(), "開いた scene に置いてある ID が入らない");

                // 新しいワールドとしてアップロードすると、SDK が ID を振り替える
                SetProp(PipelineManager(), "blueprintId", p => p.stringValue = "wrld_test-new");
                EditorSceneManager.SaveScene(scene);

                StringAssert.DoesNotContain("wrld_test", File.ReadAllText(path), "保存したファイルに ID が残る");
                Assert.AreEqual("wrld_test-new", BlueprintId(), "保存の後に開いている scene の ID が戻らない");
                Assert.AreEqual("wrld_test-new", SampleBlueprintId.Load(), "新しい ID が置き場に書かれない");
            }
            finally
            {
                SampleBlueprintId.ScenePath = prevScene;
                SampleBlueprintId.StorePath = prevStore;
                Directory.Delete("Temp/LogStreamDevTests", true);
            }
        }

        static Component PipelineManager() =>
            UnityEngine.Object.FindObjectsOfType<Component>(true).First(c => c != null && c.GetType().FullName == "VRC.Core.PipelineManager");

        static string BlueprintId() => new SerializedObject(PipelineManager()).FindProperty("blueprintId").stringValue;

        static void AddWorldAndCore(int logLimit)
        {
            Instantiate(VRCWorld);
            var core = Instantiate(Prefabs + "LogStreamCore.prefab");
            SetProp(FindByTypeName(core, "Kmnk.LogStream.LogStreamCore"), "_logLimit", p => p.intValue = logLimit);
        }

        static void AddTestWriter()
        {
            AddTestUdon("TestWriter", TestWriter);
        }

        // テスト用の U# スクリプトの program を持つ GameObject を置く
        static GameObject AddTestUdon(string name, string programPath)
        {
            var go = new GameObject(name);
            go.AddComponent<UdonBehaviour>().programSource = AssetDatabase.LoadAssetAtPath<AbstractUdonProgramSource>(programPath);
            return go;
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
            return InPlayMode(scene, () => Once(body));
        }

        static IEnumerator Once(Action body)
        {
            body();
            yield break;
        }

        // body が IEnumerator を返す形。Play Mode の中でフレームを待つテストに使う
        static IEnumerator InPlayMode(Scene scene, Func<IEnumerator> body)
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

            yield return body();
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
