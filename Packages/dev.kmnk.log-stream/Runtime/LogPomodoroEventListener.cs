using UdonSharp;

namespace Kmnk.LogStream.Udon
{
    // LogPomodoro の状態が変わったことを受け取る側の基底。受け取る側はこれを継承して override し、状態は LogPomodoro から読む
    public abstract class LogPomodoroEventListener : UdonSharpBehaviour
    {
        public abstract void OnPomodoroStatusChanged();
    }
}
