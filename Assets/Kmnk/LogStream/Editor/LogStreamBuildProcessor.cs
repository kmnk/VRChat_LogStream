using System.Linq;
using UdonSharp;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

namespace Kmnk.LogStream
{
    // scene のビルドのたびに、その scene の中だけで、2.x の設定を移し、Id から LogStreamCore を結び、テンプレートのボタンを作り直す
    internal class LogStreamBuildProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            LegacySettingsMigrator.MigrateScene(scene);

            var roots = scene.GetRootGameObjects();
            foreach (var udon in roots.SelectMany(o => o.GetComponentsInChildren<UdonSharpBehaviour>(true)))
            {
                LogStreamEditorUtility.LinkCore(udon);
            }
            foreach (var input in roots.SelectMany(o => o.GetComponentsInChildren<Udon.LogInput>(true)))
            {
                LogInputEditor.RebuildTemplateButtons(input, false);
            }
        }
    }
}
