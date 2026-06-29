/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer
{
    public partial class Compatibility
    {
        public static int GetInstanceID(Object obj)
        {
#if UNITY_6000_4_OR_NEWER
#pragma warning disable CS0618 // Type or member is obsolete
            return obj.GetEntityId();
#pragma warning restore CS0618 // Type or member is obsolete
#else
            return obj.GetInstanceID();
#endif
        }

#if UNITY_6000_3_OR_NEWER
        public static int EntityToInstance(EntityId id)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return (int)id;
#pragma warning restore CS0618 // Type or member is obsolete
        }
        
        public static EntityId InstanceToEntity(int id)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return (EntityId)id;
#pragma warning restore CS0618 // Type or member is obsolete
        }
#endif
    }
}