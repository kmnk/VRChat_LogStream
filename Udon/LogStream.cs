using System;
using System.Globalization;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace Kmnk.LogStream.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class LogStream : UdonSharpBehaviour
    {
        [SerializeField]
        int _logLimit = 100;

        [SerializeField]
        string _timeFormat = "HH:mm:ss";

        [SerializeField]
        string[] _initialMessages = null;

        [SerializeField]
        string _initialName = "";

        [SerializeField]
        GameObject _logLinesParent = null;

        [SerializeField]
        Text _dateText = null;

        [SerializeField]
        Text _timeText = null;

        [SerializeField]
        Text _playersCountText = null;

        [UdonSynced]
        private string[] _types = null;

        [UdonSynced]
        private long[] _ticks = null;

        [UdonSynced]
        private string[] _names = null;

        [UdonSynced]
        private string[] _messages = null;

        private Udon.LogLine[] _logLines = null;

        private DateTime _dateTime;

        private string _currentType = "";

        private void Start()
        {
            _logLines = _logLinesParent.GetComponentsInChildren<Udon.LogLine>();
            InitializeLogLines();

            if (AmIOwner())
            {
                InitializeUdonSyncedFields();
                RequestSerialization();
            }

            DisplayAllLogLines();

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

        public override void OnDeserialization()
        {
            if (!HasAllLogLinesInitialized()) { return; }
            if (!HasAllUdonSyncedFieldInitialized()) { return; }
            DisplayAllLogLines();
        }

        public int GetLogLimit()
        {
            return _logLimit;
        }

        public string[] GetTypes()
        {
            return _types;
        }

        public string[] GetMessages()
        {
            return _messages;
        }

        public string[] GetNames()
        {
            return _names;
        }

        public long[] GetTicks()
        {
            return _ticks;
        }

        private void InitializeLogLines()
        {
            foreach (var l in _logLines)
            {
                l.Initialize();
            }
        }

        private bool HasAllLogLinesInitialized()
        {
            foreach (var l in _logLines)
            {
                if (!l.HasInitialized()) { return false; }
            }
            return true;
        }

        private void InitializeUdonSyncedFields()
        {
            _types = new string[_logLimit];
            _ticks = new long[_logLimit];
            _names = new string[_logLimit];
            _messages = new string[_logLimit];

            for (var i = 0; i < _logLimit; i++)
            {
                _types[i] = string.Empty;
                _ticks[i] = 0;
                if (_initialMessages != null && _initialMessages.Length > i)
                {
                    _messages[i] = _initialMessages[i];
                    _names[i] = _initialName;
                }
                else
                {
                    _messages[i] = string.Empty;
                    _names[i] = string.Empty;
                }
            }
        }


        private bool HasAllUdonSyncedFieldInitialized()
        {
            if (_types == null) { return false; }
            if (_ticks == null) { return false; }
            if (_names == null) { return false; }
            if (_messages == null) { return false; }
            return true;
        }

        public void AddMessage(string type, string message, string playerName)
        {
            if (string.IsNullOrEmpty(message)) { return; }
            if (!HasAllUdonSyncedFieldInitialized()) { return; }

            TakeOwner();

            for (var i = _logLimit-1; i > 0; i--) {
                _types[i] = _types[i - 1];
                _ticks[i] = _ticks[i - 1];
                _names[i] = _names[i - 1];
                _messages[i] = _messages[i - 1];
            }

            _types[0] = type;
            _ticks[0] = DateTime.UtcNow.Ticks;
            _names[0] = playerName ?? Networking.LocalPlayer.displayName;
            _messages[0] = message;

            RequestSerialization();
            DisplayAllLogLines();
        }

        private void DisplayAllLogLines()
        {
            if (!HasAllUdonSyncedFieldInitialized()) { return; }
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

            if (_evnetListeners != null)
            {
                foreach (var listener in _evnetListeners)
                {
                    listener.OnDisplayAllLogLines();
                }
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

        #region base
        private bool AmIOwner()
        {
            return Networking.IsOwner(gameObject);
        }

        private void TakeOwner()
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        #endregion
    }
}