using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace Kmnk.LogStream.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LogInput : LogWriteBase
    {
        [SerializeField]
        [Tooltip("テンプレートメッセージ指定（旧方式）")]
        private string[] _templateMessages = null;

        [SerializeField]
        [Tooltip("テキストファイルからテンプレートメッセージを読み込みます")]
        private bool _useTemplateText = false;

        [SerializeField]
        [Tooltip("テンプレートメッセージを改行区切りで指定したテキストファイル")]
        private TextAsset _templateText = null;

        [SerializeField]
        [Tooltip("テンプレートメッセージのボタンが入るオブジェクト")]
        private Transform _templateButtonsTransform = null;

        [SerializeField]
        [Tooltip("テンプレートメッセージを増やした時に複製するボタンのオブジェクト")]
        private GameObject _templateButtonOrigin = null;

        [SerializeField]
        private Button _activateButton = null;

        [SerializeField]
        private GameObject _inputParent = null;

        [SerializeField]
        private InputField _inputField = null;

        public void AddMessage()
        {
            AddMessage(LogType.Message, _inputField.text, null);
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
