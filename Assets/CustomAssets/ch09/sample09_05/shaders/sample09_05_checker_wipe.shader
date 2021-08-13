Shader "hlsl_grimoire/sample09_05/checker_wipe"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _WipeHorizontalDivision("Wipe Horizontal Division", Range(10, 100)) = 20
        _WipeDivision("Wipe Division", Range(10, 100)) = 20
        _WipeSize("Wipe Size", Range(0, 1)) = 0
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
            float _WipeHorizontalDivision;
            float _WipeDivision;
            float _WipeSize;

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
                float t = floor(i.uv.y * 100 / _WipeHorizontalDivision);
                t = fmod(t, 2.0);
                t = fmod( i.uv.x * 100 + 0.5 * _WipeDivision * t, _WipeDivision);
                clip(t - _WipeSize * _WipeDivision);
                return i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
