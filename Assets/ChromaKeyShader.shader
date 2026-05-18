Shader "Custom/ChromaKey"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _KeyColor ("Key Color (Chroma)", Color) = (1,0,1,1)
        _Range ("Range (Sensitivity)", Range(0, 1)) = 0.01
        _Edge ("Edge Softness", Range(0, 1)) = 0.05
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _KeyColor;
            float _Range;
            float _Edge;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                float d = distance(col.rgb, _KeyColor.rgb);

                if (d < _Range + 0.1) {
                    col.rgb *= float3(0.2, 0.2, 0.2);
                }
                
                col.a = smoothstep(_Range, _Range + _Edge, d);
                
                if (col.a == 0) discard;
                return col;
            }
            ENDCG
        }
    }
}