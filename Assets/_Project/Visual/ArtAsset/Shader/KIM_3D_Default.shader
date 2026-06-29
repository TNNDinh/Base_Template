Shader "KIM/3D/Default" {
	Properties {
		_MainTex ("Pattern Texture", 2D) = "white" {}
		_ColorFresnel ("Color Fresnel", Vector) = (1,1,1,1)
		_FresnelPow ("Fresnel Power", Range(0, 1)) = 0
		[Toggle] _SelectionGuide ("Selection Guide", Float) = 0
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