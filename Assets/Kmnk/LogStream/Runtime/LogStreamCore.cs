using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Kmnk.LogStream.Udon
{
    // ログを型・時刻・名前・本文の 4 本の配列で持ち、同期する。新しいログは先頭に入り、最も古いものが落ちる
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class LogStreamCore : UdonSharpBehaviour
    {
        [SerializeField]
        [Tooltip("複数の LogStream を設置する際に指定します")]
        private int _id = 0;

        [SerializeField]
        [Tooltip("ログの最大保持数を指定します")]
        private int _logLimit = 100;

        [SerializeField]
        [Tooltip("ログの時間部分のフォーマットを指定します")]
        private string _timeFormat = "HH:mm:ss";

        [SerializeField]
        [Tooltip("ワールド作成時に最初から入れておくメッセージを指定します")]
        private string[] _initialMessages = null;

        [SerializeField]
        [Tooltip("ワールド作成時に最初から入れておくメッセージの名前部分を指定します")]
        private string _initialName = "";

        [SerializeField]
        private AudioSource _soundEffectAudioSource = null;

        [SerializeField]
        [Tooltip("ログが更新された時に効果音を流す場合に ON にします")]
        private bool _soundEffectEnabled = false;

        [UdonSynced]
        private LogType[] _types = null;

        [UdonSynced]
        private long[] _ticks = null;

        [UdonSynced]
        private string[] _names = null;

        [UdonSynced]
        private string[] _messages = null;

        private UdonSharpBehaviour[] _eventListeners = null;

        public int GetId()
        {
            return _id;
        }

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

        public bool AreSyncedFieldsInitialized()
        {
            return _types != null && _ticks != null && _names != null && _messages != null;
        }

        public void AddEventListener(LogStreamEventListener listener)
        {
            _eventListeners = ListenerArray.Append(_eventListeners, listener);
        }

        public void AddMessage(LogType type, string message, string playerName)
        {
            if (string.IsNullOrEmpty(message)) { return; }
            if (!AreSyncedFieldsInitialized()) { return; }

            Networking.SetOwner(Networking.LocalPlayer, gameObject);

            for (var i = _logLimit - 1; i > 0; i--)
            {
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
            InvokeOnDisplayAllLogLines();
            PlaySoundEffect();
        }

        private void Start()
        {
            if (!Networking.IsOwner(gameObject)) { return; }
            InitializeSyncedFields();
            RequestSerialization();
            InvokeOnDisplayAllLogLines();
        }

        public override void OnDeserialization()
        {
            if (!AreSyncedFieldsInitialized()) { return; }
            InvokeOnDisplayAllLogLines();
            PlaySoundEffect();
        }

        private void InitializeSyncedFields()
        {
            _types = new LogType[_logLimit];
            _ticks = new long[_logLimit];
            _names = new string[_logLimit];
            _messages = new string[_logLimit];

            for (var i = 0; i < _logLimit; i++)
            {
                var hasInitialMessage = _initialMessages != null && i < _initialMessages.Length;
                _types[i] = LogType.None;
                _ticks[i] = 0;
                _messages[i] = hasInitialMessage ? _initialMessages[i] : string.Empty;
                _names[i] = hasInitialMessage ? _initialName : string.Empty;
            }
        }

        private void InvokeOnDisplayAllLogLines()
        {
            if (_eventListeners == null) { return; }
            for (var i = 0; i < _eventListeners.Length; i++)
            {
                ((LogStreamEventListener)_eventListeners[i]).OnDisplayAllLogLines();
            }
        }

        private void PlaySoundEffect()
        {
            if (!_soundEffectEnabled) { return; }
            if (_soundEffectAudioSource == null) { return; }
            _soundEffectAudioSource.Play();
        }
    }
}
