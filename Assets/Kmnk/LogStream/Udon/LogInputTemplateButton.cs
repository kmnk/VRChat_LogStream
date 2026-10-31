using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace Kmnk.LogStream.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LogInputTemplateButton : UdonSharpBehaviour
    {
        [SerializeField]
        Udon.LogInput _logInput;

        [SerializeField]
        Text _buttonText;

        public void AppendToInput()
        {
            if (!_logInput.IsActiveInput())
            {
                _logInput.ActivateInput();
            }
            _logInput.AppendTextToInput(_buttonText.text);
        }
    }
}