Shader "hlsl_grimoire/ch14/forward/dither"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _Cutout("Cutout", Range(0,1)) = 0
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
        ZWrite On

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
            #pragma vertex PreDepthVert
            #pragma fragment frag

            CBUFFER_START(UnityPerMaterial)
            half _Cutout;
            CBUFFER_END

            Output frag(Varyings i)
            {
                ClipDither(i.positionCS.xy, _Cutout);

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

        // フォワードライティングのOpaque描画パス
        Pass
        {
            Tags
            {
                "LightMode" = "CustomCh14Forward"
            }

            HLSLPROGRAM
            #include "UnityCG.cginc"
            #include "../Common/Ch14LightUtility.hlsl"
            #include "../Common/Ch14ShadowUtility.hlsl"
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 normal : NORMAL;
            };

            CBUFFER_START(UnityPerMaterial)
            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _Color;
            half _Cutout;
            CBUFFER_END

            Varyings vert(Attributes attributes)
            {
                Varyings o = (Varyings)0;
                o.positionCS = UnityObjectToClipPos(attributes.positionOS);
                o.positionWS = mul(unity_ObjectToWorld, float4(attributes.positionOS, 1));
                o.uv = TRANSFORM_TEX(attributes.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(attributes.normal);
                return o;
            }

            half4 frag(Varyings i) : COLOR0
            {
                ClipDither(i.positionCS.xy, _Cutout);

                half4 albedo = tex2D(_MainTex, i.uv) * _Color;
                half3 normal = i.normal;

                Light light = GetMainLight();

                float dotNL = saturate(dot(normal, light.lightDir));
                half3 diffuse = light.color * dotNL;

                half shadowFactor = SampleShadow(i.positionWS.xyz, dotNL);

                half3 finalColor = albedo;
                finalColor *= diffuse * shadowFactor + GetAmbientLightColor();

                return half4(finalColor, 1);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}