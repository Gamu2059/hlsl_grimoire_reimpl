Shader "Hidden/hlsl_grimoire/sample10_04/bloom"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    HLSLINCLUDE
    #include "Library/PackageCache/com.unity.render-pipelines.universal@10.5.0/Shaders/PostProcessing/Common.hlsl"
    CBUFFER_START(UnityPerMaterial)
    TEXTURE2D(_MainTex);
    float4 _MainTex_ST;
    float4 _MainTex_TexelSize;
    CBUFFER_END

    Varyings vert(Attributes i)
    {
        Varyings o;
        o.positionCS = TransformObjectToHClip(i.positionOS);
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
            Name "Bloom Luminance Extract"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            half4 frag(Varyings i) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, i.uv);
                float t = dot(color.xyz, float3(0.2125f, 0.7154f, 0.0721f));
                clip(t - 1.f);
                return color;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "1pass Bloom Blur"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            int _SamplingCount;
            float _SamplingSpace;
            float _SamplingWeights[4];
            float2 _InverseTextureSize;

            half4 frag(Varyings i) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, i.uv);
                half4 blurColor = color * _SamplingWeights[0];
                float2 offset = _InverseTextureSize.xy * _SamplingSpace;

                // 横方向のサンプリング
                for (int x = 1; x <= _SamplingCount; x++)
                {
                    float2 uv = offset * float2(x, 0);
                    blurColor += SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, i.uv + uv) * _SamplingWeights[x];
                    blurColor += SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, i.uv - uv) * _SamplingWeights[x];
                }

                // 縦方向のサンプリング
                for (int y = 1; y <= _SamplingCount; y++)
                {
                    float2 uv = offset * float2(0, y);
                    blurColor += SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, i.uv + uv) * _SamplingWeights[y];
                    blurColor += SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, i.uv - uv) * _SamplingWeights[y];
                }

                return blurColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "2pass Bloom X Blur"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            int _SamplingCount;
            float _SamplingSpace;
            float _SamplingWeights[4];
            float2 _InverseTextureSize;

            half4 frag(Varyings i) : SV_Target
            {
                half4 blurColor = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, i.uv) * _SamplingWeights[0];
                float2 offset = _InverseTextureSize.xy * _SamplingSpace;

                // 横方向のサンプリング
                for (int x = 1; x <= _SamplingCount; x++)
                {
                    float2 uv = offset * float2(x, 0);
                    blurColor += SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, i.uv + uv) * _SamplingWeights[x];
                    blurColor += SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, i.uv - uv) * _SamplingWeights[x];
                }
                return blurColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "2pass Bloom Y Blur"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            int _SamplingCount;
            float _SamplingSpace;
            float _SamplingWeights[4];
            float2 _InverseTextureSize;

            half4 frag(Varyings i) : SV_Target
            {
                half4 blurColor = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, i.uv) * _SamplingWeights[0];
                float2 offset = _InverseTextureSize.xy * _SamplingSpace;

                // 縦方向のサンプリング
                for (int y = 1; y <= _SamplingCount; y++)
                {
                    float2 uv = offset * float2(0, y);
                    blurColor += SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, i.uv + uv) * _SamplingWeights[y];
                    blurColor += SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, i.uv - uv) * _SamplingWeights[y];
                }
                return blurColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Combine"
            // 加算合成なので、Blendの指定を忘れずに
            Blend One One
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float _Lerp;
            half4 _Tint;

            half4 frag(Varyings i) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, i.uv) * _Tint * _Lerp;
            }
            ENDHLSL
        }
    }
}