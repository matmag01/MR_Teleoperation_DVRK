Shader "VideoStreaming/RGBStereoOffset"
{
	Properties{
		_MainTex("UpDown (RGBA)", 2D) = "white" {}
		_HorizontalOffset ("Horizontal Offset", Range (0,1.0)) = 0.0
		_VerticalOffset ("Vertical Offset", Range (0,1.0)) = 0.0
		_Swap ("Whether to swap the left and right channel", Range (0, 1.0)) = 0.1
	}

	SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 100


		Pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			float _HorizontalOffset;
			float _VerticalOffset;
			float _Swap;

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
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				if (_Swap < 0.5) { // not swapped
					if (unity_StereoEyeIndex == 0) { // left eye
						float xClamp = clamp(i.uv.x - _HorizontalOffset, 0.0, 1.0);
						float yClamp = clamp((i.uv.y - _VerticalOffset) * 0.5 + 0.5, 0.5, 1.0);
						fixed4 col = tex2D(_MainTex, float2(xClamp, yClamp));
						return col;
					}
					else if (unity_StereoEyeIndex == 1) { // right eye
						float xClamp = clamp(i.uv.x + _HorizontalOffset, 0.0, 1.0);
						float yClamp = clamp((i.uv.y + _VerticalOffset) * 0.5, 0.0, 0.5);
						fixed4 col = tex2D(_MainTex, float2(xClamp, yClamp));
						return col;
					}
					else {
						return 0;
					}
				}
				else { // swapped
					if (unity_StereoEyeIndex == 0) { // left eye
						float xClamp = clamp(i.uv.x - _HorizontalOffset, 0.0, 1.0);
						float yClamp = clamp((i.uv.y - _VerticalOffset) * 0.5, 0.0, 0.5);
						fixed4 col = tex2D(_MainTex, float2(xClamp, yClamp));
						return col;
					}
					else if (unity_StereoEyeIndex == 1) { // right eye
						float xClamp = clamp(i.uv.x + _HorizontalOffset, 0.0, 1.0);
						float yClamp = clamp((i.uv.y + _VerticalOffset) * 0.5 + 0.5, 0.5, 1.0);
						fixed4 col = tex2D(_MainTex, float2(xClamp, yClamp));
						return col;
					}
					else {
						return 0;
					}
				}
			}
			ENDCG
		}
	}
}
