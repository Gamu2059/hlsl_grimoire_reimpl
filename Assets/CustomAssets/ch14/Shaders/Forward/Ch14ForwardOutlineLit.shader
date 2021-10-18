Shader "hlsl_grimoire/ch14/forward/outline_lit"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _OutlineThreshold("Outline Threshold", Float) = 0.001
        _OutlineColor("Outline Color", Color) = (1,1,1,1)
    }

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
            #pragma fragment PreDepthFrag
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
            float _OutlineThreshold;
            half4 _OutlineColor;
            CBUFFER_END

            sampler2D _PreDepthTex;
            float4 _PreDepthTex_TexelSize;

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
                // 近傍8テクセルへのUVオフセット
                float2 uvOffset[8] = {
                    float2(0.0f, _PreDepthTex_TexelSize.y), //上
                    float2(0.0f, -_PreDepthTex_TexelSize.y), //下
                    float2(_PreDepthTex_TexelSize.x, 0.0f), //右
                    float2(-_PreDepthTex_TexelSize.x, 0.0f), //左
                    float2(_PreDepthTex_TexelSize.x, _PreDepthTex_TexelSize.y), //右上
                    float2(-_PreDepthTex_TexelSize.x, _PreDepthTex_TexelSize.y), //左上
                    float2(_PreDepthTex_TexelSize.x, -_PreDepthTex_TexelSize.y), //右下
                    float2(-_PreDepthTex_TexelSize.x, -_PreDepthTex_TexelSize.y) //左下
                };
                
                float4 cs = mul(UNITY_MATRIX_VP, i.positionWS);
                float2 projUV = cs.xy / cs.w * float2(0.5, -0.5) + 0.5;

                float depth = tex2D(_PreDepthTex, projUV).x;
                float otherDepth = 0;
                for (int j = 0; j < 8; j++)
                {
                    otherDepth += tex2D(_PreDepthTex, projUV + uvOffset[j]).x;
                }
                otherDepth *= 0.125;

                if (abs(depth - otherDepth) > _OutlineThreshold)
                {
                    return _OutlineColor;
                }
                
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