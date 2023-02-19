using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

using Kmnk.Core.Udon;

namespace Kmnk.LogStream.Udon
{
    public enum PomodoroStatus
    {
        NotStarted,
        ToPomodoro,
        InPomodoro,
        PausedPomodoro,
        ToBreak,
        InBreak,
        PausedBreak,
    }

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class LogPomodoro : LogWriteBase
    {
        LogType _type = LogType.Others;

        private Color _activeTextColor = new Color32(255, 255, 255, 255);
        private Color _inactiveTextColor = new Color32(128, 128, 128, 128);

        [SerializeField]
        int _pomodoroMinutes;

        [SerializeField]
        int _shortBreakMinutes;

        [SerializeField]
        int _longBreakMinutes;

        [SerializeField]
        int _longBreakInterval;

        [SerializeField]
        bool _autoContinue = false;

        [SerializeField]
        int _autoContinueIntervalSeconds = 5;

        [SerializeField]
        AudioSource _soundEffectAudioSource = null;

        [SerializeField]
        bool _soundEffectEnabled = false;

        [SerializeField]
        bool _onlyMaster = false;

        [SerializeField]
        string _pomodoroName;

        [SerializeField]
        string _breakName;

        [SerializeField]
        string _startTimerLogFormat;

        [SerializeField]
        string _endTimerLogFormat;

        [SerializeField]
        string _skipTimerLogFormat;

        [SerializeField]
        Button _toggleButton = null;

        [SerializeField]
        Text _toggleButtonText = null;

        [SerializeField]
        Button _skipButton = null;

        [SerializeField]
        Image _skipButtonImage = null;

        [SerializeField]
        Button _resetButton = null;

        [SerializeField]
        Image _resetButtonImage = null;

        [SerializeField]
        Text _currentStatusText = null;

        [SerializeField]
        Text _timerText = null;

        [UdonSynced]
        int _pomodoroCount = 0;

        [UdonSynced]
        PomodoroStatus _currentStatus = PomodoroStatus.NotStarted;

        [UdonSynced]
        private long _endDateTimeTicks;

        [UdonSynced]
        private long _pausingRemainingSeconds;

        private DateTime _dateTime;

        private bool _wasIOwner = false;

        private PomodoroStatus _prevStatus = PomodoroStatus.NotStarted;

        private void SetCurrentStatusText(string statusText)
        {
            _currentStatusText.text = statusText;
        }

        private void SetCurrentTimerTextBy(long remainingSeconds)
        {
            var minutes = remainingSeconds / 60;
            var seconds = remainingSeconds - (minutes * 60);
            _timerText.text = $"{minutes:00}:{seconds:00}";
        }

        private int NextBreakMinutes()
        {
            return _pomodoroCount % _longBreakInterval == 0
                ? _longBreakMinutes
                : _shortBreakMinutes;
        }

        private void IncrementPomodoroCount()
        {
            _pomodoroCount += 1;
        }

        private void ResetPomodoroCount()
        {
            _pomodoroCount = 1;
        }

        private bool IsPaused()
        {
            if (_currentStatus == PomodoroStatus.NotStarted) { return true; }
            if (_currentStatus == PomodoroStatus.ToPomodoro && !_autoContinue) { return true; }
            if (_currentStatus == PomodoroStatus.PausedPomodoro) { return true; }
            if (_currentStatus == PomodoroStatus.ToBreak && !_autoContinue) { return true; }
            if (_currentStatus == PomodoroStatus.PausedBreak) { return true; }
            return false;
        }

        private void ResetTimerBy(DateTime dateTime)
        {
            _endDateTimeTicks = dateTime.Ticks;
        }

        private void ResumeTimer()
        {
            ResetTimerBy(
                DateTime.UtcNow.AddSeconds(_pausingRemainingSeconds)
            );
        }

        private void ResetTimerToPomodoro()
        {
            ResetTimerBy(
                DateTime.UtcNow.AddSeconds(_pomodoroMinutes * 60)
            );
        }

        private void ResetTimerToBreak()
        {
            ResetTimerBy(
                DateTime.UtcNow.AddSeconds(NextBreakMinutes() * 60)
            );
        }

        private void ResetTimerInterval()
        {
            ResetTimerBy(
                DateTime.UtcNow.AddSeconds(
                    _autoContinueIntervalSeconds > 0
                        ? _autoContinueIntervalSeconds
                        : 1
                )
            );
        }

        private DateTime GetEndDateTime()
        {
            return new DateTime(_endDateTimeTicks);
        }

        private long GetRemainingSeconds()
        {
            return (long)((GetEndDateTime() - DateTime.UtcNow).TotalSeconds);
        }

        private void ResetPausingRemainingSeconds()
        {
            _pausingRemainingSeconds = GetRemainingSeconds();
        }

        private void ClaerPausingRemainingSeconds()
        {
            _pausingRemainingSeconds = 0L;
        }

        private void InitializeUdonSyncedFields()
        {
            _currentStatus = PomodoroStatus.NotStarted;
            ResetPomodoroCount();
            ResetTimerToPomodoro();
        }

        private void Start()
        {
            ResetPausingRemainingSeconds();
            ResetTimerBy(DateTime.UtcNow.AddSeconds(_pomodoroMinutes * 60));

            ResetButtonActive();

            if (Util.AmIOwner(gameObject))
            {
                _wasIOwner = true;
                InitializeUdonSyncedFields();
                RequestSerialization();
                OnUdonSyncedFieldsChange();
            }

            _dateTime = DateTime.UtcNow;
        }

        private void Update()
        {
            if (_wasIOwner != Util.AmIOwner(gameObject))
            {
                ResetButtonActive();
                _wasIOwner = Util.AmIOwner(gameObject);
            }
            if (_dateTime.Second != DateTime.UtcNow.Second && !IsPaused())
            {
                if (Util.AmIOwner(gameObject)) { HandleTimer(); }
                DisplayTimer();
            }
            _dateTime = DateTime.UtcNow;
        }

        public override void OnDeserialization()
        {
            base.OnDeserialization();
            OnUdonSyncedFieldsChange();
        }

        private void OnUdonSyncedFieldsChange()
        {
            DisplayTimer();
            DisplayStatus();
            if (_prevStatus != _currentStatus)
            {
                OnStatusChange();
                _prevStatus = _currentStatus;
            }
        }

        public void TogglePomodoro()
        {
            if (_onlyMaster && !Util.AmIOwner(gameObject)) { return; }
            if (!_onlyMaster && !_wasIOwner) { Util.TakeOwner(gameObject); }

            switch (_currentStatus)
            {
                case PomodoroStatus.NotStarted:
                case PomodoroStatus.ToPomodoro:
                    _currentStatus = PomodoroStatus.InPomodoro;
                    ResetTimerToPomodoro();
                    ClaerPausingRemainingSeconds();
                    AddPomodoroStartMessage();
                    break;

                case PomodoroStatus.InPomodoro:
                    _currentStatus = PomodoroStatus.PausedPomodoro;
                    ResetPausingRemainingSeconds();
                    break;

                case PomodoroStatus.PausedPomodoro:
                    _currentStatus = PomodoroStatus.InPomodoro;
                    ResumeTimer();
                    ClaerPausingRemainingSeconds();
                    break;

                case PomodoroStatus.ToBreak:
                    _currentStatus = PomodoroStatus.InBreak;
                    ResetTimerToBreak();
                    ClaerPausingRemainingSeconds();
                    AddBreakStartMessage();
                    break;

                case PomodoroStatus.InBreak:
                    _currentStatus = PomodoroStatus.PausedBreak;
                    ResetPausingRemainingSeconds();
                    break;

                case PomodoroStatus.PausedBreak:
                    _currentStatus = PomodoroStatus.InBreak;
                    ResumeTimer();
                    ClaerPausingRemainingSeconds();
                    break;
            }

            RequestSerialization();
            OnUdonSyncedFieldsChange();
        }

        public void SkipPomodoro()
        {
            if (_onlyMaster && !Util.AmIOwner(gameObject)) { return; }
            if (!_onlyMaster && !_wasIOwner) { Util.TakeOwner(gameObject); }

            ResetPausingRemainingSeconds();

            switch (_currentStatus)
            {
                case PomodoroStatus.NotStarted:
                case PomodoroStatus.ToPomodoro:
                case PomodoroStatus.InPomodoro:
                case PomodoroStatus.PausedPomodoro:
                    _currentStatus = PomodoroStatus.ToBreak;
                    ResetTimerToBreak();
                    AddPomodoroSkipMessage();
                    break;

                case PomodoroStatus.ToBreak:
                case PomodoroStatus.InBreak:
                case PomodoroStatus.PausedBreak:
                    _currentStatus = PomodoroStatus.ToPomodoro;
                    IncrementPomodoroCount();
                    ResetTimerToPomodoro();
                    AddBreakSkipMessage();
                    break;
            }

            RequestSerialization();
            OnUdonSyncedFieldsChange();
        }

        public void ResetPomodoro()
        {
            if (_onlyMaster && !Util.AmIOwner(gameObject)) { return; }
            if (!_onlyMaster && !_wasIOwner) { Util.TakeOwner(gameObject); }

            _currentStatus = PomodoroStatus.NotStarted;
            ResetPomodoroCount();
            ResetTimerToPomodoro();

            RequestSerialization();
            OnUdonSyncedFieldsChange();
        }

        private void HandleTimer()
        {
            if (!_autoContinue
                && _currentStatus != PomodoroStatus.InPomodoro
                && _currentStatus != PomodoroStatus.InBreak)
            {
                return;
            } else if (_autoContinue
                && _currentStatus != PomodoroStatus.ToPomodoro
                && _currentStatus != PomodoroStatus.InPomodoro
                && _currentStatus != PomodoroStatus.ToBreak
                && _currentStatus != PomodoroStatus.InBreak)
            {
                return;
            }

            if (GetRemainingSeconds() > 0) { return; }

            if (_currentStatus == PomodoroStatus.InPomodoro)
            {
                _currentStatus = PomodoroStatus.ToBreak;
                if (!_autoContinue)
                {
                    ResetTimerToBreak();
                }
                else
                {
                    ResetTimerInterval();
                }
                AddPomodoroEndMessage();
            }
            else if (_currentStatus == PomodoroStatus.InBreak)
            {
                _currentStatus = PomodoroStatus.ToPomodoro;
                IncrementPomodoroCount();
                if (!_autoContinue)
                {
                    ResetTimerToPomodoro();
                }
                else
                {
                    ResetTimerInterval();
                }
                AddBreakEndMessage();
            }
            else if (_currentStatus == PomodoroStatus.ToPomodoro)
            {
                _currentStatus = PomodoroStatus.InPomodoro;
                ResetTimerToPomodoro();
                AddPomodoroStartMessage();
            }
            else if (_currentStatus == PomodoroStatus.ToBreak)
            {
                _currentStatus = PomodoroStatus.InBreak;
                ResetTimerToBreak();
                AddBreakStartMessage();
            }

            RequestSerialization();
            OnUdonSyncedFieldsChange();
        }

        private void DisplayTimer()
        {
            switch(_currentStatus)
            {
                case PomodoroStatus.NotStarted:
                case PomodoroStatus.ToPomodoro:
                    SetCurrentTimerTextBy(_pomodoroMinutes * 60);
                    break;

                case PomodoroStatus.ToBreak:
                    SetCurrentTimerTextBy(NextBreakMinutes() * 60);
                    break;

                default:
                    SetCurrentTimerTextBy(GetRemainingSeconds());
                    break;
            }
        }

        private void DisplayStatus()
        {
            switch(_currentStatus)
            {
                case PomodoroStatus.NotStarted:
                case PomodoroStatus.ToPomodoro:
                    SetCurrentStatusText($"Next {_pomodoroName} #{_pomodoroCount}");
                    break;

                case PomodoroStatus.InPomodoro:
                case PomodoroStatus.PausedPomodoro:
                    SetCurrentStatusText($"{_pomodoroName} #{_pomodoroCount}");
                    break;

                case PomodoroStatus.ToBreak:
                    SetCurrentStatusText($"Next {_breakName} #{_pomodoroCount}");
                    break;

                case PomodoroStatus.InBreak:
                case PomodoroStatus.PausedBreak:
                    SetCurrentStatusText($"{_breakName} #{_pomodoroCount}");
                    break;
            }

            switch(_currentStatus)
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

        private void OnStatusChange()
        {
            if ((_prevStatus == PomodoroStatus.InPomodoro || _prevStatus == PomodoroStatus.PausedPomodoro)
                && _currentStatus == PomodoroStatus.ToBreak)
            {
                PlaySoundEffect();
            }
            else if ((_prevStatus == PomodoroStatus.InBreak || _prevStatus == PomodoroStatus.PausedBreak)
                && _currentStatus == PomodoroStatus.ToPomodoro)
            {
                PlaySoundEffect();
            }
        }

        private void PlaySoundEffect()
        {
            if (!_soundEffectEnabled) { return; }
            if (_soundEffectAudioSource == null) { return; }
            _soundEffectAudioSource.Play();
        }

        private void ResetButtonActive()
        {
            var isActive = !_onlyMaster || Util.AmIOwner(gameObject);
            SetToggleButtonActive(isActive);
            SetSkipButtonActive(isActive);
            SetResetButtonActive(isActive);
        }

        private void SetToggleButtonActive(bool active)
        {
            _toggleButton.interactable = active;
            _toggleButtonText.color
                = active ? _activeTextColor : _inactiveTextColor;
        }

        private void SetSkipButtonActive(bool active)
        {
            _skipButton.interactable = active;
            _skipButtonImage.color
                = active ? _activeTextColor : _inactiveTextColor;
        }

        private void SetResetButtonActive(bool active)
        {
            _resetButton.interactable = active;
            _resetButtonImage.color
                = active ? _activeTextColor : _inactiveTextColor;
        }

        private void AddPomodoroStartMessage()
        {
            AddMessage(
                _type,
                string.Format(
                    _startTimerLogFormat,
                    _pomodoroName,
                    _pomodoroCount,
                    _pomodoroMinutes
                ),
                ""
            );
        }

        private void AddBreakStartMessage()
        {
            AddMessage(
                _type,
                string.Format(
                    _startTimerLogFormat,
                    _breakName,
                    _pomodoroCount,
                    NextBreakMinutes()
                ),
                ""
            );
        }

        private void AddPomodoroEndMessage()
        {
            AddMessage(
                _type,
                string.Format(
                    _endTimerLogFormat,
                    _pomodoroName,
                    _pomodoroCount
                ),
                ""
            );
        }

        private void AddBreakEndMessage()
        {
            AddMessage(
                _type,
                string.Format(
                    _endTimerLogFormat,
                    _breakName,
                    _pomodoroCount - 1
                ),
                ""
            );
        }

        private void AddPomodoroSkipMessage()
        {
            AddMessage(
                _type,
                string.Format(
                    _skipTimerLogFormat,
                    _pomodoroName,
                    _pomodoroCount
                ),
                ""
            );
        }

        private void AddBreakSkipMessage()
        {
            AddMessage(
                _type,
                string.Format(
                    _skipTimerLogFormat,
                    _breakName,
                    _pomodoroCount - 1
                ),
                ""
            );
        }
    }
}