Shader "Custom/PixelWater"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _Speed ("Speed", Float) = 0.08

        _WaveStrength ("Wave Strength", Float) = 0.03

        _WaveFrequency ("Wave Frequency", Float) = 20
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Background"
        }

        Pass
        {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _Speed;
            float _WaveStrength;
            float _WaveFrequency;

            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);

                float2 uv = TRANSFORM_TEX(v.uv, _MainTex);

                // Scroll nước
                uv.x += _Time.y * _Speed;

                // Sóng dọc
                uv.y += sin((uv.x * _WaveFrequency) + (_Time.y * 3))
                        * _WaveStrength;

                // Méo ngang nhẹ
                uv.x += cos((uv.y * 15) + (_Time.y * 2))
                        * 0.01;

                o.uv = uv;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                return col;
            }

            ENDCG
        }
    }
}