using System.Linq;
using UdonSharp;
using UnityEditor;

namespace Kmnk.LogStream
{
    // PomodoroVoiceZone の外側の Inspector。範囲の決め方の tips と、Pomodoro が結ばれていないときの警告を描く
    [CustomEditor(typeof(PomodoroVoiceZone))]
    [CanEditMultipleObjects]
    internal class PomodoroVoiceZoneRootEditor : GimmickRootEditor
    {
        protected override void DrawTips(UdonSharpBehaviour[] udons)
        {
            EditorGUILayout.HelpBox(
                "範囲は子の Udon の Box Collider の Center と Size で決めます。足元の位置で判定するので、下の面を床より下まで伸ばしてください",
                MessageType.Info);
            var unlinked = udons.Any(u => new SerializedObject(u).FindProperty("_pomodoro").objectReferenceValue == null);
            if (unlinked)
            {
                EditorGUILayout.HelpBox("Pomodoro が空です。LogPomodoro の子の Udon を入れてください", MessageType.Warning);
            }
        }
    }
}
