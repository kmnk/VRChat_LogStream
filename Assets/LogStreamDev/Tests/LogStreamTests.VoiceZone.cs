using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using VRC.SDK3.ClientSim;
using VRC.SDKBase;

namespace Kmnk.LogStreamDev.Tests
{
    // PomodoroVoiceZone: 作業のタイマーが動いている間、聞く人（ローカル）か話す人（リモート）のどちらかが範囲の中なら、リモートの声の値を変える。
    // ClientSim は音を鳴らさないので、声の値で確かめる。リモートの player は TeleportTo が効かないので transform を動かし、範囲は collider の方を動かす
    public partial class LogStreamTests
    {
        const string VoiceZoneType = "Kmnk.LogStream.Udon.PomodoroVoiceZone";

        // リモートの player をローカルから x にこれだけ離して置く。範囲（10 m 四方）を、ローカルだけ・リモートだけ・両方・どちらも外の 4 通りに置ける
        const float RemoteOffset = 8f;

        static VRCPlayerApi _remote;
        static Vector3 _localPosition;

        // 作業中は、要件 2 の表の「変える」組み合わせで声が届かなくなり、外れると変える前の値に戻る
        [UnityTest]
        public IEnumerator VoiceZone_MutesWhileRunning()
        {
            return InPlayMode(VoiceZoneScene("VoiceZoneMute", null), VoiceZoneMuteBody);
        }

        static IEnumerator VoiceZoneMuteBody()
        {
            yield return SpawnRemoteForVoiceZone();
            var p = FindUdon("LogPomodoro");
            _remote.SetVoiceDistanceNear(1f);
            _remote.SetVoiceDistanceFar(10f);
            _remote.SetVoiceVolumetricRadius(0.5f);

            PlaceZone("outside");
            Pomodoro(p, "Toggle");
            AssertPomodoro(p, InPomodoro, 1);
            yield return WaitSeconds(1.5f);
            Assert.AreEqual(10f, _remote.GetVoiceDistanceFar(), "外と外なのに変わる");

            PlaceZone("local");
            yield return WaitUntilFar(0f);
            AssertVoice(0f, 0f, 0f, "聞く人だけが中のときに届かなくならない");

            PlaceZone("outside");
            yield return WaitUntilFar(10f);
            AssertVoice(1f, 10f, 0.5f, "外へ出たときに変える前の値に戻らない");

            PlaceZone("remote");
            yield return WaitUntilFar(0f);
            AssertVoice(0f, 0f, 0f, "話す人だけが中のときに届かなくならない");

            PlaceZone("both");
            yield return WaitSeconds(1.5f);
            AssertVoice(0f, 0f, 0f, "中と中のときに届く");

            Pomodoro(p, "Toggle");
            AssertPomodoro(p, PausedPomodoro, 1);
            yield return WaitUntilFar(10f);
            AssertVoice(1f, 10f, 0.5f, "一時停止で変える前の値に戻らない");
        }

        // 完全に聞こえなくしない設定では、届く距離は変えず gain を設定の値にし、戻すと変える前の gain になる
        [UnityTest]
        public IEnumerator VoiceZone_ReducesGainWhenNotMutingCompletely()
        {
            var scene = VoiceZoneScene("VoiceZoneGain", zone =>
            {
                SetProp(zone, "_muteCompletely", p => p.boolValue = false);
                SetProp(zone, "_reducedVoiceGain", p => p.floatValue = 5f);
            });
            return InPlayMode(scene, VoiceZoneGainBody);
        }

        static IEnumerator VoiceZoneGainBody()
        {
            yield return SpawnRemoteForVoiceZone();
            var p = FindUdon("LogPomodoro");
            _remote.SetVoiceGain(8f);
            var far = _remote.GetVoiceDistanceFar();

            PlaceZone("remote");
            Pomodoro(p, "Toggle");
            yield return WaitUntil(() => _remote.GetVoiceGain() == 5f, 2.5f);
            Assert.AreEqual(5f, _remote.GetVoiceGain(), "gain が設定の値にならない");
            Assert.AreEqual(far, _remote.GetVoiceDistanceFar(), "小さくする設定なのに届く距離が変わる");

            Pomodoro(p, "Toggle");
            yield return WaitUntil(() => _remote.GetVoiceGain() == 8f, 2.5f);
            Assert.AreEqual(8f, _remote.GetVoiceGain(), "変える前の gain に戻らない");
        }

        // 作業のタイマーが動いていない状態（始まる前・一時停止・次を待つ間・休憩）では、範囲の中でも声を変えない
        [UnityTest]
        public IEnumerator VoiceZone_IgnoresNonRunningStates()
        {
            return InPlayMode(VoiceZoneScene("VoiceZoneStates", null), VoiceZoneStatesBody);
        }

        static IEnumerator VoiceZoneStatesBody()
        {
            yield return SpawnRemoteForVoiceZone();
            var p = FindUdon("LogPomodoro");
            _remote.SetVoiceDistanceFar(10f);
            PlaceZone("both");

            yield return WaitSeconds(1.5f);
            Assert.AreEqual(10f, _remote.GetVoiceDistanceFar(), "始まる前に変わる");

            Pomodoro(p, "Toggle");
            yield return WaitUntilFar(0f);
            Assert.AreEqual(0f, _remote.GetVoiceDistanceFar(), "作業中に変わらない");

            var steps = new[] { ("Toggle", PausedPomodoro), ("Skip", ToBreak), ("Toggle", InBreak), ("Toggle", PausedBreak), ("Skip", ToPomodoro) };
            foreach (var (action, status) in steps)
            {
                Pomodoro(p, action);
                Assert.AreEqual(status, Convert.ToInt32(p.GetProgramVariable("_currentStatus")), $"{action} の後の状態");
                yield return WaitSeconds(1.5f);
                Assert.AreEqual(10f, _remote.GetVoiceDistanceFar(), $"状態 {status} で声が変わる");
            }
        }

        // ギミックの prefab は、親に外側のコンポーネント（GimmickRoot の派生）を持ち、子孫にそれと同じ名前の UdonSharpBehaviour がある。
        // LogStream（他の prefab を束ねたもの）と LogSample（親に Udon を直に置いた例）は外側を持たない
        [Test]
        public void Prefabs_HaveGimmickRoot()
        {
            var rootType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Kmnk.LogStream.GimmickRoot")).First(t => t != null);
            var paths = AssetDatabase.FindAssets("t:Prefab", new[] { Prefabs.TrimEnd('/') })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.EndsWith("/LogStream.prefab") && !p.EndsWith("/LogSample.prefab"))
                .ToArray();
            Assert.AreEqual(7, paths.Length, "ギミックの prefab の数");
            foreach (var path in paths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var root = prefab.GetComponent(rootType);
                Assert.IsNotNull(root, $"{path} の親に外側のコンポーネントが無い");
                Assert.IsTrue(prefab.GetComponentsInChildren<UdonSharp.UdonSharpBehaviour>(true).Any(u => u.GetType().Name == root.GetType().Name),
                    $"{path} の子に {root.GetType().Name} の Udon が無い");
            }
        }

        // prefab の Udon に PomodoroVoiceZone と isTrigger の BoxCollider があり、範囲がそれを指す。sample の scene では scene の LogPomodoro のどれかに結ばれている
        [Test]
        public void VoiceZone_PrefabHasTriggerCollider()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "PomodoroVoiceZone.prefab");
            Assert.IsNotNull(prefab, "prefab が無い");
            var udon = prefab.transform.Find("Udon");
            Assert.IsNotNull(udon, "子の Udon が無い");
            var box = udon.GetComponent<BoxCollider>();
            Assert.IsNotNull(box, "BoxCollider が無い");
            Assert.IsTrue(box.isTrigger, "BoxCollider が isTrigger でない");
            var zones = new SerializedObject(FindByTypeName(prefab, VoiceZoneType)).FindProperty("_zones");
            Assert.AreEqual(1, zones.arraySize);
            Assert.AreSame(box, zones.GetArrayElementAtIndex(0).objectReferenceValue, "範囲が BoxCollider を指さない");

            var scene = EditorSceneManager.OpenScene(SampleScene, OpenSceneMode.Single);
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true)).Where(c => c != null).ToArray();
            var zone = all.Single(c => c.GetType().FullName == VoiceZoneType);
            // sample には LogStream の組が 2 つあり、範囲はそのどちらかの LogPomodoro に結ぶ
            var pomodoros = all.Where(c => c.GetType().FullName == "Kmnk.LogStream.Udon.LogPomodoro").ToArray();
            CollectionAssert.Contains(pomodoros, new SerializedObject(zone).FindProperty("_pomodoro").objectReferenceValue, "sample の範囲が scene の LogPomodoro に結ばれていない");
        }

        // 床と LogPomodoro と PomodoroVoiceZone を置いた scene。範囲は始めは誰もいない所に置き、configure で設定を変える
        static UnityEngine.SceneManagement.Scene VoiceZoneScene(string name, Action<Component> configure)
        {
            return NewSavedScene(name, () =>
            {
                AddWorldAndCore(logLimit: 20);
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "Floor";
                floor.transform.position = new Vector3(0f, -0.5f, 0f);
                floor.transform.localScale = new Vector3(200f, 1f, 200f);
                var pomodoro = Instantiate(Prefabs + "LogPomodoro.prefab");
                var zone = Instantiate(Prefabs + "PomodoroVoiceZone.prefab");
                zone.transform.position = new Vector3(100f, 0f, 0f);
                var settings = FindByTypeName(zone, VoiceZoneType);
                SetProp(settings, "_pomodoro", p => p.objectReferenceValue = FindByTypeName(pomodoro, "Kmnk.LogStream.Udon.LogPomodoro"));
                configure?.Invoke(settings);
            });
        }

        static IEnumerator SpawnRemoteForVoiceZone()
        {
            ClientSimMain.SpawnRemotePlayer("Remote");
            _remote = null;
            for (var i = 0; i < 300 && _remote == null; i++)
            {
                _remote = VRCPlayerApi.AllPlayers.FirstOrDefault(pl => !pl.isLocal);
                yield return null;
            }
            Assert.IsNotNull(_remote, "リモートのプレイヤーが出ない");
            // ローカルが床に立つのを待ってから、位置を基準にする
            yield return WaitSeconds(0.5f);
            _localPosition = Networking.LocalPlayer.GetPosition();
            _remote.gameObject.transform.position = _localPosition + new Vector3(RemoteOffset, 0f, 0f);
            yield return null;
            Assert.Less(Vector3.Distance(_localPosition + new Vector3(RemoteOffset, 0f, 0f), _remote.GetPosition()), 0.01f, "リモートの player を動かせない");
        }

        // 範囲を、ローカルだけ（local）・リモートだけ（remote）・両方（both）・どちらも外（outside）が中に入る位置へ動かす
        static void PlaceZone(string where)
        {
            var x = where == "local" ? 0f : where == "remote" ? RemoteOffset : where == "both" ? RemoteOffset / 2f : 100f;
            var zone = FindUdon("PomodoroVoiceZone").transform.parent;
            zone.position = _localPosition + new Vector3(x, 0f, 0f);
        }

        static void AssertVoice(float near, float far, float volumetric, string message)
        {
            Assert.AreEqual(near, _remote.GetVoiceDistanceNear(), message + "（Near）");
            Assert.AreEqual(far, _remote.GetVoiceDistanceFar(), message + "（Far）");
            Assert.AreEqual(volumetric, _remote.GetVoiceVolumetricRadius(), message + "（Volumetric Radius）");
        }

        // 範囲の判定は 1 秒ごとなので、spec の要件 8 の 2 秒に余裕を足して 2.5 秒まで待つ
        static IEnumerator WaitUntilFar(float far) => WaitUntil(() => _remote.GetVoiceDistanceFar() == far, 2.5f);

        static IEnumerator WaitUntil(Func<bool> condition, float seconds)
        {
            var until = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < until) { yield return null; }
        }

        static IEnumerator WaitSeconds(float seconds)
        {
            var until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until) { yield return null; }
        }
    }
}
