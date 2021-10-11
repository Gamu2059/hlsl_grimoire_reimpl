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
        Tags
        {
            "Queue" = "Geometry" "RenderType" = "Opaque"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        //        Cull Off
        //        ZWrite Off

        Pass
        {
            Tags
            {
                "LightMode" = "CustomCh14"
            }

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
                float4 positionLVP : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _Color;

            float4 _LightColor;
            float4 _LightVector;

            float4x4 _LightView;
            float4x4 _LightProj;

            float _ShadowBias;
            float _ShadowNormalBias;

            sampler2D ShadowDepth;

            Varyings vert(Attributes attributes)
            {
                Varyings o = (Varyings)0;
                o.positionCS = UnityObjectToClipPos(attributes.positionOS);
                o.normalWS = UnityObjectToWorldNormal(attributes.normalOS);
                o.uv = TRANSFORM_TEX(attributes.uv, _MainTex);
                o.color = attributes.color;

                // シャドウマップ用
                o.positionLVP = mul(unity_ObjectToWorld, float4(attributes.positionOS, 1.0));
                o.positionLVP = mul(_LightView, o.positionLVP);
                o.positionLVP = mul(_LightProj, o.positionLVP);

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 color = i.color * tex2D(_MainTex, i.uv);
                float ldn = dot(_LightVector, i.normalWS);
                ldn = clamp(ldn, 0, 1);
                color.rgb *= ldn;

                float2 shadowUV = i.positionLVP.xy / i.positionLVP.w;
                shadowUV = shadowUV * float2(0.5f, -0.5f) + 0.5f;

                if (shadowUV.x > 0.0f && shadowUV.x < 1.0f && shadowUV.y > 0.0f && shadowUV.y < 1.0f)
                {
                    float zInLVP = i.positionLVP.z / i.positionLVP.w;
                    float zInShadow = tex2D(ShadowDepth, shadowUV).r;
                    float bias = _ShadowNormalBias * tan(acos(ldn)) + _ShadowBias;
                    if (zInLVP < zInShadow - bias)
                    {
                        color.xyz = 0;
                    }
                }

                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "CustomCh14Shadow"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 lightView : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4x4 _LightView;
            float4x4 _LightProj;

            Varyings vert(Attributes attributes)
            {
                Varyings o = (Varyings)0;
                o.lightView = mul(unity_ObjectToWorld, float4(attributes.positionOS, 1.0));
                o.lightView = mul(_LightView, o.lightView);
                o.lightView = mul(_LightProj, o.lightView);
                o.positionCS = o.lightView;
                o.uv = TRANSFORM_TEX(attributes.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return 1;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}