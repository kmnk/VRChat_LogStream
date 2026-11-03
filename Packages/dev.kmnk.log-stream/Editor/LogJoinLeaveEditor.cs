using UnityEditor;

namespace Kmnk.LogStream
{
    [CustomEditor(typeof(Udon.LogJoinLeave))]
    [CanEditMultipleObjects]
    internal class LogJoinLeaveEditor : UdonSettingsEditor
    {
        protected override void DrawSettings()
        {
            Box("Core", () => Field("_id"));
            Box("Option", () =>
            {
                Field("_joinLog");
                Field("_leaveLog");
            });
        }
    }
}
