using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Core;

namespace Kmnk.LogStreamDev
{
    // サンプルの scene のワールドの ID を、git で追跡しない UserSettings/ に置く。
    // 開いた scene には ID を入れ、保存するファイルからは外す。保存が終わると開いている scene に戻すので、
    // VRChat SDK の Build & Upload は ID の入った PipelineManager を使う。
    // 新しいワールドとしてアップロードし SDK が ID を振ったときは、次の保存で ID をファイルに書く。
    // ID を変えるときは Tools > LogStreamDev > Sample Blueprint ID で書き換える
    [InitializeOnLoad]
    public static class SampleBlueprintId
    {
        // テストが一時的な scene とファイルに差し替える
        public static string ScenePath = "Packages/dev.kmnk.log-stream/Samples/LogStream.unity";
        public static string StorePath = "UserSettings/LogStreamDev/SampleBlueprintId.txt";

        static PipelineManager _restoreTarget;
        static string _restoreId;

        static SampleBlueprintId()
        {
            EditorSceneManager.sceneOpened += (scene, _) => Inject(scene);
            EditorSceneManager.sceneSaving += OnSaving;
            EditorSceneManager.sceneSaved += OnSaved;
            // Editor を起動したときに開いていた scene には sceneOpened が来ない
            EditorApplication.delayCall += () =>
            {
                for (var i = 0; i < SceneManager.sceneCount; i++) { Inject(SceneManager.GetSceneAt(i)); }
            };
        }

        public static string Load() => File.Exists(StorePath) ? File.ReadAllText(StorePath).Trim() : "";

        public static void Store(string id)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StorePath));
            File.WriteAllText(StorePath, id + "\n");
        }

        // 置いてある ID を scene に入れる。scene を変更ありにはしない
        public static void Inject(Scene scene)
        {
            if (scene.path != ScenePath) { return; }
            var pm = Find(scene);
            var id = Load();
            if (pm == null || id == "" || pm.blueprintId == id) { return; }
            pm.blueprintId = id;
        }

        static void OnSaving(Scene scene, string path)
        {
            if (path != ScenePath) { return; }
            var pm = Find(scene);
            if (pm == null || string.IsNullOrEmpty(pm.blueprintId)) { return; }
            if (Load() != pm.blueprintId) { Store(pm.blueprintId); }
            _restoreTarget = pm;
            _restoreId = pm.blueprintId;
            pm.blueprintId = "";
        }

        static void OnSaved(Scene scene)
        {
            if (_restoreTarget == null) { return; }
            _restoreTarget.blueprintId = _restoreId;
            _restoreTarget = null;
            _restoreId = null;
        }

        // ID の入ったサンプルの scene を開いて保存し直し、ID を置き場へ移す。
        // 動かし方: bin/lib/unity.sh の run_unity で -executeMethod Kmnk.LogStreamDev.SampleBlueprintId.MoveOutOfScene
        public static void MoveOutOfScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            EditorSceneManager.MarkSceneDirty(scene);
            var ok = EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LogStreamDev] saved={ok} stored={Load()}");
            EditorApplication.Exit(ok ? 0 : 1);
        }

        static PipelineManager Find(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) { return null; }
            foreach (var root in scene.GetRootGameObjects())
            {
                var pm = root.GetComponentInChildren<PipelineManager>(true);
                if (pm != null) { return pm; }
            }
            return null;
        }
    }

    // 置いてある ID を見て、書き換える。別のマシンで最初にサンプルをアップロードするときに使う
    public class SampleBlueprintIdWindow : EditorWindow
    {
        string _id;

        [MenuItem("Tools/LogStreamDev/Sample Blueprint ID")]
        static void Open() => GetWindow<SampleBlueprintIdWindow>("Sample Blueprint ID");

        void OnEnable() => _id = SampleBlueprintId.Load();

        void OnGUI()
        {
            EditorGUILayout.LabelField("置き場所", SampleBlueprintId.StorePath);
            _id = EditorGUILayout.TextField("World ID", _id);
            if (GUILayout.Button("保存して開いている scene に入れる"))
            {
                SampleBlueprintId.Store(_id.Trim());
                for (var i = 0; i < SceneManager.sceneCount; i++) { SampleBlueprintId.Inject(SceneManager.GetSceneAt(i)); }
            }
        }
    }
}
