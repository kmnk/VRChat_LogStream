using UnityEngine;
using VRC.SDKBase;

namespace Kmnk.Core.Udon
{
    public static class Util
    {
        public static bool AmIOwner(GameObject gameObject)
        {
            return Networking.IsOwner(gameObject);
        }

        public static void TakeOwner(GameObject gameObject)
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
    }
}