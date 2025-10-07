Shader "Custom/NV12YUVtoRGB" {
    Properties {
        _YTex ("Y Texture", 2D) = "white" {}
        _UVTex ("UV Texture", 2D) = "white" {}
    }
    SubShader {
        Pass {
            ZTest Always Cull Off ZWrite Off
            Fog { Mode off }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _YTex;
            sampler2D _UVTex;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // Campiona il valore Y dalla texture Y
                float y = tex2D(_YTex, i.uv).r;
                
                // Campiona i valori UV dalla texture UV
                // Nota: la risoluzione UV è la metà di quella Y
                float2 uv = tex2D(_UVTex, i.uv).rg; 
                
                // Spostamento dei valori U e V
                float u = uv.x - 0.5;
                float v = uv.y - 0.5;

                // Formula di conversione YUV -> RGB
                float r = y + 1.402 * v;
                float g = y - 0.344 * u - 0.714 * v;
                float b = y + 1.772 * u;

                return fixed4(r, g, b, 1.0);
            }
            ENDCG
        }
    } 
}