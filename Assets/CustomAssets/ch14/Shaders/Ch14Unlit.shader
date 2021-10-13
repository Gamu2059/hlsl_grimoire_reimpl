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
            #pragma vertex ShadowVert
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

            float4x4 _LvpMat;

            float _ShadowBias;
            float _ShadowNormalBias;

            sampler2D ShadowDepth;
            sampler2D Depth;

            Varyings ShadowVert(Attributes attributes)
            {
                Varyings o = (Varyings)0;
                o.positionCS = UnityObjectToClipPos(attributes.positionOS);
                o.normalWS = UnityObjectToWorldNormal(attributes.normalOS);
                o.uv = TRANSFORM_TEX(attributes.uv, _MainTex);
                o.color = attributes.color;

                // シャドウマップ用
                o.positionLVP = mul(unity_ObjectToWorld, float4(attributes.positionOS, 1.0));
                o.positionLVP = mul(_LvpMat, o.positionLVP);

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // half4 color = i.color * tex2D(_MainTex, i.uv);
                // float ldn = dot(_LightVector, i.normalWS);
                // ldn = clamp(ldn, 0, 1);
                // color.rgb *= ldn;
                //
                // float2 shadowUV = i.positionLVP.xy / i.positionLVP.w;
                // shadowUV = shadowUV * float2(0.5f, -0.5f) + 0.5f;
                //
                // if (shadowUV.x > 0.0f && shadowUV.x < 1.0f && shadowUV.y > 0.0f && shadowUV.y < 1.0f)
                // {
                //     float zInLVP = i.positionLVP.z / i.positionLVP.w;
                //     float zInShadow = tex2D(ShadowDepth, shadowUV).r;
                //     float bias = _ShadowNormalBias * tan(acos(ldn)) + _ShadowBias;
                //     if (zInLVP < zInShadow - bias)
                //     {
                //         color.xyz = 0;
                //     }
                // }

                half depth = SAMPLE_DEPTH_TEXTURE(Depth, i.uv);
                return half4(depth, depth, depth, 1);
            }
            ENDHLSL
        }
        
        // デプス描画パス
        Pass
        {
            Tags
            {
                "LightMode" = "CustomCh14Depth"
            }
            ColorMask R

            HLSLPROGRAM
            #include "Ch14Depth.hlsl"
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            ENDHLSL
        }

        // シャドウ描画パス
        Pass
        {
            Tags
            {
                "LightMode" = "CustomCh14Shadow"
            }

            HLSLPROGRAM
            #include "Ch14Shadow.hlsl"
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}