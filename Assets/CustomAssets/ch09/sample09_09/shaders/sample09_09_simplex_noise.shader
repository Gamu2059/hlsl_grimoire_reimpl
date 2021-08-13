Shader "hlsl_grimoire/sample09_09/simplex_noise"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _NoisePower("Noise Power", Range(0, 0.5)) = 0
        [Toggle(USE_IMPROVE)] _UseImprove("Use Improve", float) = 0
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    ENDHLSL

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _ USE_IMPROVE

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
            float _NoisePower;

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
                o.positionCS = TransformObjectToHClip(attributes.positionOS);
                o.uv = TRANSFORM_TEX(attributes.uv, _MainTex);
                o.color = attributes.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                #ifdef USE_IMPROVE
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
                uv = i.uv + mul(rotateMatrix, uv) * _NoisePower;
                #else
                // HLSLシェーダーの魔導書のサンプルそのまま
                float t = SimplexNoise(i.positionCS.xyz);
                t = (t - 0.5f) * 2.0f;
                float2 uv = i.uv + t * _NoisePower;
                #endif

                return i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}