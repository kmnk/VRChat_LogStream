using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace Kmnk.LogStream.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LogInputTemplateButton : UdonSharpBehaviour
    {
        [SerializeField]
        private LogInput _logInput = null;

        [SerializeField]
        private Text _buttonText = null;

        public void _AppendToInput()
        {
            if (!_logInput.IsActiveInput())
            {
                _logInput._ActivateInput();
            }
            _logInput.AppendTextToInput(_buttonText.text);
        }
    }
}
