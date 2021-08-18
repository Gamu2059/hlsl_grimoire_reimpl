Shader "Hidden/hlsl_grimoire/sample10_02/monochrome"
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

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Library/PackageCache/com.unity.render-pipelines.universal@10.5.0/Shaders/PostProcessing/Common.hlsl"

            CBUFFER_START(UnityPerMaterial)
            TEXTURE2D(_MainTex);
            float4 _MainTex_ST;
            CBUFFER_END

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
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, i.uv);
                half monochromeFactor = 0.299 * color.r + 0.587 * color.g + 0.114 * color.b;
                half4 monochromeColor = half4(monochromeFactor, monochromeFactor, monochromeFactor, color.a);
                return lerp(color, monochromeColor, _Lerp);
            }
            ENDHLSL
        }
    }
}