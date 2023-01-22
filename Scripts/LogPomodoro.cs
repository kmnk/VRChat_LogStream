using UnityEngine;

namespace Kmnk.LogStream
{
    public class LogPomodoro : MonoBehaviour
    {
#pragma warning disable CS0414
        [SerializeField]
        [Tooltip("複数の LogStream を設置する際に対象の LogStream の id を指定します")]
        int _id = 0;

        [SerializeField]
        [Tooltip("ポモドーロの時間を分単位で指定します")]
        int _pomodoroMinutes = 25;

        [SerializeField]
        [Tooltip("短い休憩の時間を分単位で指定します")]
        int _shortBreakMinutes = 5;

        [SerializeField]
        [Tooltip("長い休憩の時間を分単位で指定します")]
        int _longBreakMinutes = 30;

        [SerializeField]
        [Tooltip("長い休憩までのポモドーロ数を指定します")]
        int _longBreakInterval = 4;

        [SerializeField]
        [Tooltip("タイマー終了後、自動で次のタイマーを開始するかを指定します")]
        bool _autoContinue = false;

        [SerializeField]
        [Tooltip("自動で次のタイマーを開始するまでの間の秒数を指定します。 0 を指定しても最低 1 秒が間に挟まります")]
        int _autoContinueIntervalSeconds = 5;

        [SerializeField]
        [Tooltip("タイマーが 0 になった時に流す効果音を指定します")]
        AudioClip _soundEffectAudioClip = null;

        [SerializeField]
        [Tooltip("タイマーが 0 になった時に効果音を流す場合に ON にします")]
        bool _soundEffectEnabled = false;

        [SerializeField]
        [Tooltip("タイマーが 0 になった時の効果音の音量を指定します")]
        float _soundEffectGain = 10f;

        [SerializeField]
        [Tooltip("タイマーが 0 になった時の効果音が届く範囲を指定します")]
        float _soundEffectFar = 10f;

        [SerializeField]
        [Tooltip("マスターのみ操作できるようにするかどうかを指定します")]
        bool _onlyMaster = false;

        [SerializeField]
        [Tooltip("ポモドーロの名前を設定します")]
        string _pomodoroName = "<color='red'>Pomodoro</color>";

        [SerializeField]
        [Tooltip("短い休憩の名前を設定します")]
        string _breakName = "<color='green'>Break</color>";

        [SerializeField]
        [Tooltip("ポモドーロ開始のメッセージを設定します。 {0} には 1 ポモドーロ時間の分が、 {1} にはポモドーロ数が入ります（必須）")]
        string _startPomodoroLogFormat = "Start {0} minutes Pomodoro {1}";

        [SerializeField]
        [Tooltip("ポモドーロ終了のメッセージを設定します。 {0} にはポモドーロ数が入ります（必須）")]
        string _endPomodoroLogFormat = "End Pomodoro {0}";

        [SerializeField]
        [Tooltip("休憩開始のメッセージを設定します。 {0} には休憩時間の分が、 {1} にはポモドーロ数が入ります（必須）")]
        string _startBreakLogFormat = "Start {0} minutes Break {1}";

        [SerializeField]
        [Tooltip("休憩終了のメッセージを設定します。 {0} にはポモドーロ数が入ります（必須）")]
        string _endBreakLogFormat = "End Break {0}";
#pragma warning restore CS0414
    }
}