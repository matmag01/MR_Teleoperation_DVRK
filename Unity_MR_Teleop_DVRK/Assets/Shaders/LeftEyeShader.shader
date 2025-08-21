Shader "Custom/CylinderPerEye"
{
    Properties
    {
        _Color("Color", Color) = (1,0,0,1)
        _EyeOffset("Eye Offset", Float) = 0 // 0 = sinistro, 1 = destro
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            float4 _Color;
            float _EyeOffset;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Se non corrisponde all’occhio richiesto → trasparente
                #if defined(UNITY_SINGLE_PASS_STEREO)
                    float eye = unity_StereoEyeIndex;
                #else
                    float eye = _EyeOffset; // fallback se non SPIS
                #endif

                float alpha = (eye == _EyeOffset) ? _Color.a : 0;
                fixed4 col = _Color;
                col.a = alpha;
                clip(col.a - 0.5);
                return col;
            }
            ENDCG
        }
    }
}
