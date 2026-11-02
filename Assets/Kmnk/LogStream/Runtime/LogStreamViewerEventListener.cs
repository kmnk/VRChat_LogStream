using UdonSharp;

namespace Kmnk.LogStream.Udon
{
    // LogStreamViewer の絞り込みが変わったことを受け取る側の基底。受け取る側はこれを継承して override する
    public abstract class LogStreamViewerEventListener : UdonSharpBehaviour
    {
        public abstract void OnChangeType(LogType type);
    }
}
