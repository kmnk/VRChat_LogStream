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
        protected LogStreamCore _core = null;

        [SerializeField]
        GameObject _logLinesParent = null;

        [SerializeField]
        Text _dateText = null;

        [SerializeField]
        Text _timeText = null;

        [SerializeField]
        Text _playersCountText = null;

        private LogLine[] _logLines = null;

        private DateTime _dateTime;

        protected LogType _currentType = LogType.None;

        private void Start()
        {
            _logLines = _logLinesParent.GetComponentsInChildren<LogLine>();
            InitializeLogLines();

            _dateTime = DateTime.UtcNow;
            UpdateHeader();

            _core.AddEventListener(this);
        }

        protected void UpdateHeader()
        {
            var d = DateTime.UtcNow;
            _dateText.text = d.ToLocalTime().ToString("yyyy/MM/dd");
            _timeText.text = d.ToLocalTime().ToString("HH:mm:ss");
            _playersCountText.text = VRCPlayerApi.GetPlayerCount().ToString();
        }

        protected virtual void Update() {
            if (_dateTime.Second != DateTime.UtcNow.Second)
            {
                UpdateHeader();
            }
            _dateTime = DateTime.UtcNow;
        }

        private void InitializeLogLines()
        {
            foreach (var l in _logLines)
            {
                l.Initialize();
            }
        }

        protected bool HasAllLogLinesInitialized()
        {
            foreach (var l in _logLines)
            {
                if (!l.HasInitialized()) { return false; }
            }
            return true;
        }

        protected virtual void DisplayAllLogLines()
        {
            if (!HasAllLogLinesInitialized()) { return; }
            if (!_core.HasAllUdonSyncedFieldInitialized()) { return; }

            // maybe can write cooler
            var logIndex = -1;
            for (var i = 0; i < _logLines.Length; i++)
            {
                for (++logIndex; logIndex < _core.GetLogLimit(); logIndex++)
                {
                    if (_currentType == LogType.None
                        || _core.GetTypes()[logIndex] == _currentType)
                    {
                        break;
                    }
                }
                DisplayLogLine(
                    i,
                    logIndex < _core.GetLogLimit() ? logIndex : -1
                );
            }
        }

        private void DisplayLogLine(int lineIndex, int logIndex)
        {
            _logLines[lineIndex].Display(
                logIndex >= 0 ? _core.GetMessages()[logIndex] : string.Empty,
                logIndex >= 0 ? _core.GetNames()[logIndex] : string.Empty,
                FormatTicks(logIndex >= 0 ? _core.GetTicks()[logIndex] : 0)
            );
        }

        public void ChangeType(LogType type) 
        {
            _currentType = type;        
            DisplayAllLogLines();
            InvokeOnChangeType();
        }

        private string FormatTicks(long ticks)
        {
            if (ticks == 0) { return string.Empty; }
            return (new DateTime(ticks))
                .ToLocalTime()
                .ToString(_core.GetTimeFormat(), CultureInfo.InvariantCulture);
        }

        public void OnDisplayAllLogLines()
        {
            DisplayAllLogLines();
        }
        
        public void OnChangeType()
        {
            InvokeOnChangeType();
        }

        protected LogStreamViewerEventListener[] _eventListeners;
        public void AddEventListener(UdonSharpBehaviour listener)
        {
            if (_eventListeners == null)
            {
                _eventListeners
                    = new LogStreamViewerEventListener[]
                    {
                         (LogStreamViewerEventListener)listener
                    };
            }
            else
            {
                var newArray
                    = new LogStreamViewerEventListener[_eventListeners.Length + 1];
                _eventListeners.CopyTo(newArray, 0);
                newArray[newArray.Length - 1]
                    = (LogStreamViewerEventListener)listener;
                _eventListeners = newArray;
            }
        }

        protected virtual void InvokeOnChangeType()
        {
            if (_eventListeners != null)
            {
                foreach (var listener in _eventListeners)
                {
                    listener.OnChangeType(_currentType);
                }
            }
        }
    }
}