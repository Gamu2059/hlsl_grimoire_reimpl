Shader "hlsl_grimoire/ch14/forward/toon_transparent"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "RenderType" = "Transparent"
        }

        Blend One Zero
        ZWrite On

        // フォワードライティングのOpaque描画パス
        Pass
        {
            Tags
            {
                "LightMode" = "CustomCh14Forward"
            }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

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
                half4 albedo = tex2D(_MainTex, i.uv) * _Color;
                half3 normal = i.normal;

                Light light = GetMainLight();

                float dotNL = saturate(dot(normal, light.lightDir));
                float toonDotNL = floor(dotNL * 5) * 0.2;
                half3 diffuse = light.color * toonDotNL;

                half shadowFactor = SampleShadow(i.positionWS.xyz, dotNL);

                half3 finalColor = albedo;
                finalColor *= diffuse * shadowFactor + GetAmbientLightColor();

                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}