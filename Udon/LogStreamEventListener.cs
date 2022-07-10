using UdonSharp;

namespace Kmnk.LogStream.Udon
{
    public abstract class LogStreamEventListener : UdonSharpBehaviour
    {
        public void OnDisplayAllLogLines() {}

        public void OnChangeType(string type) {}
    }
}