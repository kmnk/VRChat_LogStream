using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace Kmnk.LogStream.Udon
{
    // LogPomodoro の状態を、ボタン・文字・画像に描く。状態は持たず、LogPomodoro から渡された値だけを描く
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LogPomodoroView : UdonSharpBehaviour
    {
        [SerializeField]
        private Button _toggleButton = null;

        [SerializeField]
        private Text _toggleButtonText = null;

        [SerializeField]
        private Button _skipButton = null;

        [SerializeField]
        private Image _skipButtonImage = null;

        [SerializeField]
        private Button _resetButton = null;

        [SerializeField]
        private Image _resetButtonImage = null;

        [SerializeField]
        private Text _currentStatusText = null;

        [SerializeField]
        private Text _timerText = null;

        [SerializeField]
        private Button _onlyMasterToggleButton = null;

        [SerializeField]
        private Image _onlyMasterToggleButtonImage = null;

        [SerializeField]
        private Sprite _lockedSprite = null;

        [SerializeField]
        private Sprite _unlockedSprite = null;

        [SerializeField]
        private Button _autoContinueToggleButton = null;

        [SerializeField]
        private Image _autoContinueToggleButtonImage = null;

        private Color _activeColor = new Color32(255, 255, 255, 255);
        private Color _inactiveColor = new Color32(128, 128, 128, 128);

        // 状態が変わった時に、全てを描き直す
        public void Show(
            PomodoroStatus status,
            int count,
            long timerSeconds,
            bool canOperate,
            bool onlyMaster,
            bool autoContinue,
            string pomodoroName,
            string breakName)
        {
            ShowTimer(timerSeconds);
            ShowStatus(status, count, pomodoroName, breakName);
            ShowButtons(status, canOperate, onlyMaster, autoContinue);
        }

        // 1 秒ごとに、残りの時間だけを描き直す
        public void ShowTimer(long timerSeconds)
        {
            if (timerSeconds < 0) { timerSeconds = 0; }
            var minutes = timerSeconds / 60;
            var seconds = timerSeconds - minutes * 60;
            _timerText.text = $"{minutes:00}:{seconds:00}";
        }

        private void ShowStatus(PomodoroStatus status, int count, string pomodoroName, string breakName)
        {
            switch (status)
            {
                case PomodoroStatus.NotStarted:
                case PomodoroStatus.ToPomodoro:
                    _currentStatusText.text = $"Next {pomodoroName} #{count}";
                    break;

                case PomodoroStatus.InPomodoro:
                case PomodoroStatus.PausedPomodoro:
                    _currentStatusText.text = $"{pomodoroName} #{count}";
                    break;

                case PomodoroStatus.ToBreak:
                    _currentStatusText.text = $"Next {breakName} #{count}";
                    break;

                case PomodoroStatus.InBreak:
                case PomodoroStatus.PausedBreak:
                    _currentStatusText.text = $"{breakName} #{count}";
                    break;
            }
        }

        private void ShowButtons(PomodoroStatus status, bool canOperate, bool onlyMaster, bool autoContinue)
        {
            SetButtonActive(_toggleButton, null, canOperate);
            _toggleButtonText.color = canOperate ? _activeColor : _inactiveColor;
            SetButtonActive(_skipButton, _skipButtonImage, canOperate);
            SetButtonActive(_resetButton, _resetButtonImage, canOperate);
            SetButtonActive(_onlyMasterToggleButton, _onlyMasterToggleButtonImage, canOperate);
            _onlyMasterToggleButtonImage.sprite = onlyMaster ? _lockedSprite : _unlockedSprite;
            _autoContinueToggleButton.interactable = canOperate;
            _autoContinueToggleButtonImage.color = canOperate && autoContinue ? _activeColor : _inactiveColor;

            if (!canOperate)
            {
                _toggleButtonText.text = "MASTER ONLY";
            }
            else if (status == PomodoroStatus.InPomodoro || status == PomodoroStatus.InBreak)
            {
                _toggleButtonText.text = "PAUSE";
            }
            else
            {
                _toggleButtonText.text = "START";
            }
        }

        private void SetButtonActive(Button button, Image image, bool active)
        {
            button.interactable = active;
            if (image != null)
            {
                image.color = active ? _activeColor : _inactiveColor;
            }
        }
    }
}
