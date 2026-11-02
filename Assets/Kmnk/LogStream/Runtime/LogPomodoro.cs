using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace Kmnk.LogStream.Udon
{
    // ポモドーロのタイマー。状態と終わりの時刻を同期し、状態が移るたびにログを足す
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class LogPomodoro : LogWriteBase
    {
        [SerializeField]
        [Tooltip("ポモドーロの時間を分単位で指定します")]
        private int _pomodoroMinutes = 25;

        [SerializeField]
        [Tooltip("短い休憩の時間を分単位で指定します")]
        private int _shortBreakMinutes = 5;

        [SerializeField]
        [Tooltip("長い休憩の時間を分単位で指定します")]
        private int _longBreakMinutes = 15;

        [SerializeField]
        [Tooltip("長い休憩までのポモドーロ数を指定します")]
        private int _longBreakInterval = 4;

        [SerializeField]
        [Tooltip("タイマー終了後、自動で次のタイマーを開始するかを指定します")]
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
        [Tooltip("マスターのみ操作できるようにするかどうかを指定します")]
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
        private Button _toggleButton = null;

        [SerializeField]
        private Text _toggleButtonText = null;

        [SerializeField]
        private Button _skipButton = null;

        [SerializeField]
        private Image _skipButtonImage = null;

        [SerializeField]
        private Button _resetButton = null;

        [SerializeField]
        private Image _resetButtonImage = null;

        [SerializeField]
        private Text _currentStatusText = null;

        [SerializeField]
        private Text _timerText = null;

        [SerializeField]
        private Button _onlyMasterToggleButton = null;

        [SerializeField]
        private Image _onlyMasterToggleButtonImage = null;

        [SerializeField]
        private Sprite _lockedSprite = null;

        [SerializeField]
        private Sprite _unlockedSprite = null;

        [SerializeField]
        private Button _autoContinueToggleButton = null;

        [SerializeField]
        private Image _autoContinueToggleButtonImage = null;

        [UdonSynced]
        private int _pomodoroCount = 0;

        [UdonSynced]
        private PomodoroStatus _currentStatus = PomodoroStatus.NotStarted;

        [UdonSynced]
        private long _endDateTimeTicks = 0;

        [UdonSynced]
        private long _pausingRemainingSeconds = 0;

        [UdonSynced]
        private bool _syncedOnlyMaster = false;

        [UdonSynced]
        private bool _syncedAutoContinue = false;

        private Color _activeColor = new Color32(255, 255, 255, 255);
        private Color _inactiveColor = new Color32(128, 128, 128, 128);

        private DateTime _dateTime;

        private bool _wasOwner = false;

        private PomodoroStatus _prevStatus = PomodoroStatus.NotStarted;

        public void TogglePomodoro()
        {
            if (!CanOperate()) { return; }
            TakeOwnership();

            switch (_currentStatus)
            {
                case PomodoroStatus.NotStarted:
                case PomodoroStatus.ToPomodoro:
                    _currentStatus = PomodoroStatus.InPomodoro;
                    SetEndTimeAfterMinutes(_pomodoroMinutes);
                    ClearPausingRemainingSeconds();
                    AddTimerMessage(_startTimerLog, _pomodoroName, _pomodoroCount, _pomodoroMinutes);
                    break;

                case PomodoroStatus.InPomodoro:
                    _currentStatus = PomodoroStatus.PausedPomodoro;
                    SavePausingRemainingSeconds();
                    AddTimerMessage(_pauseTimerLog, _pomodoroName, 0, 0);
                    break;

                case PomodoroStatus.PausedPomodoro:
                    _currentStatus = PomodoroStatus.InPomodoro;
                    SetEndTimeAfterSeconds(_pausingRemainingSeconds);
                    ClearPausingRemainingSeconds();
                    AddTimerMessage(_resumeTimerLog, _pomodoroName, 0, 0);
                    break;

                case PomodoroStatus.ToBreak:
                    _currentStatus = PomodoroStatus.InBreak;
                    SetEndTimeAfterMinutes(NextBreakMinutes());
                    ClearPausingRemainingSeconds();
                    AddTimerMessage(_startTimerLog, _breakName, _pomodoroCount, NextBreakMinutes());
                    break;

                case PomodoroStatus.InBreak:
                    _currentStatus = PomodoroStatus.PausedBreak;
                    SavePausingRemainingSeconds();
                    AddTimerMessage(_pauseTimerLog, _breakName, 0, 0);
                    break;

                case PomodoroStatus.PausedBreak:
                    _currentStatus = PomodoroStatus.InBreak;
                    SetEndTimeAfterSeconds(_pausingRemainingSeconds);
                    ClearPausingRemainingSeconds();
                    AddTimerMessage(_resumeTimerLog, _breakName, 0, 0);
                    break;
            }

            ApplySyncedFields();
        }

        public void SkipPomodoro()
        {
            if (!CanOperate()) { return; }
            TakeOwnership();

            SavePausingRemainingSeconds();

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

        public void ResetPomodoro()
        {
            if (!CanOperate()) { return; }
            TakeOwnership();

            _currentStatus = PomodoroStatus.NotStarted;
            _pomodoroCount = 1;
            SetEndTimeAfterMinutes(_pomodoroMinutes);
            AddTimerMessage(_resetTimerLog, _pomodoroName, 0, 0);

            ApplySyncedFields();
        }

        public void ToggleOnlyMaster()
        {
            if (!CanOperate()) { return; }
            TakeOwnership();

            _syncedOnlyMaster = !_onlyMaster;

            ApplySyncedFields();
        }

        public void ToggleAutoContinue()
        {
            if (!CanOperate()) { return; }
            TakeOwnership();

            _syncedAutoContinue = !_autoContinue;

            ApplySyncedFields();
        }

        public override void OnDeserialization()
        {
            OnSyncedFieldsChanged();
        }

        private void Start()
        {
            _syncedOnlyMaster = _onlyMaster;
            _syncedAutoContinue = _autoContinue;
            SavePausingRemainingSeconds();
            SetEndTimeAfterMinutes(_pomodoroMinutes);

            UpdateInteractable();

            if (Networking.IsOwner(gameObject))
            {
                _wasOwner = true;
                _currentStatus = PomodoroStatus.NotStarted;
                _pomodoroCount = 1;
                SetEndTimeAfterMinutes(_pomodoroMinutes);
                ApplySyncedFields();
            }

            _dateTime = DateTime.UtcNow;
        }

        private void Update()
        {
            var isOwner = Networking.IsOwner(gameObject);
            if (_wasOwner != isOwner)
            {
                UpdateInteractable();
                _wasOwner = isOwner;
            }
            var now = DateTime.UtcNow;
            if (_dateTime.Second != now.Second && !IsWaiting())
            {
                if (isOwner) { AdvanceIfExpired(); }
                DisplayTimer();
            }
            _dateTime = now;
        }

        private bool CanOperate()
        {
            return !_onlyMaster || Networking.IsOwner(gameObject);
        }

        private void TakeOwnership()
        {
            if (!_wasOwner)
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
        }

        // 同期する値を送り、自分の表示にも反映する
        private void ApplySyncedFields()
        {
            RequestSerialization();
            OnSyncedFieldsChanged();
        }

        private void OnSyncedFieldsChanged()
        {
            if (_syncedOnlyMaster != _onlyMaster || _syncedAutoContinue != _autoContinue)
            {
                _onlyMaster = _syncedOnlyMaster;
                _autoContinue = _syncedAutoContinue;
                UpdateInteractable();
            }

            DisplayTimer();
            DisplayStatus();

            if (_prevStatus != _currentStatus)
            {
                PlaySoundIfTimeUp();
                _prevStatus = _currentStatus;
            }
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

        // owner が 1 秒ごとに呼ぶ。終わりの時刻を過ぎていれば次の状態へ移る
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
            _endDateTimeTicks = DateTime.UtcNow.AddSeconds(seconds).Ticks;
        }

        private long GetRemainingSeconds()
        {
            return (long)(new DateTime(_endDateTimeTicks) - DateTime.UtcNow).TotalSeconds;
        }

        private void SavePausingRemainingSeconds()
        {
            _pausingRemainingSeconds = GetRemainingSeconds();
        }

        private void ClearPausingRemainingSeconds()
        {
            _pausingRemainingSeconds = 0;
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

        private void DisplayTimer()
        {
            switch (_currentStatus)
            {
                case PomodoroStatus.NotStarted:
                case PomodoroStatus.ToPomodoro:
                    DisplayRemainingSeconds(_pomodoroMinutes * 60L);
                    break;

                case PomodoroStatus.ToBreak:
                    DisplayRemainingSeconds(NextBreakMinutes() * 60L);
                    break;

                case PomodoroStatus.PausedPomodoro:
                case PomodoroStatus.PausedBreak:
                    break;

                default:
                    DisplayRemainingSeconds(GetRemainingSeconds());
                    break;
            }
        }

        private void DisplayRemainingSeconds(long remainingSeconds)
        {
            var minutes = remainingSeconds / 60;
            var seconds = remainingSeconds - minutes * 60;
            _timerText.text = $"{minutes:00}:{seconds:00}";
        }

        private void DisplayStatus()
        {
            switch (_currentStatus)
            {
                case PomodoroStatus.NotStarted:
                case PomodoroStatus.ToPomodoro:
                    _currentStatusText.text = $"Next {_pomodoroName} #{_pomodoroCount}";
                    break;

                case PomodoroStatus.InPomodoro:
                case PomodoroStatus.PausedPomodoro:
                    _currentStatusText.text = $"{_pomodoroName} #{_pomodoroCount}";
                    break;

                case PomodoroStatus.ToBreak:
                    _currentStatusText.text = $"Next {_breakName} #{_pomodoroCount}";
                    break;

                case PomodoroStatus.InBreak:
                case PomodoroStatus.PausedBreak:
                    _currentStatusText.text = $"{_breakName} #{_pomodoroCount}";
                    break;
            }

            DisplayToggleButtonText();
        }

        private void UpdateInteractable()
        {
            var active = CanOperate();
            SetButtonActive(_toggleButton, null, active);
            _toggleButtonText.color = active ? _activeColor : _inactiveColor;
            SetButtonActive(_skipButton, _skipButtonImage, active);
            SetButtonActive(_resetButton, _resetButtonImage, active);
            SetButtonActive(_onlyMasterToggleButton, _onlyMasterToggleButtonImage, active);
            _onlyMasterToggleButtonImage.sprite = _onlyMaster ? _lockedSprite : _unlockedSprite;
            _autoContinueToggleButton.interactable = active;
            _autoContinueToggleButtonImage.color = active && _autoContinue ? _activeColor : _inactiveColor;
            DisplayToggleButtonText();
        }

        private void SetButtonActive(Button button, Image image, bool active)
        {
            button.interactable = active;
            if (image != null)
            {
                image.color = active ? _activeColor : _inactiveColor;
            }
        }

        private void DisplayToggleButtonText()
        {
            if (!CanOperate())
            {
                _toggleButtonText.text = "MASTER ONLY";
                return;
            }

            switch (_currentStatus)
            {
                case PomodoroStatus.InPomodoro:
                case PomodoroStatus.InBreak:
                    _toggleButtonText.text = "PAUSE";
                    break;

                default:
                    _toggleButtonText.text = "START";
                    break;
            }
        }
    }
}
