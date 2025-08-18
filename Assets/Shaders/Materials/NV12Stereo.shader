Shader "VideoStreaming/NV12Stereo"
{
	Properties
	{
		_YTex("Y channel", 2D) = "white" {}
		_UVTex("UV channel", 2D) = "gray" {}
	}
		SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 100

		Pass
	{
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"

	struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
		UNITY_VERTEX_INPUT_INSTANCE_ID
	};

	struct v2f
	{
		float4 vertex : SV_POSITION;
		float2 uv : TEXCOORD0;
		UNITY_VERTEX_INPUT_INSTANCE_ID
		UNITY_VERTEX_OUTPUT_STEREO
	};

	sampler2D _MainTex;
	sampler2D _YTex;
	sampler2D _UVTex;

	v2f vert(appdata v)
	{
		v2f o;
		UNITY_SETUP_INSTANCE_ID(v);
		UNITY_INITIALIZE_OUTPUT(v2f, o);
		UNITY_TRANSFER_INSTANCE_ID(v, o);
		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{

		float2 i_based_on_eye = float2(i.uv.x, i.uv.y);
	
		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
		if (unity_StereoEyeIndex == 0) {
			i_based_on_eye = float2(i.uv.x, i.uv.y * 0.5);
			
		}
		else if (unity_StereoEyeIndex == 1) {
			i_based_on_eye = float2(i.uv.x, i.uv.y * 0.5 + 0.5);
		}
	
		float ych = tex2D(_YTex, i_based_on_eye).a;
		float uch = tex2D(_UVTex, i_based_on_eye).x * 0.872 - 0.436;     //  Scale from 0 ~ 1 to -0.436 ~ +0.436
		float vch = tex2D(_UVTex, i_based_on_eye).y * 1.230 - 0.615;     //  Scale from 0 ~ 1 to -0.615 ~ +0.615
																/*  BT.601  */
		float rch = clamp(ych + 1.13983 * vch, 0.0, 1.0);
		float gch = clamp(ych - 0.39465 * uch - 0.58060 * vch, 0.0, 1.0);
		float bch = clamp(ych + 2.03211 * uch, 0.0, 1.0);

		fixed4 col = fixed4(rch, gch, bch, 1.0);


		return col;
	}
		ENDCG
	}
	}
}