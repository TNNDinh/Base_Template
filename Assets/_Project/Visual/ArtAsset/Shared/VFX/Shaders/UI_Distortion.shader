Shader "Custom/BuiltIn_UIDistortion"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _NoiseTex ("Distortion Noise (Normal Map)", 2D) = "bump" {}
        _DistortionStrength ("Distortion Strength", Range(0, 0.1)) = 0.02
        _SpeedX ("Speed X", Range(-1, 1)) = 0.1
        _SpeedY ("Speed Y", Range(-1, 1)) = 0.1

        // UI Masking Requirements (Required for UI elements)
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _WriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _ReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent" 
            "IgnoreProjector" = "True" 
            "RenderType" = "Transparent" 
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        // Stencil block for UI Masking
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_ReadMask]
            WriteMask [_WriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        // 1. This command grabs the screen behind the object
        GrabPass
        {
            "_GrabTexture"
        }

        // 2. This pass renders the distorted image
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
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 grabPos  : TEXCOORD1; // Coordinates for the GrabTexture
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            sampler2D _GrabTexture; // Automatically filled by GrabPass
            
            float4 _NoiseTex_ST;
            float _DistortionStrength;
            float _SpeedX;
            float _SpeedY;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color;

                // Calculate where on the screen this object is
                OUT.grabPos = ComputeGrabScreenPos(OUT.vertex);
                
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 1. Calculate Moving Noise
                float2 speed = float2(_SpeedX, _SpeedY) * _Time.y;
                float2 noiseUV = TRANSFORM_TEX(IN.texcoord, _NoiseTex) + speed;
                
                // 2. Get Normal Data
                half3 normal = UnpackNormal(tex2D(_NoiseTex, noiseUV));

                // 3. Offset the Screen Grab UVs
                // We use grabPos.z to handle perspective correctly
                IN.grabPos.xy += normal.xy * _DistortionStrength * IN.grabPos.w;

                // 4. Sample the background texture
                fixed4 bgcolor = tex2Dproj(_GrabTexture, IN.grabPos);

                // 5. Apply the UI Color (Vertex Color) for tinting/alpha
                return bgcolor * IN.color;
            }
            ENDCG
        }
    }
}