using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Kmnk.LogStreamDev
{
    // 2.x の prefab を置き、設定のクラスの値に既定と違う上書きを入れた scene を作る。
    // 3.0.0 で設定を Udon へ移す処理のテストが、この scene を 2.x の利用者の scene として開く。
    // 作り直すと 2.x の prefab でなくなるので、2.x の prefab がある commit でだけ動かす。
    // 動かし方: bin/lib/unity.sh の run_unity で -executeMethod Kmnk.LogStreamDev.V2Fixture.Create
    public static class V2Fixture
    {
        public const string ScenePath = "Assets/LogStreamDev/Fixtures/V2Overrides.unity";

        const string Prefabs = "Assets/Kmnk/LogStream/Prefabs/";

        static readonly string[] Gimmicks =
        {
            "LogStreamCore", "LogStreamViewer", "LogInput", "LogJoinLeave", "LogTriggerEnterExit", "LogPomodoro",
        };

        public static void Create()
        {
            try
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Instantiate("Packages/com.vrchat.worlds/Samples/UdonExampleScene/Prefabs/VRCWorld.prefab");
                foreach (var name in Gimmicks)
                {
                    var root = Instantiate(Prefabs + name + ".prefab");
                    var settings = root.GetComponents<Component>().First(c => c != null && c.GetType().FullName == "Kmnk.LogStream." + name);
                    var so = new SerializedObject(settings);
                    var p = so.GetIterator();
                    p.NextVisible(true);
                    while (p.NextVisible(false))
                    {
                        Override(p);
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log("[LogStreamDev] created " + ScenePath);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        // 型ごとに既定と違う値を入れる。_id は Core と結ぶのに使うので変えない
        static void Override(SerializedProperty p)
        {
            if (p.name == "_id") { return; }
            switch (p.propertyType)
            {
                case SerializedPropertyType.String:
                    p.stringValue = p.name == "_timeFormat" ? "HH-mm-ss" : "V2 " + p.name + " {{name}}";
                    break;
                case SerializedPropertyType.Integer:
                    p.intValue = 3;
                    break;
                case SerializedPropertyType.Boolean:
                    p.boolValue = !p.boolValue;
                    break;
                case SerializedPropertyType.Float:
                    p.floatValue = 3.5f;
                    break;
                case SerializedPropertyType.ObjectReference:
                    if (p.name == "_soundEffectAudioClip")
                    {
                        p.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Kmnk/LogStream/AudioClip/chi-n2.mp3");
                    }
                    else if (p.name == "_templateText")
                    {
                        p.objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Kmnk/LogStream/TemplateText/SampleTemplateText.txt");
                    }
                    break;
                case SerializedPropertyType.Generic:
                    if (p.isArray && p.arrayElementType == "string")
                    {
                        p.ClearArray();
                        for (var i = 0; i < 2; i++)
                        {
                            p.InsertArrayElementAtIndex(i);
                            p.GetArrayElementAtIndex(i).stringValue = $"V2 {p.name} {i}";
                        }
                    }
                    break;
            }
        }

        static GameObject Instantiate(string path)
        {
            return (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        }
    }
}
