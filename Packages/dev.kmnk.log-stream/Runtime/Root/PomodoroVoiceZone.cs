using UnityEngine;

namespace Kmnk.LogStream
{
    // PomodoroVoiceZone の外側のコンポーネント。設定は子の Udon（Kmnk.LogStream.Udon.PomodoroVoiceZone）が持つ
    [DisallowMultipleComponent]
    public class PomodoroVoiceZone : GimmickRoot
    {
        // 範囲の collider は選んだときにしか描かれないので、選んでいなくても範囲が見えるよう、子の BoxCollider を線で描く
        private void OnDrawGizmos()
        {
            var boxes = GetComponentsInChildren<BoxCollider>(true);
            var matrix = Gizmos.matrix;
            var color = Gizmos.color;
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
            for (var i = 0; i < boxes.Length; i++)
            {
                Gizmos.matrix = boxes[i].transform.localToWorldMatrix;
                Gizmos.DrawWireCube(boxes[i].center, boxes[i].size);
            }
            Gizmos.matrix = matrix;
            Gizmos.color = color;
        }
    }
}
