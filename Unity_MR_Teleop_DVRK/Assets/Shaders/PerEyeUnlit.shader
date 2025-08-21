Shader "Custom/StereoCylinder"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _EyeMask ("Eye Mask (0=Left,1=Right)", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _Color;
            float _EyeMask;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Se l'occhio corrente non corrisponde all'EyeMask, rendi trasparente
                if (unity_StereoEyeIndex != _EyeMask)
                    discard;

                return _Color;
            }
            ENDCG
        }
    }
}
