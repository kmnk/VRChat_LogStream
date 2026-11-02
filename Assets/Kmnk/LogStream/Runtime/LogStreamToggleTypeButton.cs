using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace Kmnk.LogStream.Udon
{
    // 押すと LogStreamViewer を _type に絞り込み、もう 1 度押すと絞り込みを外す
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LogStreamToggleTypeButton : LogStreamViewerEventListener
    {
        [SerializeField]
        private LogStreamViewer _viewer = null;

        [SerializeField]
        private LogType _type = LogType.None;

        [SerializeField]
        private Image _buttonImage = null;

        [SerializeField]
        private Text _buttonText = null;

        [SerializeField]
        private bool _isActive = false;

        private Color _activeImageColor = new Color32(0, 0, 0, 128);
        private Color _inactiveImageColor = new Color32(0, 0, 0, 0);
        private Color _activeTextColor = new Color32(255, 255, 255, 255);
        private Color _inactiveTextColor = new Color32(128, 128, 128, 255);

        public void Toggle()
        {
            if (_viewer == null) { return; }
            _isActive = !_isActive;
            _viewer.ChangeType(_isActive ? _type : LogType.None);
            ApplyColor();
        }

        public override void OnChangeType(LogType type)
        {
            _isActive = type == _type;
            ApplyColor();
        }

        private void Start()
        {
            if (_viewer == null) { return; }
            _viewer.AddEventListener(this);
        }

        private void ApplyColor()
        {
            _buttonImage.color = _isActive ? _activeImageColor : _inactiveImageColor;
            _buttonText.color = _isActive ? _activeTextColor : _inactiveTextColor;
        }
    }
}
