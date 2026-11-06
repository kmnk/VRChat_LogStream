using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Kmnk.LogStream.Udon
{
    // ポモドーロのタイマー。状態と終わりの時刻を同期し、状態が移るたびにログを足す。表示は LogPomodoroView が描く
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class LogPomodoro : LogWriteBase
    {
        [SerializeField]
        [Tooltip("ポモドーロの時間を分単位で指定します")]
        [Min(1)]
        private int _pomodoroMinutes = 25;

        [SerializeField]
        [Tooltip("短い休憩の時間を分単位で指定します")]
        [Min(1)]
        private int _shortBreakMinutes = 5;

        [SerializeField]
        [Tooltip("長い休憩の時間を分単位で指定します")]
        [Min(1)]
        private int _longBreakMinutes = 15;

        [SerializeField]
        [Tooltip("長い休憩までのポモドーロ数を指定します")]
        [Min(1)]
        private int _longBreakInterval = 4;

        [SerializeField]
        [Tooltip("タイマー終了後、自動で次のタイマーを開始するかを指定します")]
        [UdonSynced]
        private bool _autoContinue = false;

        [SerializeField]
        [Tooltip("自動で次のタイマーを開始するまでの間の秒数を指定します。 0 を指定しても最低 1 秒が間に挟まります")]
        private int _autoContinueIntervalSeconds = 5;

        [SerializeField]
        private AudioSource _soundEffectAudioSource = null;

        [SerializeField]
        [Tooltip("タイマーが 0 になった時に効果音を流す場合に ON にします")]
        private bool _soundEffectEnabled = false;

        [SerializeField]
        [Tooltip("インスタンスのマスターのみ操作できるようにするかどうかを指定します")]
        [UdonSynced]
        private bool _onlyMaster = false;

        [SerializeField]
        [Tooltip("ポモドーロの名前を設定します")]
        private string _pomodoroName = "<color='red'>Pomodoro</color>";

        [SerializeField]
        [Tooltip("休憩の名前を設定します")]
        private string _breakName = "<color='green'>Break</color>";

        [SerializeField]
        [Tooltip("タイマー開始のメッセージを設定します。 {{status}} にはポモドーロ or 休憩の名前が、 {{count}} にはポモドーロ数が、 {{m}} にはタイマーの分数が入ります")]
        private string _startTimerLog = "<color='#ff9900'>[START]</color> {{status}} #{{count}} : {{m}} minutes";

        [SerializeField]
        [Tooltip("タイマー終了のメッセージを設定します。 {{status}} にはポモドーロ or 休憩の名前が、 {{count}} にはポモドーロ数が入ります")]
        private string _endTimerLog = "<color='#ff9900'>[END]</color> {{status}} #{{count}}";

        [SerializeField]
        [Tooltip("タイマースキップのメッセージを設定します。 {{status}} にはポモドーロ or 休憩の名前が、 {{count}} にはポモドーロ数が入ります")]
        private string _skipTimerLog = "<color='#ff9900'>[SKIP]</color> {{status}} #{{count}}";

        [SerializeField]
        [Tooltip("タイマーリセットのメッセージを設定します。 {{status}} にはポモドーロの名前が入ります")]
        private string _resetTimerLog = "<color='#ff9900'>[RESET]</color> {{status}}";

        [SerializeField]
        [Tooltip("タイマーポーズのメッセージを設定します。 {{status}} にはポモドーロ or 休憩の名前が入ります")]
        private string _pauseTimerLog = "<color='#ff9900'>[PAUSE]</color> {{status}}";

        [SerializeField]
        [Tooltip("タイマー再開のメッセージを設定します。 {{status}} にはポモドーロ or 休憩の名前が入ります")]
        private string _resumeTimerLog = "<color='#ff9900'>[RESUME]</color> {{status}}";

        [SerializeField]
        private LogPomodoroView _view = null;

        [UdonSynced]
        private int _pomodoroCount = 1;

        [UdonSynced]
        private PomodoroStatus _currentStatus = PomodoroStatus.NotStarted;

        [UdonSynced]
        private long _endDateTimeTicks = 0;

        [UdonSynced]
        private long _pausingRemainingSeconds = 0;

        private PomodoroStatus _prevStatus = PomodoroStatus.NotStarted;

        private UdonSharpBehaviour[] _eventListeners = null;

        // 状態が変わるたびに listener.OnPomodoroStatusChanged を呼ぶよう登録する
        public void AddEventListener(LogPomodoroEventListener listener)
        {
            _eventListeners = ListenerArray.Append(_eventListeners, listener);
        }

        public PomodoroStatus GetStatus()
        {
            return _currentStatus;
        }

        // 作業のタイマーが動いているか。一時停止・休憩・始まる前・次を待つ間は含めない
        public bool IsTimerRunning()
        {
            return _currentStatus == PomodoroStatus.InPomodoro;
        }

        public void _TogglePomodoro()
        {
            if (!CanOperate()) { return; }
            TakeOwnership();

            switch (_currentStatus)
            {
                case PomodoroStatus.NotStarted:
                case PomodoroStatus.ToPomodoro:
                    _currentStatus = PomodoroStatus.InPomodoro;
                    SetEndTimeAfterMinutes(_pomodoroMinutes);
                    _pausingRemainingSeconds = 0;
                    AddTimerMessage(_startTimerLog, _pomodoroName, _pomodoroCount, _pomodoroMinutes);
                    break;

                case PomodoroStatus.InPomodoro:
                    _currentStatus = PomodoroStatus.PausedPomodoro;
                    _pausingRemainingSeconds = GetRemainingSeconds();
                    AddTimerMessage(_pauseTimerLog, _pomodoroName, 0, 0);
                    break;

                case PomodoroStatus.PausedPomodoro:
                    _currentStatus = PomodoroStatus.InPomodoro;
                    SetEndTimeAfterSeconds(_pausingRemainingSeconds);
                    _pausingRemainingSeconds = 0;
                    AddTimerMessage(_resumeTimerLog, _pomodoroName, 0, 0);
                    break;

                case PomodoroStatus.ToBreak:
                    _currentStatus = PomodoroStatus.InBreak;
                    SetEndTimeAfterMinutes(NextBreakMinutes());
                    _pausingRemainingSeconds = 0;
                    AddTimerMessage(_startTimerLog, _breakName, _pomodoroCount, NextBreakMinutes());
                    break;

                case PomodoroStatus.InBreak:
                    _currentStatus = PomodoroStatus.PausedBreak;
                    _pausingRemainingSeconds = GetRemainingSeconds();
                    AddTimerMessage(_pauseTimerLog, _breakName, 0, 0);
                    break;

                case PomodoroStatus.PausedBreak:
                    _currentStatus = PomodoroStatus.InBreak;
                    SetEndTimeAfterSeconds(_pausingRemainingSeconds);
                    _pausingRemainingSeconds = 0;
                    AddTimerMessage(_resumeTimerLog, _breakName, 0, 0);
                    break;
            }

            ApplySyncedFields();
        }

        public void _SkipPomodoro()
        {
            if (!CanOperate()) { return; }
            TakeOwnership();

            _pausingRemainingSeconds = GetRemainingSeconds();

            switch (_currentStatus)
            {
                case PomodoroStatus.NotStarted:
                case PomodoroStatus.ToPomodoro:
                case PomodoroStatus.InPomodoro:
                case PomodoroStatus.PausedPomodoro:
                    _currentStatus = PomodoroStatus.ToBreak;
                    SetEndTimeAfterMinutes(NextBreakMinutes());
                    AddTimerMessage(_skipTimerLog, _pomodoroName, _pomodoroCount, 0);
                    break;

                case PomodoroStatus.ToBreak:
                case PomodoroStatus.InBreak:
                case PomodoroStatus.PausedBreak:
                    _currentStatus = PomodoroStatus.ToPomodoro;
                    _pomodoroCount++;
                    SetEndTimeAfterMinutes(_pomodoroMinutes);
                    AddTimerMessage(_skipTimerLog, _breakName, _pomodoroCount - 1, 0);
                    break;
            }

            ApplySyncedFields();
        }

        public void _ResetPomodoro()
        {
            if (!CanOperate()) { return; }
            TakeOwnership();

            _currentStatus = PomodoroStatus.NotStarted;
            _pomodoroCount = 1;
            SetEndTimeAfterMinutes(_pomodoroMinutes);
            AddTimerMessage(_resetTimerLog, _pomodoroName, 0, 0);

            ApplySyncedFields();
        }

        public void _ToggleOnlyMaster()
        {
            if (!CanOperate()) { return; }
            TakeOwnership();
            _onlyMaster = !_onlyMaster;
            ApplySyncedFields();
        }

        public void _ToggleAutoContinue()
        {
            if (!CanOperate()) { return; }
            TakeOwnership();
            _autoContinue = !_autoContinue;
            ApplySyncedFields();
        }

        // 1 秒ごとに、owner は時間切れなら次の状態へ移し、全員が残りの時間を描き直す。次の秒の変わり目に呼ばれるよう予約する
        public void _Tick()
        {
            if (!IsWaiting())
            {
                if (Networking.IsOwner(gameObject)) { AdvanceIfExpired(); }
                if (_view != null) { _view.ShowTimer(TimerSeconds()); }
            }
            SendCustomEventDelayedSeconds(nameof(_Tick), (1000 - Networking.GetNetworkDateTime().Millisecond) / 1000f + 0.02f);
        }

        public override void OnDeserialization()
        {
            OnSyncedFieldsChanged();
        }

        // マスターのみの間は、マスターでない人への所有権の移動を拒む
        public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
        {
            return !_onlyMaster || (Utilities.IsValid(requestedOwner) && requestedOwner.isMaster);
        }

        // マスターが抜けると別の人がマスターになるので、操作できるかを描き直す
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            Render();
        }

        private void Start()
        {
            _pomodoroMinutes = Mathf.Max(1, _pomodoroMinutes);
            _shortBreakMinutes = Mathf.Max(1, _shortBreakMinutes);
            _longBreakMinutes = Mathf.Max(1, _longBreakMinutes);
            _longBreakInterval = Mathf.Max(1, _longBreakInterval);

            if (Networking.IsOwner(gameObject))
            {
                _currentStatus = PomodoroStatus.NotStarted;
                _pomodoroCount = 1;
                SetEndTimeAfterMinutes(_pomodoroMinutes);
                RequestSerialization();
            }
            Render();
            _Tick();
        }

        private bool CanOperate()
        {
            var player = Networking.LocalPlayer;
            return !_onlyMaster || (Utilities.IsValid(player) && player.isMaster);
        }

        private void TakeOwnership()
        {
            if (Networking.IsOwner(gameObject)) { return; }
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        // 同期する値を送り、自分の表示にも反映する
        private void ApplySyncedFields()
        {
            RequestSerialization();
            OnSyncedFieldsChanged();
        }

        private void OnSyncedFieldsChanged()
        {
            Render();
            if (_prevStatus != _currentStatus)
            {
                PlaySoundIfTimeUp();
                _prevStatus = _currentStatus;
                NotifyStatusChanged();
            }
        }

        private void NotifyStatusChanged()
        {
            if (_eventListeners == null) { return; }
            for (var i = 0; i < _eventListeners.Length; i++)
            {
                ((LogPomodoroEventListener)_eventListeners[i]).OnPomodoroStatusChanged();
            }
        }

        private void Render()
        {
            if (_view == null) { return; }
            _view.Show(_currentStatus, _pomodoroCount, TimerSeconds(), CanOperate(), _onlyMaster, _autoContinue, _pomodoroName, _breakName);
        }

        // 時間が進まない状態か。自動で続けるときは、次を待つ間も時間が進む
        private bool IsWaiting()
        {
            switch (_currentStatus)
            {
                case PomodoroStatus.NotStarted:
                case PomodoroStatus.PausedPomodoro:
                case PomodoroStatus.PausedBreak:
                    return true;
                case PomodoroStatus.ToPomodoro:
                case PomodoroStatus.ToBreak:
                    return !_autoContinue;
                default:
                    return false;
            }
        }

        // 終わりの時刻を過ぎていれば次の状態へ移る
        private void AdvanceIfExpired()
        {
            if (IsWaiting()) { return; }
            if (GetRemainingSeconds() > 0) { return; }

            switch (_currentStatus)
            {
                case PomodoroStatus.InPomodoro:
                    _currentStatus = PomodoroStatus.ToBreak;
                    if (_autoContinue) { SetEndTimeAfterAutoContinueInterval(); }
                    else { SetEndTimeAfterMinutes(NextBreakMinutes()); }
                    AddTimerMessage(_endTimerLog, _pomodoroName, _pomodoroCount, 0);
                    break;

                case PomodoroStatus.InBreak:
                    _currentStatus = PomodoroStatus.ToPomodoro;
                    _pomodoroCount++;
                    if (_autoContinue) { SetEndTimeAfterAutoContinueInterval(); }
                    else { SetEndTimeAfterMinutes(_pomodoroMinutes); }
                    AddTimerMessage(_endTimerLog, _breakName, _pomodoroCount - 1, 0);
                    break;

                case PomodoroStatus.ToPomodoro:
                    _currentStatus = PomodoroStatus.InPomodoro;
                    SetEndTimeAfterMinutes(_pomodoroMinutes);
                    AddTimerMessage(_startTimerLog, _pomodoroName, _pomodoroCount, _pomodoroMinutes);
                    break;

                case PomodoroStatus.ToBreak:
                    _currentStatus = PomodoroStatus.InBreak;
                    SetEndTimeAfterMinutes(NextBreakMinutes());
                    AddTimerMessage(_startTimerLog, _breakName, _pomodoroCount, NextBreakMinutes());
                    break;
            }

            ApplySyncedFields();
        }

        // 表示する残りの秒数。始まる前は次のタイマーの長さ、一時停止中は止めた時の残り
        private long TimerSeconds()
        {
            switch (_currentStatus)
            {
                case PomodoroStatus.NotStarted:
                case PomodoroStatus.ToPomodoro:
                    return _pomodoroMinutes * 60L;
                case PomodoroStatus.ToBreak:
                    return NextBreakMinutes() * 60L;
                case PomodoroStatus.PausedPomodoro:
                case PomodoroStatus.PausedBreak:
                    return _pausingRemainingSeconds;
                default:
                    return GetRemainingSeconds();
            }
        }

        private int NextBreakMinutes()
        {
            return _pomodoroCount % _longBreakInterval == 0 ? _longBreakMinutes : _shortBreakMinutes;
        }

        private void SetEndTimeAfterMinutes(int minutes)
        {
            SetEndTimeAfterSeconds(minutes * 60L);
        }

        private void SetEndTimeAfterAutoContinueInterval()
        {
            SetEndTimeAfterSeconds(_autoContinueIntervalSeconds > 0 ? _autoContinueIntervalSeconds : 1);
        }

        private void SetEndTimeAfterSeconds(long seconds)
        {
            _endDateTimeTicks = Networking.GetNetworkDateTime().AddSeconds(seconds).Ticks;
        }

        private long GetRemainingSeconds()
        {
            return (long)(new DateTime(_endDateTimeTicks) - Networking.GetNetworkDateTime()).TotalSeconds;
        }

        // template の {{status}}・{{count}}・{{m}} を埋めたログを、名前の欄を空にして足す
        private void AddTimerMessage(string template, string statusName, int count, int minutes)
        {
            var message = template
                .Replace("{{status}}", statusName)
                .Replace("{{count}}", count.ToString())
                .Replace("{{m}}", minutes.ToString());
            AddMessage(LogType.Others, message, string.Empty);
        }

        private void PlaySoundIfTimeUp()
        {
            var pomodoroEnded = (_prevStatus == PomodoroStatus.InPomodoro || _prevStatus == PomodoroStatus.PausedPomodoro)
                && _currentStatus == PomodoroStatus.ToBreak;
            var breakEnded = (_prevStatus == PomodoroStatus.InBreak || _prevStatus == PomodoroStatus.PausedBreak)
                && _currentStatus == PomodoroStatus.ToPomodoro;
            if (!pomodoroEnded && !breakEnded) { return; }
            if (!_soundEffectEnabled) { return; }
            if (_soundEffectAudioSource == null) { return; }
            _soundEffectAudioSource.Play();
        }
    }
}
