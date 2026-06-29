Shader "Custom/OptimizedCrystalShader"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Distortion ("Distortion", Range(0, 0.1)) = 0.05
        _Alpha ("Alpha", Range(0,1)) = 1.0
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.1, 5)) = 2.0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Distortion;
            float _Alpha;
            float4 _RimColor;
            float _RimPower;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = WorldSpaceViewDir(v.vertex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 normal = normalize(i.normal);
                float3 viewDir = normalize(i.viewDir);

                // Simple distortion
                float2 distortedUV = i.uv + _Distortion * normal.xy;
                float4 baseColor = tex2D(_MainTex, distortedUV);

                // Rim lighting
                float rim = pow(1.0 - saturate(dot(viewDir, normal)), _RimPower);
                baseColor.rgb += _RimColor.rgb * rim * _RimColor.a;

                baseColor.a *= _Alpha;
                return baseColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}