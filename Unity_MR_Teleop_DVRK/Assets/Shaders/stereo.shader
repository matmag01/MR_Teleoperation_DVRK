// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Stereo-Video" {
Properties {
    _Tint ("Tint Color", Color) = (.5, .5, .5, .5)
    [Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
    _Rotation ("Rotation", Range(0, 360)) = 0
    [NoScaleOffset] _MainTex ("Spherical  (HDR)", 2D) = "grey" {}
    [KeywordEnum(6 Frames Layout, Latitude Longitude Layout)] _Mapping("Mapping", Float) = 1
    [Enum(360 Degrees, 0, 180 Degrees, 1)] _ImageType("Image Type", Float) = 0
    [Toggle] _MirrorOnBack("Mirror on Back", Float) = 0
    [Enum(None, 0, Side by Side, 1, Over Under, 2)] _Layout("3D Layout", Float) = 0
    _Transparency("Transparency", Range(0.0, 1.0)) = 1 // Add transparency property
}

SubShader {
    Tags { "Queue"="Transparent" "RenderType"="Geometry" "PreviewType"="Skybox" }
    ZWrite On
  /*  LOD 100
    Cull Off
    ZWrite Off
    Blend SrcAlpha OneMinusSrcAlpha*/

    Pass {

        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0
        #pragma multi_compile_local __ _MAPPING_6_FRAMES_LAYOUT

        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4 _MainTex_TexelSize;
        half4 _MainTex_HDR;
        half4 _Tint;
        half _Exposure;
        float _Rotation;
#ifndef _MAPPING_6_FRAMES_LAYOUT
        bool _MirrorOnBack;
        int _ImageType;
        int _Layout;
#endif
        // Define transparency property
        float _Transparency;
#ifndef _MAPPING_6_FRAMES_LAYOUT
        inline float2 ToRadialCoords(float3 coords)
        {
            float3 normalizedCoords = normalize(coords);
            float latitude = acos(normalizedCoords.y);
            float longitude = atan2(normalizedCoords.z, normalizedCoords.x);
            float2 sphereCoords = float2(longitude, latitude) * float2(0.5/UNITY_PI, 1.0/UNITY_PI);
            return float2(0.5,1.0) - sphereCoords;
        }
#endif

#ifdef _MAPPING_6_FRAMES_LAYOUT
        inline float2 ToCubeCoords(float3 coords, float3 layout, float4 edgeSize, float4 faceXCoordLayouts, float4 faceYCoordLayouts, float4 faceZCoordLayouts)
        {
            // Determine the primary axis of the normal
            float3 absn = abs(coords);
            float3 absdir = absn > float3(max(absn.y,absn.z), max(absn.x,absn.z), max(absn.x,absn.y)) ? 1 : 0;
            // Convert the normal to a local face texture coord [-1,+1], note that tcAndLen.z==dot(coords,absdir)
            // and thus its sign tells us whether the normal is pointing positive or negative
            float3 tcAndLen = mul(absdir, float3x3(coords.zyx, coords.xzy, float3(-coords.xy,coords.z)));
            tcAndLen.xy /= tcAndLen.z;
            // Flip-flop faces for proper orientation and normalize to [-0.5,+0.5]
            bool2 positiveAndVCross = float2(tcAndLen.z, layout.x) > 0;
            tcAndLen.xy *= (positiveAndVCross[0] ? absdir.yx : (positiveAndVCross[1] ? float2(absdir[2],0) : float2(0,absdir[2]))) - 0.5;
            // Clamp values which are close to the face edges to avoid bleeding/seams (ie. enforce clamp texture wrap mode)
            tcAndLen.xy = clamp(tcAndLen.xy, edgeSize.xy, edgeSize.zw);
            // Scale and offset texture coord to match the proper square in the texture based on layout.
            float4 coordLayout = mul(float4(absdir,0), float4x4(faceXCoordLayouts, faceYCoordLayouts, faceZCoordLayouts, faceZCoordLayouts));
            tcAndLen.xy = (tcAndLen.xy + (positiveAndVCross[0] ? coordLayout.xy : coordLayout.zw)) * layout.yz;
            return tcAndLen.xy;
        }
#endif

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
#ifdef _MAPPING_6_FRAMES_LAYOUT
            float3 layout : TEXCOORD1;
            float4 edgeSize : TEXCOORD2;
            float4 faceXCoordLayouts : TEXCOORD3;
            float4 faceYCoordLayouts : TEXCOORD4;
            float4 faceZCoordLayouts : TEXCOORD5;
#else
            float2 image180ScaleAndCutoff : TEXCOORD1;
            float4 layout3DScaleAndOffset : TEXCOORD2;
#endif
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
            o.texcoord.x = o.texcoord.x*0.5;
            o.texcoord = o.texcoord + float3(0.25 + 0.5*(unity_StereoEyeIndex), 0.5, 0);

            // // Calculate constant horizontal scale and cutoff for 180 (vs 360) image type
            // if (_ImageType == 0)  // 360 degree
            //     o.image180ScaleAndCutoff = float2(1.0, 1.0);
            // else  // 180 degree
            //     o.image180ScaleAndCutoff = float2(2.0, _MirrorOnBack ? 1.0 : 0.5);
            // // Calculate constant scale and offset for 3D layouts
            // if (_Layout == 0) // No 3D layout
            //     o.layout3DScaleAndOffset = float4(0,0,1,1);
            // else if (_Layout == 1) // Side-by-Side 3D layout
            //     o.layout3DScaleAndOffset = float4(unity_StereoEyeIndex,0,0.5,1);
            // else // Over-Under 3D layout
            //     o.layout3DScaleAndOffset = float4(0, 1-unity_StereoEyeIndex,1,0.5);

            // o.texcoord = 

            return o;
        }


        fixed4 frag (v2f i) : SV_Target
        {
            // float3 t = i.texcoord + float3(0,-0.5,0);
            // half4 tex = tex2D(_MainTex, t);
            half4 tex = tex2D(_MainTex, i.texcoord );
             tex.a = _Transparency;
            return tex;
        }

        //half4 frag(v2f i) : SV_Target{
        //        half4 col = tex2D(_MainTex, i.texcoord);
        //        col.rgb *= _Tint.rgb * _MainTex_HDR.rgb * _Exposure;
        //        col.rgb = RotateAroundYInDegrees(col.rgb, _Rotation);

        //        // Apply transparency to alpha channel
        //        col.a *= _Transparency;

        //        return col;
        //}

//         fixed4 frag (v2f i) : SV_Target
//         {
// #ifdef _MAPPING_6_FRAMES_LAYOUT
//             float2 tc = ToCubeCoords(i.texcoord, i.layout, i.edgeSize, i.faceXCoordLayouts, i.faceYCoordLayouts, i.faceZCoordLayouts);
// #else
//             float2 tc = ToRadialCoords(i.texcoord);
//             if (tc.x > i.image180ScaleAndCutoff[1])
//                 return half4(0,0,0,1);
//             tc.x = fmod(tc.x*i.image180ScaleAndCutoff[0], 1);
//             tc = (tc + i.layout3DScaleAndOffset.xy) * i.layout3DScaleAndOffset.zw;
// #endif

//             half4 tex = tex2D (_MainTex, tc);
//             half3 c = DecodeHDR (tex, _MainTex_HDR);
//             c = c * _Tint.rgb * unity_ColorSpaceDouble.rgb;
//             c *= _Exposure;
//             return half4(c, 1);
//         }
        ENDCG
    }
}


CustomEditor "SkyboxPanoramicShaderGUI"
Fallback Off

}
