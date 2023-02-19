using System;
using System.Globalization;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

using Kmnk.Core.Udon;

namespace Kmnk.LogStream.Udon
{
    public class LogStreamBase : UdonSharpBehaviour
    {
        [SerializeField]
        protected LogStream _logStream = null;

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

        protected virtual void Start()
        {
            _logLines = _logLinesParent.GetComponentsInChildren<LogLine>();
            InitializeLogLines();

            if (Util.AmIOwner(gameObject))
            {
                OnStartIfOwner();
            }

            _dateTime = DateTime.UtcNow;
            UpdateHeader();
        }

        protected virtual void OnStartIfOwner() {}

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

            // maybe can write cooler
            var logIndex = -1;
            for (var i = 0; i < _logLines.Length; i++)
            {
                for (++logIndex; logIndex < _logStream.GetLogLimit(); logIndex++)
                {
                    if (_currentType == LogType.None
                        || _logStream.GetTypes()[logIndex] == _currentType)
                    {
                        break;
                    }
                }
                DisplayLogLine(
                    i,
                    logIndex < _logStream.GetLogLimit() ? logIndex : -1
                );
            }
            InvokeOnDisplayAllLogLines();
        }

        private void DisplayLogLine(int lineIndex, int logIndex)
        {
            _logLines[lineIndex].Display(
                logIndex >= 0 ? _logStream.GetMessages()[logIndex] : string.Empty,
                logIndex >= 0 ? _logStream.GetNames()[logIndex] : string.Empty,
                FormatTicks(logIndex >= 0 ? _logStream.GetTicks()[logIndex] : 0)
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
                .ToString(_logStream.GetTimeFormat(), CultureInfo.InvariantCulture);
        }

        protected LogStreamEventListener[] _evnetListeners;
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

        protected virtual void InvokeOnDisplayAllLogLines()
        {
            if (_evnetListeners != null)
            {
                foreach (var listener in _evnetListeners)
                {
                    listener.OnDisplayAllLogLines();
                }
            }
        }

        protected virtual void InvokeOnChangeType()
        {
            if (_evnetListeners != null)
            {
                foreach (var listener in _evnetListeners)
                {
                    listener.OnChangeType(_currentType);
                }
            }
        }
    }
}