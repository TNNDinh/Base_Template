Shader "KIM/TextMesh/TextCustom" {
	Properties {
		[NoScaleOffset] _MainTex ("Font Atlas", 2D) = "white" {}
		_SizeFont ("Size Font", Range(0, 1)) = 0.5
		_BlurFont ("Blur Font", Range(0, 1)) = 0.1
		[Header(Gradient Overlay)] [Toggle(GRADIENT_ON)] _Gradinet ("Enable Gradient", Float) = 0
		_Color01 ("Gradient Color 01", Vector) = (0,0,0,1)
		_Color02 ("Gradient Color 02", Vector) = (1,1,1,1)
		_ScaleGra ("Gradient Scale", Float) = 1
		_PosGra ("Gradient Poisition", Float) = 0
		_AngleGra ("Gradient Angle", Float) = 0
		[Header(Inner Glow)] [Toggle(INNER_ON)] _Inner ("Enable Inner Glow", Float) = 0
		_InColor ("Inner Color", Vector) = (0.5,0.5,0.5,1)
		[KeywordEnum(Normal, Darken, Multiply, Color Burn, Linear Burn, Lighten, Screen, Color Dodge, Linear Dodge, Lighter Color, Overlay, Soft Light, Hard Light, Vivid Light, Linear Light, Pin Light, Hard Mix, Diference, Exclusion, Subtract, Divide)] BLENDING_MODE ("Blending Mode", Float) = 0
		_InSize ("Inner Size", Range(0, 1)) = 0.5
		_BlurIn ("Inner Glow", Range(0, 1)) = 0.1
		[Header(Drop Shadow)] [Toggle(DROPSHADOW_ON)] _DropShadow ("Enable Drop Shadow", Float) = 0
		_ShadowColor ("Shadow Color", Vector) = (0.5,0.5,0.5,0.5)
		_ShaSize ("Shadow Size", Range(0, 1)) = 0.5
		_BlurSha ("Shadow Blur", Range(0, 1)) = 0.1
		_ShadowDistance ("Shadow Distance", Range(0, 1)) = 0.5
		[Header(Outline)] [Toggle(OUTLINE_ON)] _Outline ("Enable Outline", Float) = 0
		_OutlineColor ("Outline Color", Vector) = (1,1,1,1)
		_OutSize ("Outline Size", Range(0, 1)) = 0.6
		_BlurOut ("Blur Outline", Range(0, 1)) = 0.1
		[Header(Warp Text)] [Toggle(WARPTEXT_ON)] _WarpText ("Enable Warp Text", Float) = 0
		_RoundWarp ("Round Warp", Range(0, 5)) = 2
		_WarpHeight ("Height", Float) = 5
		_WarpWidth ("Width", Float) = 1
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		_ColorMask ("Color Mask", Float) = 15
		_Cutoff ("Cutoff", Float) = 0.5
		[Header(Engine)] [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Float) = 2
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		sampler2D _MainTex;
		struct Input
		{
			float2 uv_MainTex;
		};

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
}