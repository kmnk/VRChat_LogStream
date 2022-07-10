using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class LogSample : UdonSharpBehaviour
{
    string _type = "SAMPLE";

    [SerializeField]
    [Tooltip("ワールドに設置した LogStream Prefab の Udon オブジェクトをドラッグ＆ドロップしてください")]
    Kmnk.LogStream.Udon.LogStream _logStream = null;

    // UdonSharpBehaviour に予め用意されている、 Use した時に呼ばれる 関数です
    public override void Interact()
    {
        // _logStream に LogStream をセットし忘れていた時に止めるための行
        if (_logStream == null) { return; }
        _logStream.AddMessage(
            _type,
            "Use LogSample by",
            Networking.LocalPlayer.displayName
        );
    }
}
