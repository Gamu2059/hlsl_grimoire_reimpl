Shader "hlsl_grimoire/sample09_06/grayscale"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _LerpRate("Lerp Rate", Range(0, 1)) = 0
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    ENDHLSL

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float _LerpRate;

            Varyings vert(Attributes attributes)
            {
                Varyings o = (Varyings)0;
                o.positionCS = TransformObjectToHClip(attributes.positionOS);
                o.uv = TRANSFORM_TEX(attributes.uv, _MainTex);
                o.color = attributes.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 color = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half grayScale = 0.299 * color.r + 0.587 * color.g + 0.114 * color.b;
                half4 grayScaleColor = half4(grayScale, grayScale, grayScale, color.a);
                return lerp(color, grayScaleColor, _LerpRate);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
