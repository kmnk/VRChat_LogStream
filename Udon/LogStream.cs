using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

using Kmnk.Core.Udon;

namespace Kmnk.LogStream.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class LogStream : LogStreamBase
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
        AudioSource _soundEffectAudioSource = null;

        [SerializeField]
        bool _soundEffectEnabled = false;

        [UdonSynced]
        private LogType[] _types = null;

        [UdonSynced]
        private long[] _ticks = null;

        [UdonSynced]
        private string[] _names = null;

        [UdonSynced]
        private string[] _messages = null;

        public int GetLogLimit()
        {
            return _logLimit;
        }

        public string GetTimeFormat()
        {
            return _timeFormat;
        }

        public LogType[] GetTypes()
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

        protected override void Start()
        {
            base.Start();

            DisplayAllLogLines();
        }

        protected override void OnStartIfOwner()
        {
            base.OnStartIfOwner();
            InitializeUdonSyncedFields();
            RequestSerialization();
        }

        protected override void Update()
        {
            base.Update();
        }

        public override void OnDeserialization()
        {
            if (!HasAllLogLinesInitialized()) { return; }
            if (!HasAllUdonSyncedFieldInitialized()) { return; }
            DisplayAllLogLines();
            PlaySoundEffect();
        }

        private void InitializeUdonSyncedFields()
        {
            _types = new LogType[_logLimit];
            _ticks = new long[_logLimit];
            _names = new string[_logLimit];
            _messages = new string[_logLimit];

            for (var i = 0; i < _logLimit; i++)
            {
                _types[i] = LogType.None;
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

        public void AddMessage(LogType type, string message, string playerName)
        {
            if (string.IsNullOrEmpty(message)) { return; }
            if (!HasAllUdonSyncedFieldInitialized()) { return; }

            Util.TakeOwner(gameObject);

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
            PlaySoundEffect();
        }

        protected override void DisplayAllLogLines()
        {
            if (!HasAllUdonSyncedFieldInitialized()) { return; }
            base.DisplayAllLogLines();
        }

        private void PlaySoundEffect()
        {
            if (!_soundEffectEnabled) { return; }
            if (_soundEffectAudioSource == null) { return; }
            _soundEffectAudioSource.Play();
        }
    }
}