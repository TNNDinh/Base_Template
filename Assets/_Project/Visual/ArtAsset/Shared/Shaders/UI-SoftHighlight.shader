Shader "UI/SoftHighlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0,0,0,0.8)
        
        _HoleCenter ("Hole Center", Vector) = (0.5, 0.5, 0, 0)
        _HoleSize ("Hole Size (Width, Height)", Vector) = (100, 100, 0, 0)
        _Roundness ("Roundness", Float) = 10
        _Softness ("Softness", Float) = 50
        _CanvasSize ("Canvas Size", Vector) = (1080, 1920, 0, 0)
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _TextureSampleAdd;
            
            float4 _HoleCenter;
            float4 _HoleSize;
            float _Roundness;
            float _Softness;
            float4 _CanvasSize;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 pixelPos = IN.texcoord * _CanvasSize.xy;
                float2 diff = abs(pixelPos - _HoleCenter.xy);
                
                // Subtract corner _Roundness from half size to find the box inner bounds
                float2 q = diff - (_HoleSize.xy * 0.5 - _Roundness);
                // The distance to the rounded box is the length of the positive part of q, plus the max of the components if inside, minus the roundness
                float dist = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - _Roundness;
                
                float alphaFactor = saturate(dist / max(_Softness, 0.001));
                alphaFactor = smoothstep(0, 1, alphaFactor);
                
                fixed4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                
                // Cut the hole alpha
                color.a *= alphaFactor;
                
                return color;
            }
            ENDCG
        }
    }
}
