using UnityEngine;

namespace Kmnk.LogStream
{
    public class LogStream : MonoBehaviour
    {
#pragma warning disable CS0414
        [SerializeField]
        [Tooltip("複数の LogStream を設置する際に指定します")]
        int _id = 0;

        [SerializeField]
        [Tooltip("ログが更新された時に流す効果音を指定します")]
        AudioClip _soundEffectAudioClip = null;

        [SerializeField]
        [Tooltip("ログが更新された時に効果音を流す場合に ON にします")]
        bool _soundEffectEnabled = false;

        [SerializeField]
        [Tooltip("ログが更新された時の効果音の音量を指定します")]
        float _soundEffectGain = 10f;

        [SerializeField]
        [Tooltip("ログが更新された時の効果音が届く範囲を指定します")]
        float _soundEffectFar = 10f;

        [SerializeField]
        [Tooltip("ログの時間部分のフォーマットを指定します")]
        string _timeFormat = "HH:mm:ss";

        [SerializeField]
        [Tooltip("ログの最大保持数を指定します")]
        int _logLimit = 100;

        [SerializeField]
        [Tooltip("ワールド作成時に最初から入れておくメッセージを指定します")]
        string[] _initialMessages = null;

        [SerializeField]
        [Tooltip("ワールド作成時に最初から入れておくメッセージの名前部分を指定します")]
        string _initialName = "";
#pragma warning restore CS0414

        public int GetId()
        {
            return _id;
        }
    }
}