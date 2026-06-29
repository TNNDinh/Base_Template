/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.UnityTypes
{
    public static class ITreeViewDataSourceRef
    {
        private static MethodInfo _isExpandedMethod;
        private static Type _type;

        private static MethodInfo isExpandedMethod
        {
            get
            {
                if (_isExpandedMethod == null)
                {
#if UNITY_6000_3_OR_NEWER
                    Type parameterType = typeof(EntityId);
#else
                    Type parameterType = typeof(int);
#endif
                    
                    MethodInfo[] methods = type.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    _isExpandedMethod = methods.FirstOrDefault(m =>
                    {
                        if (m.Name != "IsExpanded") return false;
                        ParameterInfo[] parameters = m.GetParameters();
                        return parameters.Length == 1 && 
                               parameters[0].ParameterType == parameterType;
                    });
                }
                return _isExpandedMethod;
            }
        }

        private static Type type
        {
            get
            {
                if (_type == null)
                {
#if UNITY_6000_3_OR_NEWER
                    _type = Reflection.GetEditorType("IMGUI.Controls.ITreeViewDataSource`1");
                    _type = _type.MakeGenericType(typeof(EntityId));
#elif UNITY_6000_2_OR_NEWER
                    _type = Reflection.GetEditorType("IMGUI.Controls.ITreeViewDataSource`1");
                    _type = _type.MakeGenericType(typeof(int));
#else
                    _type = Reflection.GetEditorType("IMGUI.Controls.ITreeViewDataSource");
#endif
                } ;
                return _type;
            }
        }

        public static bool IsExpanded(
            object instance, 
            int id)
        {
#if UNITY_6000_3_OR_NEWER
            object[] ps = new object[] {Compatibility.InstanceToEntity(id)};
#else
            object[] ps = new object[] {id};
#endif
            return (bool)isExpandedMethod.Invoke(instance, ps);
        }
    }
}