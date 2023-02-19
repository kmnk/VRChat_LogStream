using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace Kmnk.LogStream.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LogInput : LogWriteBase
    {
        LogType _type = LogType.Message;

        [SerializeField]
        Button _activateButton = null;

        [SerializeField]
        GameObject _inputParent = null;

        [SerializeField]
        InputField _inputField = null;

        public void AddMessage()
        {
            AddMessage(_type, _inputField.text, null);
        }

        public void DeactivateInput()
        {
            _activateButton.gameObject.SetActive(true);
            _inputParent.SetActive(false);
        }

        public void ActivateInput()
        {
            _activateButton.gameObject.SetActive(false);
            _inputParent.SetActive(true);
            _inputField.text = string.Empty;
            _inputField.Select();
        }

        public bool IsActiveInput()
        {
            return _inputParent.activeSelf;
        }

        public void AppendTextToInput(string text)
        {
            _inputField.text += text;
        }
    }
}