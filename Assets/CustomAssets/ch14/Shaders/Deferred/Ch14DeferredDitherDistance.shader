Shader "hlsl_grimoire/ch14/deferred/dither_distance"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0
        [Toggle] _ReceiveShadow("Is Receive Shadow", Float) = 0

        _MaxDistance("Max Distance", Float) = 10
        _MinDistance("Min Distance", Float) = 1

        [Toggle(USE_ORIGIN_POS)] _UseOriginPosition("Use Origin Position", Float) = 0
    }

    HLSLINCLUDE
    #include "UnityCG.cginc"

    static const float bayerPattern[4][4] = {
        {0 / 16.0, 8 / 16.0, 2 / 16.0, 10 / 16.0},
        {12 / 16.0, 4 / 16.0, 14 / 16.0, 6 / 16.0},
        {3 / 16.0, 11 / 16.0, 1 / 16.0, 9 / 16.0},
        {15 / 16.0, 7 / 16.0, 13 / 16.0, 5 / 16.0},
    };

    void ClipDither(float2 uvCS, half cutOut)
    {
        int x = fmod(uvCS.x, 4.0f);
        int y = fmod(uvCS.y, 4.0f);
        float dither = bayerPattern[y][x];
        clip(dither - cutOut);
    }
    ENDHLSL

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry" "RenderType" = "Opaque"
        }

        Blend One Zero

        // シャドウ描画パス
        Pass
        {
            Tags
            {
                "LightMode" = "CustomCh14Shadow"
            }

            HLSLPROGRAM
            #include "../Common/Ch14ShadowPass.hlsl"
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            ENDHLSL
        }

        // デプス描画パス
        Pass
        {
            Tags
            {
                "LightMode" = "CustomCh14PreDepth"
            }

            HLSLPROGRAM
            #include "../Common/Ch14PreDepthPass.hlsl"
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature USE_ORIGIN_POS

            CBUFFER_START(UnityPerMaterial)
            half _MaxDistance;
            half _MinDistance;
            CBUFFER_END

            struct DitherVaryings
            {
                float4 positionCS : SV_POSITION;
                float4 positionVS : TEXCOORD0;
                float cutout : TEXCOORD1;
            };

            DitherVaryings vert(Attributes attributes)
            {
                DitherVaryings o = (DitherVaryings)0;
                o.positionCS = UnityObjectToClipPos(attributes.positionOS);
                o.positionVS = mul(unity_MatrixV, mul(unity_ObjectToWorld, float4(attributes.positionOS, 1)));

                #if defined(USE_ORIGIN_POS)
                float3 vs = mul(unity_MatrixV, mul(unity_ObjectToWorld, float4(0, 0, 0, 1)));
                #else
                float3 vs = o.positionVS.xyz;
                #endif
                o.cutout = 1 - smoothstep(_MinDistance, _MaxDistance, length(vs));

                return o;
            }

            Output frag(DitherVaryings i)
            {
                ClipDither(i.positionCS.xy, i.cutout);

                const float depth = 1 - Linear01Depth(i.positionCS.z / i.positionCS.w);
                Output o = (Output)0;
                o.depth.x = depth;
                o.depth.y = i.positionVS.z / i.positionVS.w;
                o.depth.z = DecodeLinear01Depth(depth);
                o.actDepth = depth;
                return o;
            }
            ENDHLSL
        }

        // GBuffer描画パス
        Pass
        {
            Tags
            {
                "LightMode" = "CustomCh14GBuffer"
            }

            HLSLPROGRAM
            // 通常のGBufferのSRP Batchには含まれなくなるが、通常のGBuffer側のCBufferを無駄に汚さないようにした
            #define DONT_USE_GBUFFER_PARAM
            #include "../Common/Ch14GBufferPass.hlsl"
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature USE_ORIGIN_POS

            struct DitherVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCORRD0;
                float2 uv : TEXCOORD1;
                float3 normal : NORMAL;
                float cutout : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _Color;
            half _Metallic;
            half _Smoothness;
            half _ReceiveShadow;
            half _MaxDistance;
            half _MinDistance;
            CBUFFER_END

            DitherVaryings vert(Attributes attributes)
            {
                DitherVaryings o = (DitherVaryings)0;
                o.positionCS = UnityObjectToClipPos(attributes.positionOS);
                float4 ws = mul(unity_ObjectToWorld, float4(attributes.positionOS.xyz, 1));
                o.positionWS = ws.xyz;
                o.uv = TRANSFORM_TEX(attributes.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(attributes.normal);

                #if defined(USE_ORIGIN_POS)
                float3 vs = mul(unity_MatrixV, mul(unity_ObjectToWorld, float4(0, 0, 0, 1)));
                #else
                float3 vs = mul(unity_MatrixV, ws).xyz;
                #endif
                o.cutout = 1 - smoothstep(_MinDistance, _MaxDistance, length(vs));
                return o;
            }

            Output frag(DitherVaryings i)
            {
                ClipDither(i.positionCS.xy, i.cutout);
                
                Output o = (Output)0;
                o.albedo = tex2D(_MainTex, i.uv) * _Color;
                o.normal = half4(i.normal * 0.5 + 0.5, 1);
                o.worldPos = float4(i.positionWS, 1);

                // FrameDebugでパラメータを確認しやすくするため、意図的にアルファチャンネルを確保し、常に1をセットしている
                o.metalSmooth.x = clamp(_Metallic, 0, 1);
                o.metalSmooth.y = clamp(_Smoothness, 0, 1);
                o.metalSmooth.a = 1;
                o.shadowParam = _ReceiveShadow == 0 ? 0 : 1;
                return o;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}