#ifndef CH14_SHADOW_UTILITY
#define CH14_SHADOW_UTILITY
#include "UnityCG.cginc"
#pragma multi_compile _ PCF_SHADOW VSM_SHADOW
#pragma target 3.5
#endif

float4x4 _WorldToLightViewProjection;
float4x4 _LightView;
float4 _LightZParam;
float _ShadowBias;
float _ShadowNormalBias;
sampler2D _ShadowDepthTex;
float4 _ShadowDepthTex_TexelSize;

#if defined(PCF_SHADOW)
int _PcfSampleCount;
float _PcfSampleSpace;
#endif

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
/// ワールド空間からライトビュー空間へと変換します
/// </summary>
float4 TransformWorldToLightView(float3 positionWS)
{
    return mul(_LightView, positionWS);
}

/// <summary>
/// オブジェクト空間からライトビュー空間へと変換します
/// </summary>
float4 TransformObjectToLightView(float3 positionOS)
{
    return mul(_LightView, mul(unity_ObjectToWorld, float4(positionOS, 1)));
}

/// <summary>
/// 線形なライトからの深度値を[0,1]へと深度値に変換します
/// </summary>
float Linear01LightDepth(float depth)
{
    return (depth - _LightZParam.x) / (_LightZParam.y - _LightZParam.x);
}

/// <summary>
/// ワールド空間からシャドウUV空間へと変換します
/// </summary>
float2 TransformWorldToShadowUV(float3 positionWS)
{
    float4 positionLVP = TransformWorldToLightViewProjection(positionWS);
    return positionLVP.xy / positionLVP.w * float2(0.5f, -0.5f) + 0.5f;
}

half SampleHardShadow(float4 positionLVP, float2 uv, float dotNL)
{
    float zInLVP = positionLVP.z / positionLVP.w;
    float zInShadow = tex2D(_ShadowDepthTex, uv).x;
    float bias = _ShadowNormalBias * tan(acos(dotNL)) + _ShadowBias;
    return zInLVP < zInShadow - bias ? 0.0h : 1.0h;
}

half SamplePcfShadow(float4 positionLVP, float2 uv, float dotNL)
{
    float zInLVP = positionLVP.z / positionLVP.w;
    float zInShadow = tex2D(_ShadowDepthTex, uv).x;
    float bias = _ShadowNormalBias * tan(acos(dotNL)) + _ShadowBias;
    if (zInLVP < zInShadow - bias)
    {
        #if defined(PCF_SHADOW)
        float shadowCount = 0;
            
        UNITY_LOOP
        for (int y = -_PcfSampleCount; y <= _PcfSampleCount; y++)
        {
            UNITY_LOOP
            for (int x = -_PcfSampleCount; x <= _PcfSampleCount; x++)
            {
                if (y == 0 && x == 0)
                {
                    continue;
                }
            
                float2 offset = float2(x * _ShadowDepthTex_TexelSize.x, y * _ShadowDepthTex_TexelSize.y) *
                    _PcfSampleSpace;
                float zInShadowNeighbor = tex2D(_ShadowDepthTex, uv + offset);
                shadowCount += zInLVP < zInShadowNeighbor - bias ? 1 : 0;
            }
        }
        float sizeCount = _PcfSampleCount * 2 + 1;
        sizeCount = sizeCount * sizeCount - 1;
        float power = shadowCount / sizeCount;
        return 1 - power * power;
        #endif
    }

    return 1.0h;
}

half SampleVsmShadow(float3 positionWS, float2 uv, float dotNL)
{
    float distFromLight = Linear01LightDepth(length(TransformWorldToLightView(positionWS).xyz));
    float2 shadowValue = tex2D(_ShadowDepthTex, uv).zw;
    float distFromShadow = shadowValue.x;
    float bias = _ShadowNormalBias * tan(acos(dotNL)) + _ShadowBias;
    if (distFromLight < distFromShadow - bias)
    {
        float depthSqrt = shadowValue.x * shadowValue.x;
        float variance = saturate(shadowValue.y - depthSqrt);
        float md = distFromLight - shadowValue.x;
        return variance / (variance + md * md);
    }

    return 1.0h;
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
        #if defined(VSM_SHADOW)
        return SampleVsmShadow(positionWS, uv, dotNL);
        #elif defined(PCF_SHADOW)
        return SamplePcfShadow(positionLVP, uv, dotNL);
        #else
        return SampleHardShadow(positionLVP, uv, dotNL);
        #endif
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
