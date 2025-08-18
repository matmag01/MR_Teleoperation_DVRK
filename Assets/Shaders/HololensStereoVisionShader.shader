Shader "Stereo-Video-Custom" {
Properties {
    _Tint ("Tint Color", Color) = (.5, .5, .5, 1)
    [Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
    _Rotation ("Rotation", Range(0, 360)) = 0
    [NoScaleOffset] _MainTex ("Spherical (HDR)", 2D) = "grey" {}
    [KeywordEnum(6 Frames Layout, Latitude Longitude Layout)] _Mapping("Mapping", Float) = 1
    [Enum(360 Degrees, 0, 180 Degrees, 1)] _ImageType("Image Type", Float) = 0
    [Toggle] _MirrorOnBack("Mirror on Back", Float) = 0
    [Enum(None, 0, Side by Side, 1, Over Under, 2)] _Layout("3D Layout", Float) = 0
    _Transparency("Transparency", Range(0.0, 1.0)) = 1
}

SubShader {
    Tags { "Queue"="Transparent" "RenderType"="Transparent" "PreviewType"="Skybox" }
    ZWrite Off
    Blend SrcAlpha OneMinusSrcAlpha

    Pass {
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0
        #pragma multi_compile_local __ _MAPPING_6_FRAMES_LAYOUT

        #include "UnityCG.cginc"

        sampler2D _MainTex;
        half4 _Tint;
        half _Exposure;
        float _Rotation;
        float _Transparency;
        int _ImageType;
        bool _MirrorOnBack;

        inline float2 ToRadialCoords(float3 coords)
        {
            float3 n = normalize(coords);
            float latitude = acos(n.y);
            float longitude = atan2(n.z, n.x);
            float2 sphereCoords = float2(longitude, latitude) * float2(0.5/UNITY_PI, 1.0/UNITY_PI);
            return float2(0.5, 1.0) - sphereCoords;
        }

        float3 RotateAroundYInDegrees (float3 vertex, float degrees)
        {
            float alpha = degrees * UNITY_PI / 180.0;
            float sina, cosa;
            sincos(alpha, sina, cosa);
            float2x2 m = float2x2(cosa, -sina, sina, cosa);
            return float3(mul(m, vertex.xz), vertex.y).xzy;
        }

        struct appdata_t {
            float4 vertex : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct v2f {
            float4 vertex : SV_POSITION;
            float3 texcoord : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        v2f vert (appdata_t v)
        {
            v2f o;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            float3 rotated = RotateAroundYInDegrees(v.vertex, _Rotation);
            o.vertex = UnityObjectToClipPos(rotated);
            o.texcoord = v.vertex.xyz;
            return o;
        }

        fixed4 frag (v2f i) : SV_Target
        {
            float2 tc = ToRadialCoords(i.texcoord);

            // Se è un video 180°, taglia la parte dietro
            if (_ImageType == 1) {
                float cutoff = _MirrorOnBack ? 1.0 : 0.5;
                if (tc.x > cutoff)
                    return half4(0,0,0,0); // completamente trasparente
            }

            half4 tex = tex2D(_MainTex, tc);
            tex.rgb *= _Tint.rgb * _Exposure; // applica tinta ed esposizione
            tex.a *= _Transparency;           // applica trasparenza
            return tex;
        }
        ENDCG
    }
}

Fallback Off
}
