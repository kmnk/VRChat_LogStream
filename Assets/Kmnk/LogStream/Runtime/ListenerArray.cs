using UdonSharp;

namespace Kmnk.LogStream.Udon
{
    // event を受け取る側の配列を扱う。UdonSharp では List<T> と generic のメソッドを使えないので、配列を伸ばして足す
    public static class ListenerArray
    {
        public static UdonSharpBehaviour[] Append(UdonSharpBehaviour[] listeners, UdonSharpBehaviour listener)
        {
            if (listeners == null)
            {
                return new UdonSharpBehaviour[] { listener };
            }
            var appended = new UdonSharpBehaviour[listeners.Length + 1];
            listeners.CopyTo(appended, 0);
            appended[listeners.Length] = listener;
            return appended;
        }
    }
}
