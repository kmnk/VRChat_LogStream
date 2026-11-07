using UnityEngine;

namespace Kmnk.LogStream
{
    // prefab の親の GameObject に置く、ギミックの外側のコンポーネントの基底。値は持たず、設定は子の Udon が持つ。
    // 外側は、設定の入口（Inspector に子の Udon の設定を描く。Editor/GimmickRootEditor.cs）、tips、Unity の Editor の上での小さな処理を受け持つ。
    // 子の Udon の UdonSharpBehaviour は、派生のクラスと同じ名前にする
    public abstract class GimmickRoot : MonoBehaviour
    {
    }
}
