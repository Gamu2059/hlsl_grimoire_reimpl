#ifndef CH14_SHADOW_UTILITY
#define CH14_SHADOW_UTILITY
#include "UnityCG.cginc"
#endif

float4x4 _WorldToLightViewProjection;
float _ShadowBias;
float _ShadowNormalBias;
sampler2D _ShadowDepthTex;

/// <summary>
/// ワールド空間からLVP空間へと変換します
/// </summary>
float4 TransformWorldToLightViewProjection(float3 positionWS)
{
    return mul(_WorldToLightViewProjection, float4(positionWS, 1));
}

/// <summary>
/// オブジェクト空間からLVP空間へと変換します
/// </summary>
float4 TransformObjectToLightViewProjection(float3 positionOS)
{
    return mul(_WorldToLightViewProjection, mul(unity_ObjectToWorld, float4(positionOS, 1)));
}

/// <summary>
/// ワールド空間からシャドウUV空間へと変換します
/// </summary>
float2 TransformWorldToShadowUV(float3 positionWS)
{
    float4 positionLVP = TransformWorldToLightViewProjection(positionWS);
    return positionLVP.xy / positionLVP.w * float2(0.5f, -0.5f) + 0.5f;
}

/// <summary>
/// 影を取得します
/// </summary>
/// <param name="positionWS">ワールド空間座標</param>
/// <param name="dotNL">ワールド空間法線とワールド空間ライトベクトルの内積</param>
half SampleShadow(float3 positionWS, float dotNL)
{
    float4 positionLVP = TransformWorldToLightViewProjection(positionWS);
    float2 uv = positionLVP.xy / positionLVP.w * float2(0.5f, -0.5f) + 0.5f;

    if (uv.x > 0 && uv.x < 1 && uv.y > 0 && uv.y < 1)
    {
        float zInLVP = positionLVP.z / positionLVP.w;
        float zInShadow = tex2D(_ShadowDepthTex, uv).x;
        float bias = _ShadowNormalBias * tan(acos(dotNL)) + _ShadowBias;
        if (zInLVP < zInShadow - bias)
        {
            return 0.0h;
        }
    }

    return 1.0h;
}

/// <summary>
/// 影の標準色を取得します
/// </summary>
half3 DefaultShadowColor()
{
    return 0.0h;
}