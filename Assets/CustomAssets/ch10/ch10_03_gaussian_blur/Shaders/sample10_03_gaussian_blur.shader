Shader "Hidden/hlsl_grimoire/sample10_03/gaussian_blur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off 
        ZWrite Off 
        ZTest Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            CBUFFER_END

            float _Lerp;
            int _SamplingCount;
            int _SamplingSpace;
            half _AvgDivFactor;

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 blurColor = color;
                float2 offset = _MainTex_TexelSize.xy * _SamplingSpace;
                for (int x = -_SamplingCount; x <= _SamplingCount; x++)
                {
                    for (int y = -_SamplingCount; y <= _SamplingCount; y++)
                    {
                        if (x == 0 && y == 0)
                        {
                            continue;
                        }

                        float2 uv = offset * float2(x, y);
                        blurColor += SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, i.uv + uv);
                    }
                }
                blurColor *= _AvgDivFactor;
                return lerp(color, blurColor, _Lerp);
            }
            ENDHLSL
        }
    }
}