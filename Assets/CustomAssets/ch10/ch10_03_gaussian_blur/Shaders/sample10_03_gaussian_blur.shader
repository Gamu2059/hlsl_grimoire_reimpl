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
            Name "1pass Gaussian Blur"
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
            float _SamplingSpace;
            float _SamplingWeights[4];
            float2 _InverseTextureSize;

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
                half4 blurColor = color * _SamplingWeights[0];
                float2 offset = _InverseTextureSize.xy * _SamplingSpace;

                // 横方向のサンプリング
                for (int x = 1; x <= _SamplingCount; x++)
                {
                    float2 uv = offset * float2(x, 0);
                    blurColor += SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, i.uv + uv) * _SamplingWeights[x];
                    blurColor += SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, i.uv - uv) * _SamplingWeights[x];
                }

                // 縦方向のサンプリング
                for (int y = 1; y <= _SamplingCount; y++)
                {
                    float2 uv = offset * float2(0, y);
                    blurColor += SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, i.uv + uv) * _SamplingWeights[y];
                    blurColor += SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, i.uv - uv) * _SamplingWeights[y];
                }

                return lerp(color, blurColor, _Lerp);
            }
            ENDHLSL
        }

        Pass
        {
            Name "2pass Gaussian X Blur"
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

            int _SamplingCount;
            float _SamplingSpace;
            float _SamplingWeights[4];
            float2 _InverseTextureSize;

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 blurColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _SamplingWeights[0];
                float2 offset = _InverseTextureSize.xy * _SamplingSpace;

                // 横方向のサンプリング
                for (int x = 1; x <= _SamplingCount; x++)
                {
                    float2 uv = offset * float2(x, 0);
                    blurColor += SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, i.uv + uv) * _SamplingWeights[x];
                    blurColor += SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, i.uv - uv) * _SamplingWeights[x];
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

            int _SamplingCount;
            float _SamplingSpace;
            float _SamplingWeights[4];
            float2 _InverseTextureSize;

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 blurColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _SamplingWeights[0];
                float2 offset = _InverseTextureSize.xy * _SamplingSpace;

                // 縦方向のサンプリング
                for (int y = 1; y <= _SamplingCount; y++)
                {
                    float2 uv = offset * float2(0, y);
                    blurColor += SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, i.uv + uv) * _SamplingWeights[y];
                    blurColor += SAMPLE_TEXTURE2D_X(_MainTex, sampler_MainTex, i.uv - uv) * _SamplingWeights[y];
                }
                return blurColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "2pass Gaussian Blur Combine"
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
            CBUFFER_END

            TEXTURE2D(_CameraColorTexture);
            float _Lerp;

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_MainTex, i.uv);
                half4 blurColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return lerp(color, blurColor, _Lerp);
            }
            ENDHLSL
        }
    }
}