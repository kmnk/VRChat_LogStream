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
        int _longBreakMinutes = 15;

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
        [Tooltip("タイマー開始のメッセージを設定します。 {{status}} にはポモドーロ or 休憩の名前が、 {{count}} にはポモドーロ数が、 {{m}} にはタイマーの分数が入ります")]
        string _startTimerLog = "<color='#ff9900'>[START]</color> {{status}} #{{count}} : {{m}} minutes";

        [SerializeField]
        [Tooltip("タイマー終了のメッセージを設定します。 {{status}} にはポモドーロ or 休憩の名前が、 {{count}} にはポモドーロ数が入ります")]
        string _endTimerLog = "<color='#ff9900'>[End]</color> {{status}} #{{count}}";

        [SerializeField]
        [Tooltip("タイマースキップのメッセージを設定します。 {{status}} にはポモドーロ or 休憩の名前が、 {{count}} にはポモドーロ数が入ります")]
        string _skipTimerLog = "<color='#ff9900'>[Skip]</color> {{status}} #{{count}}";

        [SerializeField]
        [Tooltip("タイマーリセットのメッセージを設定します。 {{status}} にはポモドーロの名前が入ります")]
        string _resetTimerLog = "<color='#ff9900'>[Reset]</color> {{status}}";
#pragma warning restore CS0414
    }
}