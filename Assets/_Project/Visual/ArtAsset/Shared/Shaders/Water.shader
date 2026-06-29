Shader "Sprites/WaterFlowSimple"
{
    Properties
    {
        [PerRendererData] _MainTex ("Water Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // Flow
        _Speed1 ("Flow Speed Layer1", Vector) = (0.02, 0.01, 0, 0)
        _Speed2 ("Flow Speed Layer2", Vector) = (-0.015, 0.008, 0, 0)

        // Sóng
        _WaveStrength ("Wave Strength", Float) = 0.01
        _WaveFrequency ("Wave Frequency", Float) = 10

        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Sprite"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile DUMMY PIXELSNAP_ON
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            float4 _Speed1;
            float4 _Speed2;
            float _WaveStrength;
            float _WaveFrequency;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                #ifdef PIXELSNAP_ON
                o.vertex = UnityPixelSnap(o.vertex);
                #endif
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;

                // Layer 1
                float2 uv1 = i.uv + float2(t * _Speed1.x, t * _Speed1.y);
                uv1.y += sin((uv1.x + t) * _WaveFrequency) * _WaveStrength;

                // Layer 2
                float2 uv2 = i.uv + float2(t * _Speed2.x, t * _Speed2.y);
                uv2.x += cos((uv2.y + t) * _WaveFrequency) * _WaveStrength;

                fixed4 col1 = tex2D(_MainTex, uv1);
                fixed4 col2 = tex2D(_MainTex, uv2);

                // Blend 2 layer → chỉ chuyển động, giữ nguyên màu
                fixed4 c = (col1 * 0.5 + col2 * 0.5) * i.color;

                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
