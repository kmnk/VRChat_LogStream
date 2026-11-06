using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace Kmnk.LogStreamDev.Tests
{
    // ギミックごとの振る舞い。リファクタリングの前後で同じであることを確かめる
    public partial class LogStreamTests
    {
        const string V2Fixture = "Assets/LogStreamDev/Fixtures/V2Overrides.unity";

        // PomodoroStatus の値（Runtime/LogPomodoro.cs）
        const int NotStarted = 0, ToPomodoro = 1, InPomodoro = 2, PausedPomodoro = 3, ToBreak = 4, InBreak = 5, PausedBreak = 6;

        const string PomodoroName = "<color='red'>Pomodoro</color>";
        const string BreakName = "<color='green'>Break</color>";

        static string Tag(string tag) => $"<color='#ff9900'>[{tag}]</color>";

        // 設定: 2.x の利用者の scene（全ての設定に上書きがある）をビルドの処理に通すと、全ての値が Udon と別のコンポーネントに効く
        [Test]
        public void BuildProcessor_AppliesAllSettings()
        {
            var scene = EditorSceneManager.OpenScene(V2Fixture, OpenSceneMode.Single);
            RunBuildProcessor(scene);
            AssertSettingsApplied();
        }

        // 2.x の設定を移すのは 1 度だけで、移した後に Udon で変えた値は、scene を開き直しても戻らない
        [Test]
        public void Migration_RunsOnceAndKeepsUdonEdits()
        {
            var scene = EditorSceneManager.OpenScene(V2Fixture, OpenSceneMode.Single);
            var legacy = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true).First(c => c.GetType().FullName == "Kmnk.LogStream.LogJoinLeave");
            var udon = new SerializedObject(FindByTypeName(legacy.gameObject, "Kmnk.LogStream.Udon.LogJoinLeave"));
            Assert.AreEqual("V2 _joinLog {{name}}", udon.FindProperty("_joinLog").stringValue, "scene を開いた時に移らない");
            Assert.IsTrue(new SerializedObject(legacy).FindProperty("_migrated").boolValue, "移した印が付かない");

            udon.FindProperty("_joinLog").stringValue = "edited";
            udon.ApplyModifiedPropertiesWithoutUndo();
            var count = (int)EditorType("Kmnk.LogStream.LegacySettingsMigrator").GetMethod("MigrateScene").Invoke(null, new object[] { scene });

            Assert.AreEqual(0, count, "2 度目にも移す");
            udon.Update();
            Assert.AreEqual("edited", udon.FindProperty("_joinLog").stringValue);
        }

        // 複数を選んだ Inspector で Id を変えると、選んだ全てが LogStreamCore に結ばれる
        [Test]
        public void Editor_AppliesToAllSelectedTargets()
        {
            GameObject core = null, a = null, b = null;
            NewSavedScene("MultiEdit", () =>
            {
                core = Instantiate(Prefabs + "LogStreamCore.prefab");
                SetProp(FindByTypeName(core, "Kmnk.LogStream.Udon.LogStreamCore"), "_id", p => p.intValue = 1);
                a = Instantiate(Prefabs + "LogJoinLeave.prefab");
                b = Instantiate(Prefabs + "LogJoinLeave.prefab");
            });
            var udons = new[] { a, b }.Select(o => (UnityEngine.Object)FindByTypeName(o, "Kmnk.LogStream.Udon.LogJoinLeave")).ToArray();
            // 型を渡さないと UdonSharp の既定の Inspector が作られる
            var editor = Editor.CreateEditor(udons, EditorType("Kmnk.LogStream.LogJoinLeaveEditor"));
            editor.serializedObject.FindProperty("_id").intValue = 1;
            editor.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            editor.GetType().GetMethod("ApplyRelated").Invoke(editor, null);
            UnityEngine.Object.DestroyImmediate(editor);

            var coreUdon = FindByTypeName(core, "Kmnk.LogStream.Udon.LogStreamCore");
            foreach (var u in udons)
            {
                Assert.AreSame(coreUdon, new SerializedObject(u).FindProperty("_core").objectReferenceValue, "選んだ 2 つ目以降が結ばれない");
            }
        }

        // ビルドの処理は、処理する scene の中の LogStreamCore にだけ結ぶ。どちらの scene を処理しても、その scene の LogStreamCore に結ぶ
        [Test]
        public void BuildProcessor_LinksWithinProcessedScene()
        {
            GameObject coreA = null, joinLeaveA = null;
            var sceneA = NewSavedScene("SceneA", () =>
            {
                coreA = Instantiate(Prefabs + "LogStreamCore.prefab");
                joinLeaveA = Instantiate(Prefabs + "LogJoinLeave.prefab");
            });
            var sceneB = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var coreB = Instantiate(Prefabs + "LogStreamCore.prefab");
            var joinLeaveB = Instantiate(Prefabs + "LogJoinLeave.prefab");
            EditorSceneManager.SaveScene(sceneB, $"{TempDir}/SceneB.unity");

            RunBuildProcessor(sceneA);
            RunBuildProcessor(sceneB);

            Assert.AreSame(FindByTypeName(coreA, "Kmnk.LogStream.Udon.LogStreamCore"), LinkedCore(joinLeaveA), "A が別の scene の LogStreamCore に結ばれる");
            Assert.AreSame(FindByTypeName(coreB, "Kmnk.LogStream.Udon.LogStreamCore"), LinkedCore(joinLeaveB), "B が別の scene の LogStreamCore に結ばれる");
        }

        static UnityEngine.Object LinkedCore(GameObject joinLeave) =>
            new SerializedObject(FindByTypeName(joinLeave, "Kmnk.LogStream.Udon.LogJoinLeave")).FindProperty("_core").objectReferenceValue;

        // Pomodoro: 操作と時間切れで状態が移り、移るたびに決まった文面のログが足される
        [UnityTest]
        public IEnumerator Pomodoro_TransitionsAndMessages()
        {
            var scene = NewSavedScene("Pomodoro", () =>
            {
                AddWorldAndCore(logLimit: 20);
                Instantiate(Prefabs + "LogPomodoro.prefab");
            });
            return InPlayMode(scene, PomodoroBody);
        }

        static IEnumerator PomodoroBody()
        {
            var core = FindUdon("LogStreamCore");
            var p = FindUdon("LogPomodoro");
            AssertPomodoro(p, NotStarted, 1);

            Pomodoro(p, "Toggle");
            AssertPomodoro(p, InPomodoro, 1);
            AssertLatest(core, $"{Tag("START")} {PomodoroName} #1 : 25 minutes");

            Pomodoro(p, "Toggle");
            AssertPomodoro(p, PausedPomodoro, 1);
            AssertLatest(core, $"{Tag("PAUSE")} {PomodoroName}");

            Pomodoro(p, "Toggle");
            AssertPomodoro(p, InPomodoro, 1);
            AssertLatest(core, $"{Tag("RESUME")} {PomodoroName}");

            yield return Expire(p, ToBreak);
            AssertPomodoro(p, ToBreak, 1);
            AssertLatest(core, $"{Tag("END")} {PomodoroName} #1");

            Pomodoro(p, "Toggle");
            AssertPomodoro(p, InBreak, 1);
            AssertLatest(core, $"{Tag("START")} {BreakName} #1 : 5 minutes");

            Pomodoro(p, "Toggle");
            AssertPomodoro(p, PausedBreak, 1);
            AssertLatest(core, $"{Tag("PAUSE")} {BreakName}");

            Pomodoro(p, "Toggle");
            AssertPomodoro(p, InBreak, 1);
            AssertLatest(core, $"{Tag("RESUME")} {BreakName}");

            yield return Expire(p, ToPomodoro);
            AssertPomodoro(p, ToPomodoro, 2);
            AssertLatest(core, $"{Tag("END")} {BreakName} #1");

            Pomodoro(p, "Skip");
            AssertPomodoro(p, ToBreak, 2);
            AssertLatest(core, $"{Tag("SKIP")} {PomodoroName} #2");

            Pomodoro(p, "Skip");
            AssertPomodoro(p, ToPomodoro, 3);
            AssertLatest(core, $"{Tag("SKIP")} {BreakName} #2");

            Pomodoro(p, "Skip");
            Pomodoro(p, "Skip");
            AssertPomodoro(p, ToPomodoro, 4);
            Pomodoro(p, "Skip");
            Pomodoro(p, "Toggle");
            // 4 回目の後の休憩は長い休憩
            AssertPomodoro(p, InBreak, 4);
            AssertLatest(core, $"{Tag("START")} {BreakName} #4 : 15 minutes");

            Pomodoro(p, "Reset");
            AssertPomodoro(p, NotStarted, 1);
            AssertLatest(core, $"{Tag("RESET")} {PomodoroName}");

            // 自動で続けるとき: 時間切れの後、間を置いて次が始まる
            Pomodoro(p, "ToggleAutoContinue");
            Assert.AreEqual(true, p.GetProgramVariable("_autoContinue"), "自動で続ける設定が入らない");
            Pomodoro(p, "Toggle");
            yield return Expire(p, ToBreak);
            AssertLatest(core, $"{Tag("END")} {PomodoroName} #1");
            yield return Expire(p, InBreak);
            AssertLatest(core, $"{Tag("START")} {BreakName} #1 : 5 minutes");
        }

        // Pomodoro: 状態が変わるたびに、登録した listener が 1 回呼ばれ、作業のタイマーが動いているかを読める
        [UnityTest]
        public IEnumerator Pomodoro_NotifiesListenersOnStatusChange()
        {
            var scene = NewSavedScene("PomodoroListener", () =>
            {
                AddWorldAndCore(logLimit: 20);
                Instantiate(Prefabs + "LogPomodoro.prefab");
                AddTestUdon("PomodoroTestListener", PomodoroTestListener);
            });
            return InPlayMode(scene, () =>
            {
                var p = FindUdon("LogPomodoro");
                var listener = FindUdon("PomodoroTestListener");
                listener.SetProgramVariable("pomodoro", p);
                listener.SendCustomEvent("Register");

                var expected = new[]
                {
                    ("Toggle", InPomodoro, true),
                    ("Toggle", PausedPomodoro, false),
                    ("Toggle", InPomodoro, true),
                    ("Skip", ToBreak, false),
                    ("Toggle", InBreak, false),
                    ("Reset", NotStarted, false),
                };
                for (var i = 0; i < expected.Length; i++)
                {
                    var (action, status, running) = expected[i];
                    Pomodoro(p, action);
                    Assert.AreEqual(i + 1, listener.GetProgramVariable("count"), $"{action} の後の通知の回数");
                    Assert.AreEqual(status, listener.GetProgramVariable("lastStatus"), $"{action} の後に listener が読んだ状態");
                    Assert.AreEqual(running, listener.GetProgramVariable("lastRunning"), $"{action} の後の IsTimerRunning");
                }

                // 状態が変わらない操作では呼ばれない
                Pomodoro(p, "ToggleAutoContinue");
                Assert.AreEqual(expected.Length, listener.GetProgramVariable("count"), "状態が変わらないのに通知される");
            });
        }

        // 分数と長い休憩までの回数は 1 以上に丸められる（0 で割らない）
        [UnityTest]
        public IEnumerator Pomodoro_ClampsMinutesAndInterval()
        {
            var scene = NewSavedScene("PomodoroClamp", () =>
            {
                AddWorldAndCore(logLimit: 10);
                var pomodoro = Instantiate(Prefabs + "LogPomodoro.prefab");
                var settings = FindByTypeName(pomodoro, "Kmnk.LogStream.LogPomodoro");
                SetProp(settings, "_pomodoroMinutes", p => p.intValue = 0);
                SetProp(settings, "_longBreakInterval", p => p.intValue = 0);
            });
            return InPlayMode(scene, () =>
            {
                var core = FindUdon("LogStreamCore");
                var p = FindUdon("LogPomodoro");
                Pomodoro(p, "Toggle");
                AssertLatest(core, $"{Tag("START")} {PomodoroName} #1 : 1 minutes");
                Pomodoro(p, "Skip");
                Pomodoro(p, "Toggle");
                // 長い休憩までの回数が 1 なので、毎回が長い休憩になる
                AssertLatest(core, $"{Tag("START")} {BreakName} #1 : 15 minutes");
            });
        }

        // prefab のボタンを押すと、3.0.0 の名前の event が呼ばれて動く
        [UnityTest]
        public IEnumerator Buttons_CallRenamedEvents()
        {
            var scene = NewSavedScene("Buttons", () =>
            {
                AddWorldAndCore(logLimit: 10);
                Instantiate(Prefabs + "LogPomodoro.prefab");
                Instantiate(Prefabs + "LogInput.prefab");
            });
            return InPlayMode(scene, () =>
            {
                var core = FindUdon("LogStreamCore");
                var p = FindUdon("LogPomodoro");
                Click(p.transform.parent, "ToggleButton");
                AssertPomodoro(p, InPomodoro, 1);
                Click(p.transform.parent, "SkipButton");
                AssertPomodoro(p, ToBreak, 1);
                Click(p.transform.parent, "ResetButton");
                AssertPomodoro(p, NotStarted, 1);
                Click(p.transform.parent, "ToggleAutoContinueButton");
                Assert.AreEqual(true, p.GetProgramVariable("_autoContinue"));
                Click(p.transform.parent, "ToggleOnlyMasterButton");
                Assert.AreEqual(true, p.GetProgramVariable("_onlyMaster"));

                var input = FindUdon("LogInput");
                var inputParent = (GameObject)input.GetProgramVariable("_inputParent");
                Click(input.transform.parent, "Button");
                Assert.IsTrue(inputParent.activeSelf, "入力欄が開かない");
                var template = input.transform.parent.GetComponentsInChildren<Button>(false)
                    .First(b => b.GetComponentInParent<UdonBehaviour>() != null
                        && b.GetComponentInParent<UdonBehaviour>().programSource.name == "LogInputTemplateButton");
                template.onClick.Invoke();
                var typed = ((InputField)input.GetProgramVariable("_inputField")).text;
                Assert.IsNotEmpty(typed, "テンプレートが入力欄に入らない");
                Click(input.transform.parent, "EnterButton");
                AssertLatest(core, typed);
                Click(input.transform.parent, "CancelButton");
                Assert.IsFalse(inputParent.activeSelf, "入力欄が閉じない");
            });
        }

        // root の下で name の GameObject の Button を押す
        static void Click(Transform root, string name)
        {
            var button = root.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == name);
            Assert.IsNotNull(button, name + " が無い");
            button.onClick.Invoke();
        }

        // 入退室: 入った人と出た人の名前を {{name}} に入れたログが足される
        [UnityTest]
        public IEnumerator JoinLeave_AddsPlayerName()
        {
            var scene = NewSavedScene("JoinLeave", () =>
            {
                AddWorldAndCore(logLimit: 10);
                Instantiate(Prefabs + "LogJoinLeave.prefab");
            });
            return InPlayMode(scene, () =>
            {
                var core = FindUdon("LogStreamCore");
                var udon = FindUdon("LogJoinLeave");
                var name = Networking.LocalPlayer.displayName;
                RunPlayerEvent(udon, "_onPlayerJoined", "onPlayerJoinedPlayer");
                AssertLatest(core, $"<color='green'>>>JOIN>></color> {name}");
                RunPlayerEvent(udon, "_onPlayerLeft", "onPlayerLeftPlayer");
                AssertLatest(core, $"<color='red'><LEAVE</color> {name}");
            });
        }

        // Trigger: 入った人と出た人の名前を {{name}} に入れたログが足される
        [UnityTest]
        public IEnumerator TriggerEnterExit_AddsPlayerName()
        {
            var scene = NewSavedScene("Trigger", () =>
            {
                AddWorldAndCore(logLimit: 10);
                Instantiate(Prefabs + "LogTriggerEnterExit.prefab");
            });
            return InPlayMode(scene, () =>
            {
                var core = FindUdon("LogStreamCore");
                var udon = FindUdon("LogTriggerEnterExit");
                var name = Networking.LocalPlayer.displayName;
                RunPlayerEvent(udon, "_onPlayerTriggerEnter", "onPlayerTriggerEnterPlayer");
                AssertLatest(core, $"{name} has entered");
                RunPlayerEvent(udon, "_onPlayerTriggerExit", "onPlayerTriggerExitPlayer");
                AssertLatest(core, $"{name} has exited");
            });
        }

        // 入力: 入力欄の文を送ると、送った人の名前でログが足される
        [UnityTest]
        public IEnumerator Input_AddsTypedMessage()
        {
            var scene = NewSavedScene("Input", () =>
            {
                AddWorldAndCore(logLimit: 10);
                Instantiate(Prefabs + "LogInput.prefab");
            });
            return InPlayMode(scene, () =>
            {
                var core = FindUdon("LogStreamCore");
                var udon = FindUdon("LogInput");
                ((InputField)udon.GetProgramVariable("_inputField")).text = "hello";
                udon.SendCustomEvent(InputSubmitEvent);
                AssertLatest(core, "hello");
                Assert.AreEqual(Networking.LocalPlayer.displayName, ((string[])core.GetProgramVariable("_names"))[0]);
            });
        }

        // UI から呼ぶ event の名前
        const string InputSubmitEvent = "_Submit";

        static readonly Dictionary<string, string> PomodoroEvents = new Dictionary<string, string>
        {
            ["Toggle"] = "_TogglePomodoro",
            ["Skip"] = "_SkipPomodoro",
            ["Reset"] = "_ResetPomodoro",
            ["ToggleAutoContinue"] = "_ToggleAutoContinue",
        };

        static void Pomodoro(UdonBehaviour p, string action) => p.SendCustomEvent(PomodoroEvents[action]);

        static void AssertPomodoro(UdonBehaviour p, int status, int count)
        {
            Assert.AreEqual(status, Convert.ToInt32(p.GetProgramVariable("_currentStatus")), "Pomodoro の状態");
            Assert.AreEqual(count, Convert.ToInt32(p.GetProgramVariable("_pomodoroCount")), "Pomodoro の回数");
        }

        // 終わりの時刻を過去にして、状態が status に移るのを待つ
        static IEnumerator Expire(UdonBehaviour p, int status)
        {
            p.SetProgramVariable("_endDateTimeTicks", DateTime.UtcNow.AddSeconds(-1).Ticks);
            var until = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < until && Convert.ToInt32(p.GetProgramVariable("_currentStatus")) != status)
            {
                yield return null;
            }
        }

        static void AssertLatest(UdonBehaviour core, string message)
        {
            Assert.AreEqual(message, ((string[])core.GetProgramVariable("_messages"))[0]);
        }

        // Udon の組み込みの event を、引数の変数を書いてから呼ぶ
        static void RunPlayerEvent(UdonBehaviour udon, string eventName, string playerVariable)
        {
            udon.SetProgramVariable(playerVariable, Networking.LocalPlayer);
            udon.SendCustomEvent(eventName);
        }

        // 2.x の設定のクラスの値が、同じ名前の Udon の field と、音・pickup のコンポーネントに入っているか
        static void AssertSettingsApplied()
        {
            var failures = new List<string>();
            foreach (var settings in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true)
                         .Where(c => c != null && c.GetType().Namespace == "Kmnk.LogStream"))
            {
                var name = settings.GetType().Name;
                var udon = settings.GetComponentsInChildren<Component>(true)
                    .First(c => c != null && c.GetType().FullName == "Kmnk.LogStream.Udon." + name);
                var udonSo = new SerializedObject(udon);
                var p = new SerializedObject(settings).GetIterator();
                p.NextVisible(true);
                while (p.NextVisible(false))
                {
                    var key = name + "." + p.name;
                    var actual = Actual(settings, udonSo, p.name);
                    if (actual == null) { continue; }
                    var expected = Value(p);
                    if (!Equals(expected, actual))
                    {
                        failures.Add($"{key}: 設定 {expected} / 効いている値 {actual}");
                    }
                }
            }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        // 設定の名前に対応する、効いている値。対応が無ければ null
        static object Actual(MonoBehaviour settings, SerializedObject udonSo, string name)
        {
            switch (name)
            {
                case "_soundEffectAudioClip": return settings.GetComponentInChildren<AudioSource>(true).clip;
                case "_soundEffectGain": return settings.GetComponentInChildren<VRCSpatialAudioSource>(true).Gain;
                case "_soundEffectFar": return settings.GetComponentInChildren<VRCSpatialAudioSource>(true).Far;
                case "_pickupable": return settings.GetComponentInChildren<VRCPickup>(true).pickupable;
                case "_templateMessages":
                case "_useTemplateText":
                case "_templateText":
                    return name == "_templateMessages" ? TemplateButtonTexts(settings) : null;
            }
            var p = udonSo.FindProperty(name);
            return p == null ? null : Value(p);
        }

        // テンプレートのボタンの文字を、設定（テキストのファイルか、文の配列）と比べられる形で返す
        static object TemplateButtonTexts(MonoBehaviour settings)
        {
            return string.Join("|", settings.GetComponentsInChildren<UdonBehaviour>(false)
                .Where(u => u.programSource != null && u.programSource.name == "LogInputTemplateButton")
                .Select(u => u.GetComponentInChildren<Text>(true).text));
        }

        // LogStreamDev.Tests の asmdef は LogStream の asmdef を参照しないので、型を名前で探す
        static Type EditorType(string fullName) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(fullName)).First(t => t != null);

        static object Value(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.String: return p.stringValue;
                case SerializedPropertyType.Integer: return p.intValue;
                case SerializedPropertyType.Boolean: return p.boolValue;
                case SerializedPropertyType.Float: return p.floatValue;
                case SerializedPropertyType.ObjectReference: return p.objectReferenceValue;
                case SerializedPropertyType.Enum: return p.intValue;
            }
            if (p.isArray && p.arrayElementType == "string")
            {
                var so = p.serializedObject;
                var items = Enumerable.Range(0, p.arraySize).Select(i => p.GetArrayElementAtIndex(i).stringValue).ToArray();
                // LogInput のテンプレートは、テキストのファイルを使う設定ならその行がボタンになる
                var useText = so.FindProperty("_useTemplateText");
                var text = so.FindProperty("_templateText");
                if (p.name == "_templateMessages" && useText != null && useText.boolValue && text.objectReferenceValue != null)
                {
                    items = ((TextAsset)text.objectReferenceValue).text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                }
                return string.Join("|", items);
            }
            return null;
        }
    }
}
