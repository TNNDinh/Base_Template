Shader "KIM/3D/Block" {
	Properties {
		[Toggle(USE_GATE)] _Gate ("Gate", Float) = 0
		_Color ("Main Color", Vector) = (1,1,1,1)
		_ShadowColor ("Shadow Color", Vector) = (0.5,0.5,0.5,1)
		_PatternTex ("Pattern Texture", 2D) = "white" {}
		[Toggle] _SelectionGuide ("Selection Guide", Float) = 0
		[Toggle] _HasStar ("Star Block", Float) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		fixed4 _Color;
		struct Input
		{
			float2 uv_MainTex;
		};
		
		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			o.Albedo = _Color.rgb;
			o.Alpha = _Color.a;
		}
		ENDCG
	}
}