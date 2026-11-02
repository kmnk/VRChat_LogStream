using UdonSharp;

namespace Kmnk.LogStream.Udon
{
    // LogStreamCore のログが変わったことを受け取る側の基底。受け取る側はこれを継承して override する
    public abstract class LogStreamEventListener : UdonSharpBehaviour
    {
        public abstract void OnDisplayAllLogLines();
    }
}
