using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class LogSample : UdonSharpBehaviour
{
    private Kmnk.LogStream.Udon.LogType _type = Kmnk.LogStream.Udon.LogType.Others;

    [SerializeField]
    [Tooltip("ワールドに設置した LogStream Prefab の Udon オブジェクトをドラッグ＆ドロップしてください")]
    private Kmnk.LogStream.Udon.LogStreamCore _core = null;

    // UdonSharpBehaviour に予め用意されている、 Use した時に呼ばれる 関数です
    public override void Interact()
    {
        // _core に LogStream をセットし忘れていた時に止めるための行
        if (_core == null) { return; }
        _core.AddMessage(
            _type,
            "Use LogSample by",
            Networking.LocalPlayer.displayName
        );
    }
}
