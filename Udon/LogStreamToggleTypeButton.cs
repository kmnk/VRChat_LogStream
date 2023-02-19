using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace Kmnk.LogStream.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LogStreamToggleTypeButton : UdonSharpBehaviour
    {
        [SerializeField]
        LogStreamBase _logStream = null;

        [SerializeField]
        LogType _type = LogType.None;

        [SerializeField]
        Image _buttonImage;

        [SerializeField]
        Text _buttonText;

        [SerializeField]
        bool _isActive = false;

        private Color _activeImageColor = new Color32(0, 0, 0, 128);
        private Color _inactiveImageColor = new Color32(0, 0, 0, 0);

        private Color _activeTextColor = new Color32(255, 255, 255, 255);
        private Color _inactiveTextColor = new Color32(128, 128, 128, 255);

        private void Start()
        {
            if (_logStream == null) { return; }
            _logStream.AddEventListener(this);
        }

        public void Toggle()
        {
            if (_logStream == null) { return; }
            _isActive = !_isActive;
            _logStream.ChangeType(_isActive ? _type : LogType.None);
            ApplyColor();
        }

        public void OnChangeType(LogType type)
        {
            _isActive = type == _type;
            ApplyColor();
        }

        private void ApplyColor()
        {
            _buttonImage.color
                = _isActive ? _activeImageColor : _inactiveImageColor;
            _buttonText.color
                = _isActive ? _activeTextColor : _inactiveTextColor;
        }
    }
}