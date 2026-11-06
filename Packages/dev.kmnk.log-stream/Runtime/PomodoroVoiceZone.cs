using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace Kmnk.LogStream.Udon
{
    // LogPomodoro の作業のタイマーが動いている間、範囲の中にいる人の声を、範囲の中どうしと、範囲の中と外の間で聞こえなくする（または小さくする）。
    // 声の値は各端末の中だけのものなので、同期はせず、各端末が自分に聞こえる他の人の声を 1 秒ごとに書き直す。ログは書かない
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PomodoroVoiceZone : LogPomodoroEventListener
    {
        [SerializeField]
        [Tooltip("結ぶ Pomodoro を指定します。この Pomodoro の作業のタイマーが動いている間だけ声を変えます")]
        private LogPomodoro _pomodoro = null;

        [SerializeField]
        [Tooltip("範囲にする collider を指定します。どれかの中に足元があれば範囲の中です。isTrigger にし、下の面を床より下まで伸ばしてください。convex でない MeshCollider は使えません")]
        private Collider[] _zones = null;

        [SerializeField]
        [Tooltip("ON にすると、作業中の相手の声を完全に聞こえなくします。OFF にすると、声の大きさを Reduced Voice Gain にします")]
        private bool _muteCompletely = true;

        [SerializeField]
        [Tooltip("Mute Completely が OFF のときの、作業中の相手の声の gain（dB）を指定します。VRChat の既定は 15 で、0 にしても無音にはなりません")]
        [Range(0f, 24f)]
        private float _reducedVoiceGain = 5f;

        // 絞る前の声の値。playerId をキーに、Near・Far・Volumetric Radius・Gain の順の DataList を持つ
        private DataDictionary _saved = null;
        private VRCPlayerApi[] _players = new VRCPlayerApi[0];
        private bool _initialized = false;
        private bool _active = false;
        private bool _running = false;
        private bool _tickScheduled = false;

        public override void OnPomodoroStatusChanged()
        {
            if (!_initialized) { return; }
            var running = _active && _pomodoro.IsTimerRunning();
            if (running == _running) { return; }
            _running = running;
            if (running)
            {
                ApplyAll();
                ScheduleTick();
            }
            else
            {
                RestoreAll();
            }
        }

        // 作業中だけ 1 秒ごとに呼ばれ、全員の声を書き直す。出入りの取りこぼしや、他の仕組みが値を戻した場合も、次の呼び出しで直る
        public void _Tick()
        {
            _tickScheduled = false;
            if (!_running) { return; }
            ApplyAll();
            ScheduleTick();
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (_saved == null || !Utilities.IsValid(player)) { return; }
            _saved.Remove(player.playerId);
        }

        private void Start()
        {
            _saved = new DataDictionary();
            if (_zones == null || _zones.Length == 0) { _zones = GetComponents<Collider>(); }
            if (_pomodoro == null) { return; }
            _pomodoro.AddEventListener(this);
            _initialized = true;
            // 登録より前に最初の同期が済んでいた場合に、今の状態から始める
            OnPomodoroStatusChanged();
        }

        private void OnEnable()
        {
            _active = true;
            OnPomodoroStatusChanged();
        }

        private void OnDisable()
        {
            _active = false;
            // 無効の間に予約が捨てられても、有効に戻ったときに新しく予約できるようにする
            _tickScheduled = false;
            if (!_running) { return; }
            _running = false;
            RestoreAll();
        }

        private void ScheduleTick()
        {
            if (_tickScheduled) { return; }
            _tickScheduled = true;
            SendCustomEventDelayedSeconds(nameof(_Tick), 1f);
        }

        // 聞く人（自分）か話す人のどちらかが範囲の中なら絞り、どちらも外なら戻す
        private void ApplyAll()
        {
            var local = Networking.LocalPlayer;
            if (!Utilities.IsValid(local)) { return; }
            var localInside = IsInside(local);
            var players = GetPlayers();
            for (var i = 0; i < players.Length; i++)
            {
                var p = players[i];
                if (!Utilities.IsValid(p) || p.isLocal) { continue; }
                if (localInside || IsInside(p)) { Mute(p); }
                else { Restore(p); }
            }
        }

        private void RestoreAll()
        {
            var players = GetPlayers();
            for (var i = 0; i < players.Length; i++)
            {
                var p = players[i];
                if (!Utilities.IsValid(p) || p.isLocal) { continue; }
                Restore(p);
            }
        }

        private VRCPlayerApi[] GetPlayers()
        {
            var count = VRCPlayerApi.GetPlayerCount();
            if (_players.Length != count) { _players = new VRCPlayerApi[count]; }
            return VRCPlayerApi.GetPlayers(_players);
        }

        // 足元の位置が、どれかの collider の中にあるか
        private bool IsInside(VRCPlayerApi player)
        {
            if (_zones == null) { return false; }
            var position = player.GetPosition();
            for (var i = 0; i < _zones.Length; i++)
            {
                var zone = _zones[i];
                if (zone == null) { continue; }
                if ((zone.ClosestPoint(position) - position).sqrMagnitude < 0.000001f) { return true; }
            }
            return false;
        }

        // 初めて絞るときだけ今の値を取っておく。2 回目からは、自分が書いた値を絞る前の値として取らないよう、書くだけにする
        private void Mute(VRCPlayerApi player)
        {
            var key = new DataToken(player.playerId);
            if (!_saved.ContainsKey(key))
            {
                var values = new DataList();
                values.Add(player.GetVoiceDistanceNear());
                values.Add(player.GetVoiceDistanceFar());
                values.Add(player.GetVoiceVolumetricRadius());
                values.Add(player.GetVoiceGain());
                _saved.Add(key, values);
            }
            if (_muteCompletely)
            {
                player.SetVoiceDistanceNear(0f);
                player.SetVoiceDistanceFar(0f);
                player.SetVoiceVolumetricRadius(0f);
            }
            else
            {
                player.SetVoiceGain(_reducedVoiceGain);
            }
        }

        private void Restore(VRCPlayerApi player)
        {
            var key = new DataToken(player.playerId);
            DataToken token;
            if (!_saved.TryGetValue(key, TokenType.DataList, out token)) { return; }
            var values = token.DataList;
            player.SetVoiceDistanceNear(values[0].Float);
            player.SetVoiceDistanceFar(values[1].Float);
            player.SetVoiceVolumetricRadius(values[2].Float);
            player.SetVoiceGain(values[3].Float);
            _saved.Remove(key);
        }
    }
}
