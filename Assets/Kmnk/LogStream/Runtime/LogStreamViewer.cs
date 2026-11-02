using System;
using System.Globalization;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace Kmnk.LogStream.Udon
{
    // LogStreamCore のログを、子の LogLine に新しい順に描く。_currentType が None でなければ、その型のログだけを描く
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LogStreamViewer : LogStreamEventListener
    {
        [SerializeField]
        private LogStreamCore _core = null;

        [SerializeField]
        private GameObject _logLinesParent = null;

        [SerializeField]
        private Text _dateText = null;

        [SerializeField]
        private Text _timeText = null;

        [SerializeField]
        private Text _playersCountText = null;

        private LogLine[] _logLines = null;

        private DateTime _dateTime;

        private LogType _currentType = LogType.None;

        private UdonSharpBehaviour[] _eventListeners = null;

        public override void OnDisplayAllLogLines()
        {
            DisplayAllLogLines();
        }

        public void ChangeType(LogType type)
        {
            _currentType = type;
            DisplayAllLogLines();
            InvokeOnChangeType();
        }

        public void AddEventListener(LogStreamViewerEventListener listener)
        {
            _eventListeners = ListenerArray.Append(_eventListeners, listener);
        }

        private void Start()
        {
            _logLines = _logLinesParent.GetComponentsInChildren<LogLine>();
            for (var i = 0; i < _logLines.Length; i++)
            {
                _logLines[i].Initialize();
            }

            _dateTime = DateTime.UtcNow;
            UpdateHeader();

            _core.AddEventListener(this);
        }

        private void Update()
        {
            var now = DateTime.UtcNow;
            if (_dateTime.Second != now.Second)
            {
                UpdateHeader();
            }
            _dateTime = now;
        }

        private void UpdateHeader()
        {
            var now = DateTime.UtcNow.ToLocalTime();
            _dateText.text = now.ToString("yyyy/MM/dd");
            _timeText.text = now.ToString("HH:mm:ss");
            _playersCountText.text = VRCPlayerApi.GetPlayerCount().ToString();
        }

        private bool HasAllLogLineReferences()
        {
            for (var i = 0; i < _logLines.Length; i++)
            {
                if (!_logLines[i].HasTextReferences()) { return false; }
            }
            return true;
        }

        private void DisplayAllLogLines()
        {
            if (!HasAllLogLineReferences()) { return; }
            if (!_core.AreSyncedFieldsInitialized()) { return; }

            // 別のビヘイビアへの呼び出しは遅いので、ループの前に 1 度だけ受け取る
            var logLimit = _core.GetLogLimit();
            var types = _core.GetTypes();
            var messages = _core.GetMessages();
            var names = _core.GetNames();
            var ticks = _core.GetTicks();
            var timeFormat = _core.GetTimeFormat();

            var logIndex = 0;
            for (var lineIndex = 0; lineIndex < _logLines.Length; lineIndex++)
            {
                while (logIndex < logLimit && _currentType != LogType.None && types[logIndex] != _currentType)
                {
                    logIndex++;
                }

                if (logIndex < logLimit)
                {
                    _logLines[lineIndex].Display(messages[logIndex], names[logIndex], FormatTicks(ticks[logIndex], timeFormat));
                    logIndex++;
                }
                else
                {
                    _logLines[lineIndex].Display(string.Empty, string.Empty, string.Empty);
                }
            }
        }

        private string FormatTicks(long ticks, string timeFormat)
        {
            if (ticks == 0) { return string.Empty; }
            return new DateTime(ticks).ToLocalTime().ToString(timeFormat, CultureInfo.InvariantCulture);
        }

        private void InvokeOnChangeType()
        {
            if (_eventListeners == null) { return; }
            for (var i = 0; i < _eventListeners.Length; i++)
            {
                ((LogStreamViewerEventListener)_eventListeners[i]).OnChangeType(_currentType);
            }
        }
    }
}
