Shader "hlsl_grimoire/sample09_03/circle_wipe"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _WipePos("Wipe Pos", Vector) = (0.5, 0.5, 0, 0)
        _WipeSize("Wipe Size", Range(0, 1.5)) = 0
        [Toggle(WIPE_INVERSE)] _Inverse("Inverse Wipe", float) = 0
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
            #pragma shader_feature _ WIPE_INVERSE

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
            float2 _WipePos;
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
                float a = 1;
                #ifdef WIPE_INVERSE
                a = -1;
                #endif
                
                float d = length(i.uv.xy - _WipePos);
                clip((d - _WipeSize) * a);
                return i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
