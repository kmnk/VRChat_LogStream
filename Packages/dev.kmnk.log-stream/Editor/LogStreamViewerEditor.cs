using UnityEditor;
using VRC.SDK3.Components;

namespace Kmnk.LogStream
{
    [CustomEditor(typeof(Udon.LogStreamViewer))]
    [CanEditMultipleObjects]
    internal class LogStreamViewerEditor : UdonSettingsEditor
    {
        protected override void DrawSettings()
        {
            Box("Core", () => Field("_id"));
            Box("Option", () => RelatedField<VRCPickup>("pickupable", "Pickupable", "ボードのピックアップ ON/OFF を切り替えます"));
        }
    }
}
