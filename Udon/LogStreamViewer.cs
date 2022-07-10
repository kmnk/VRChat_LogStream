using System;
using System.Globalization;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace Kmnk.LogStream.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LogStreamViewer : UdonSharpBehaviour
    {
        [SerializeField]
        string _timeFormat = "HH:mm:ss";

        [SerializeField]
        Udon.LogStream _logStream = null;

        [SerializeField]
        GameObject _logLinesParent = null;

        [SerializeField]
        Text _dateText = null;

        [SerializeField]
        Text _timeText = null;

        [SerializeField]
        Text _playersCountText = null;


        int _logLimit = 100;
        private string[] _types = null;
        private long[] _ticks = null;
        private string[] _names = null;
        private string[] _messages = null;

        private Udon.LogLine[] _logLines = null;

        private DateTime _dateTime;

        private string _currentType = "";

        void Start()
        {
            _logLines = _logLinesParent.GetComponentsInChildren<LogLine>();
            InitializeLogLines();

            _logStream.AddEventListener(this);

            _dateTime = DateTime.UtcNow;
            UpdateHeader();
        }

        private void UpdateHeader()
        {
            var d = DateTime.UtcNow;
            _dateText.text = d.ToLocalTime().ToString("yyyy/MM/dd");
            _timeText.text = d.ToLocalTime().ToString("HH:mm:ss");
            _playersCountText.text = VRCPlayerApi.GetPlayerCount().ToString();
        }

        private void Update() {
            if (_dateTime.Second != DateTime.UtcNow.Second)
            {
                UpdateHeader();
            }
        }

        private void InitializeLogLines()
        {
            foreach (var l in _logLines)
            {
                l.Initialize();
            }
        }

        public void OnDisplayAllLogLines()
        {
            _logLimit = _logStream.GetLogLimit();

            _types = _logStream.GetTypes();
            _messages = _logStream.GetMessages();
            _names = _logStream.GetNames();
            _ticks = _logStream.GetTicks();

            DisplayAllLogLines();
        }

        private bool HasAllLogLinesInitialized()
        {
            foreach (var l in _logLines)
            {
                if (!l.HasInitialized()) { return false; }
            }
            return true;
        }

        private void DisplayAllLogLines()
        {
            if (!HasAllLogLinesInitialized()) { return; }

            // maybe can write cooler
            var logIndex = -1;
            for (var i = 0; i < _logLines.Length; i++)
            {
                for (++logIndex; logIndex < _logLimit; logIndex++)
                {
                    if (_currentType == "" || _types[logIndex] == _currentType) { break; }
                }
                DisplayLogLine(i, logIndex < _logLimit ? logIndex : -1);
            }
        }

        private void DisplayLogLine(int lineIndex, int logIndex)
        {
            _logLines[lineIndex].Display(
                logIndex >= 0 ? _messages[logIndex] : string.Empty,
                logIndex >= 0 ? _names[logIndex] : string.Empty,
                FormatTicks(logIndex >= 0 ? _ticks[logIndex] : 0)
            );
        }

        public void ChangeType(string type)
        {
            _currentType = type;        
            DisplayAllLogLines();
            if (_evnetListeners != null)
            {
                foreach (var listener in _evnetListeners)
                {
                    listener.OnChangeType(_currentType);
                }
            }
        }

        private string FormatTicks(long ticks)
        {
            if (ticks == 0) { return string.Empty; }
            return (new DateTime(ticks))
                .ToLocalTime()
                .ToString(_timeFormat, CultureInfo.InvariantCulture);
        }

        private LogStreamEventListener[] _evnetListeners;
        public void AddEventListener(UdonSharpBehaviour listener)
        {
            if (_evnetListeners == null)
            {
                _evnetListeners
                    = new LogStreamEventListener[]
                    {
                         (LogStreamEventListener)listener
                    };
            }
            else
            {
                var newArray
                    = new LogStreamEventListener[_evnetListeners.Length + 1];
                _evnetListeners.CopyTo(newArray, 0);
                newArray[newArray.Length - 1]
                    = (LogStreamEventListener)listener;
                _evnetListeners = newArray;
            }
        }
    }
}