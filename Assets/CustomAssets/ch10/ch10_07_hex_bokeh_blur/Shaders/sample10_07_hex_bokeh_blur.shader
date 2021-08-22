Shader "Hidden/hlsl_grimoire/sample10_06/depth_of_field"
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
            Name "Hex Bokeh Blur"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Pout
            {
                half4 color0 : COLOR0;
                half4 color1 : COLOR1;
            };

            int _SamplingCount;
            float _SamplingSpace;
            float2 _InverseTextureSize;

            Pout frag(Varyings i) : SV_Target
            {
                Pout o = (Pout)0;

                // 垂直方向へのサンプリング
                float2 offset = float2(0, 1) * _InverseTextureSize.xy * _SamplingSpace;
                for (int j = 1; j <= _SamplingCount; j++)
                {
                    o.color0 += SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, i.uv + offset * j);
                }
                o.color0 /= _SamplingCount;

                // 対角線方向へのサンプリング
                // サンプリング回数は1回多い
                offset = float2(0.86602f, -0.5f) * _InverseTextureSize.xy * _SamplingSpace;
                for (int j = 0; j <= _SamplingCount; j++)
                {
                    o.color1 += SAMPLE_TEXTURE2D_X(_MainTex, sampler_LinearClamp, i.uv + offset * j);
                }
                o.color1 /= _SamplingCount + 1;

                // 垂直方向に平均化
                o.color1 = o.color0 + o.color1;

                return o;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Hex Bokeh Blur Combine"
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D_X(_BlurTex1);
            TEXTURE2D_X(_BlurTex2);

            int _SamplingCount;
            float _SamplingSpace;
            float2 _InverseTextureSize;
            float _Lerp;

            half4 frag(Varyings i) : SV_Target
            {
                half4 color;

                // 左斜め下方向へのサンプリング
                float2 offset = float2(0.86602f, -0.5f) * _InverseTextureSize.xy * _SamplingSpace;
                for (int j = 1; j <= _SamplingCount; j++)
                {
                    color += SAMPLE_TEXTURE2D_X(_BlurTex1, sampler_LinearClamp, i.uv + offset * j);
                }

                // 右斜め下方向へのサンプリング
                // サンプリング回数は1回多い
                offset = float2(-0.86602f, -0.5f) * _InverseTextureSize.xy * _SamplingSpace;
                for (int j = 0; j <= _SamplingCount; j++)
                {
                    color += SAMPLE_TEXTURE2D_X(_BlurTex2, sampler_LinearClamp, i.uv + offset * j);
                }

                // 平均化
                color /= 2 * _SamplingCount + 1;
                color.a = _Lerp;
                return color;
            }
            ENDHLSL
        }
    }
}