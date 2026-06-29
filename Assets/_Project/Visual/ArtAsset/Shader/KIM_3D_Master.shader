Shader "KIM/3D/Master" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		[NoScaleOffset] _PatternMap ("Pattern Texture", 2D) = "white" {}
		_PatternTransform ("Pattern Transform", Vector) = (0,0,1,0)
		[NoScaleOffset] _AOMap ("Ambient", 2D) = "white" {}
		[Header(Lighting)] _HSV ("Shadow Setting", Vector) = (0,1,0.5,10)
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