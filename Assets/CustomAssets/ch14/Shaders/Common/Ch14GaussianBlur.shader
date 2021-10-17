Shader "Hidden/hlsl_grimoire/ch14/gaussian_blur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    HLSLINCLUDE
    #include "UnityCG.cginc"

    struct Attributes
    {
        float3 positionOS : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float3 positionWS : TEXCORRD0;
        float2 uv : TEXCOORD1;
        float3 normal : NORMAL;
    };

    sampler2D _MainTex;
    float4 _MainTex_ST;
    float4 _MainTex_TexelSize;

    Varyings vert(Attributes i)
    {
        Varyings o;
        o.positionCS = UnityObjectToClipPos(i.positionOS);
        o.uv = TRANSFORM_TEX(i.uv, _MainTex);
        return o;
    }
    ENDHLSL

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Off
        Blend One Zero

        Pass
        {
            Name "2pass Gaussian X Blur"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            int _SamplingCount;
            float _SamplingSpace;
            float _SamplingWeights[4];

            half4 frag(Varyings i) : SV_Target
            {
                half4 blurColor = tex2D(_MainTex, i.uv) * _SamplingWeights[0];
                float2 offset = _MainTex_TexelSize.xy * _SamplingSpace;

                // 横方向のサンプリング
                for (int x = 1; x <= _SamplingCount; x++)
                {
                    float2 uv = offset * float2(x, 0);
                    blurColor += tex2D(_MainTex, i.uv + uv) * _SamplingWeights[x];
                    blurColor += tex2D(_MainTex, i.uv - uv) * _SamplingWeights[x];
                }
                return blurColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "2pass Gaussian Y Blur"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            int _SamplingCount;
            float _SamplingSpace;
            float _SamplingWeights[4];

            half4 frag(Varyings i) : SV_Target
            {
                half4 blurColor = tex2D(_MainTex, i.uv) * _SamplingWeights[0];
                float2 offset = _MainTex_TexelSize.xy * _SamplingSpace;

                // 縦方向のサンプリング
                for (int y = 1; y <= _SamplingCount; y++)
                {
                    float2 uv = offset * float2(0, y);
                    blurColor += tex2D(_MainTex, i.uv + uv) * _SamplingWeights[y];
                    blurColor += tex2D(_MainTex, i.uv - uv) * _SamplingWeights[y];
                }
                return blurColor;
            }
            ENDHLSL
        }
    }
}