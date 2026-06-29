/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using SilkJson;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer
{
    [Serializable]
    public class MiniLayout
    {
        public string name;
        public string content;
        
        [HideInInspector]
        public string data;

        public Json json
        {
            get
            {
                JsonObject obj = new JsonObject();
                obj.Add("name", name);
                obj.Add("content", content);
                obj.Add("data", data);
                return obj;
            }
            set
            {
                name = value.V<string>("name");
                content = value.V<string>("content");
                data = value.V<string>("data");
            }
        }

        public MiniLayout()
        {
            
        }
        
        public MiniLayout(Json item)
        {
            json = item as JsonObject;
        }
    }
}
