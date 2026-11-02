using System.Linq;
using UdonSharp;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Components;

namespace Kmnk.LogStream
{
    // 2.x の設定のクラス（LegacySettings）の値を、子の Udon と音・pickup のコンポーネントへ 1 度だけ移す。
    // 2.x の scene を開いた時、Editor の起動とスクリプトの再読み込みの後、ビルドの時、親の Inspector を開いた時に走る
    [InitializeOnLoad]
    internal static class LegacySettingsMigrator
    {
        static LegacySettingsMigrator()
        {
            EditorSceneManager.sceneOpened += (scene, mode) => MigrateScene(scene);
            EditorApplication.delayCall += MigrateOpenScenes;
        }

        private static void MigrateOpenScenes()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                MigrateScene(SceneManager.GetSceneAt(i));
            }
        }

        // scene の中の全ての 2.x の設定を移す。移したものの数を返す
        public static int MigrateScene(Scene scene)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { return 0; }
            if (!scene.IsValid() || !scene.isLoaded) { return 0; }
            var count = scene.GetRootGameObjects()
                .SelectMany(o => o.GetComponentsInChildren<LegacySettings>(true))
                .Count(Migrate);
            if (count > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"[LogStream] {scene.name}: 2.x の設定 {count} 件を Udon へ移しました");
            }
            return count;
        }

        // 1 つの 2.x の設定を移す。移したら true
        public static bool Migrate(LegacySettings legacy)
        {
            var legacySo = new SerializedObject(legacy);
            var migrated = legacySo.FindProperty("_migrated");
            if (migrated.boolValue) { return false; }

            var udon = legacy.GetComponentsInChildren<UdonSharpBehaviour>(true)
                .FirstOrDefault(u => u.GetType().Name == legacy.GetType().Name);
            if (udon == null)
            {
                Debug.LogWarning($"[LogStream] {legacy.name}: 2.x の設定の移し先の Udon が見つかりません", legacy);
                return false;
            }

            var udonSo = new SerializedObject(udon);
            var p = legacySo.GetIterator();
            p.NextVisible(true);
            while (p.NextVisible(false))
            {
                if (!CopyToRelated(legacy, p) && udonSo.FindProperty(p.name) != null)
                {
                    udonSo.CopyFromSerializedProperty(p);
                }
            }
            udonSo.ApplyModifiedProperties();

            migrated.boolValue = true;
            legacySo.ApplyModifiedProperties();
            return true;
        }

        // Udon でなく別のコンポーネントが持つ設定を写す。写したら true
        private static bool CopyToRelated(LegacySettings legacy, SerializedProperty p)
        {
            switch (p.name)
            {
                case "_soundEffectAudioClip":
                    return Set<AudioSource>(legacy, "m_audioClip", t => t.objectReferenceValue = p.objectReferenceValue);
                case "_soundEffectGain":
                    return Set<VRCSpatialAudioSource>(legacy, "Gain", t => t.floatValue = p.floatValue);
                case "_soundEffectFar":
                    return Set<VRCSpatialAudioSource>(legacy, "Far", t => t.floatValue = p.floatValue);
                case "_pickupable":
                    return Set<VRCPickup>(legacy, "pickupable", t => t.boolValue = p.boolValue);
                default:
                    return false;
            }
        }

        private static bool Set<T>(LegacySettings legacy, string propertyName, System.Action<SerializedProperty> set) where T : Component
        {
            var component = legacy.GetComponentInChildren<T>(true);
            if (component != null)
            {
                var so = new SerializedObject(component);
                set(so.FindProperty(propertyName));
                so.ApplyModifiedProperties();
            }
            return true;
        }
    }
}
