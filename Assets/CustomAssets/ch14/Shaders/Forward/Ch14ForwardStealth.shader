Shader "hlsl_grimoire/ch14/forward/stealth"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _NoisePower("NoisePower", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "RenderType" = "Transparent"
        }

        Blend One Zero

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
            float _NoisePower;
            CBUFFER_END

            sampler2D _OpaqueColorTex;
            float4 _OpaqueColorTex_TexelSize;

            // float1 ハッシュ関数
            float hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            // float2 ハッシュ関数
            float hash(float2 n)
            {
                return hash(dot(n, float2(12.9898, 78.233)));
            }

            // 3次元ベクトルからシンプレックスノイズを生成する関数
            float SimplexNoise(float3 x)
            {
                float3 p = floor(x);
                float3 f = frac(x);

                f = f * f * (3.0 - 2.0 * f);
                float n = p.x + p.y * 57.0 + 113.0 * p.z;

                return lerp(lerp(lerp(hash(n + 0.0), hash(n + 1.0), f.x),
                                 lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
                            lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
                                 lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y), f.z);
            }

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
                float4 cs = mul(UNITY_MATRIX_VP, i.positionWS);
                float2 projUV = cs.xy / cs.w * float2(0.5, -0.5) + 0.5;

                // HLSLシェーダーの魔導書のサンプルを改良したもの(uvの回転もランダム化)
                float t = SimplexNoise(i.positionCS.xyz);
                float u = (t - 0.5f) * 2.0f;

                float angle = hash(i.uv);
                angle = hash(float2(angle, t));

                // angleを使って2次元回転行列を作成
                half c = cos(angle);
                half s = sin(angle);
                half2x2 rotateMatrix = half2x2(c, -s, s, c);

                // tとangleは適当な値になっていて丁度良さそうなのでuvの値に使用
                float2 uv = normalize(float2(t, angle)) * u;

                // 回転を適用してuvを求める
                projUV += mul(rotateMatrix, uv) * _NoisePower;

                half3 finalColor = tex2D(_OpaqueColorTex, projUV) * _Color;

                return half4(finalColor, 1);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}