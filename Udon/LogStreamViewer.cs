using UdonSharp;

namespace Kmnk.LogStream.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LogStreamViewer : LogStreamBase
    {
        protected override void Start()
        {
            base.Start();
            _logStream.AddEventListener(this);
        }

        public void OnDisplayAllLogLines()
        {
            DisplayAllLogLines();
        }
        
        public void OnChangeType()
        {
            InvokeOnChangeType();
        }
    }
}