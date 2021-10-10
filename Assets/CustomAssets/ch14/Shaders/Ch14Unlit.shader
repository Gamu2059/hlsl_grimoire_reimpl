Shader "hlsl_grimoire/ch14/lit"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
    }

    HLSLINCLUDE
    // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    ENDHLSL

    SubShader
    {
        Tags {"Queue" = "Geometry" "RenderType" = "Opaque" }

        Blend SrcAlpha OneMinusSrcAlpha
//        Cull Off
//        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "CustomCh14" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float3 normalWS : NORMAL;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _Color;
            float4 _LightColor;
            float4 _LightVector;

            Varyings vert(Attributes attributes)
            {
                Varyings o = (Varyings)0;
                o.positionCS = UnityObjectToClipPos(attributes.positionOS);
                o.normalWS = UnityObjectToWorldNormal(attributes.normalOS);
                o.uv = TRANSFORM_TEX(attributes.uv, _MainTex);
                o.color = attributes.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 color = i.color * tex2D(_MainTex, i.uv);
                float ldn = dot(_LightVector, i.normalWS);
                color.rgb *= clamp(ldn, 0, 1);
                return color;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
