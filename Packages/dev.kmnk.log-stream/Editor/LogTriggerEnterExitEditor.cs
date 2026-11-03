using UnityEditor;

namespace Kmnk.LogStream
{
    [CustomEditor(typeof(Udon.LogTriggerEnterExit))]
    [CanEditMultipleObjects]
    internal class LogTriggerEnterExitEditor : UdonSettingsEditor
    {
        protected override void DrawSettings()
        {
            Box("Core", () => Field("_id"));
            Box("Option", () =>
            {
                Field("_enterLog");
                Field("_exitLog");
            });
        }
    }
}
