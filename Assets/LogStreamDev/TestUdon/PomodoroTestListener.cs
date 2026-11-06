using UdonSharp;

// テストが LogPomodoro の状態の通知を数えるための U# スクリプト。Register で登録し、通知のたびに回数と読んだ状態を残す
public class PomodoroTestListener : Kmnk.LogStream.Udon.LogPomodoroEventListener
{
    public Kmnk.LogStream.Udon.LogPomodoro pomodoro;
    public int count;
    public int lastStatus = -1;
    public bool lastRunning;

    public void Register()
    {
        pomodoro.AddEventListener(this);
    }

    public override void OnPomodoroStatusChanged()
    {
        count++;
        lastStatus = (int)pomodoro.GetStatus();
        lastRunning = pomodoro.IsTimerRunning();
    }
}
