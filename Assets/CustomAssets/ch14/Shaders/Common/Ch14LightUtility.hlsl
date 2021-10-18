#ifndef CH14_LIGHT_UTILITY
#define CH14_LIGHT_UTILITY
#include "UnityCG.cginc"
#endif

float3 _LightDir;
float4 _LightColor;
float3 _AmbientLightColor;

struct Light
{
    float3 lightDir;
    float4 color;
};

/// <summary>
/// ライトを取得します
/// </summary>
Light GetMainLight()
{
    Light light = (Light)0;
    light.lightDir = _LightDir;
    light.color = _LightColor;
    return light;
}

/// <summary>
/// 環境光を取得します
/// </summary>
half3 GetAmbientLightColor()
{
    return _AmbientLightColor;
}

/// <summary>
/// この関数はフレネル反射を考慮した拡散反射率を計算します<br>
/// フレネル反射は、光が物体の表面で反射する現象のとこで、鏡面反射の強さになります<br>
/// 一方拡散反射は、光が物体の内部に入って、内部錯乱を起こして、拡散して反射してきた光のことです<br>
/// つまりフレネル反射が弱いときには、拡散反射が大きくなり、フレネル反射が強いときは、拡散反射が小さくなります<br>
/// </summary>
/// <param name="normal">法線</param>
/// <param name="lightDir">光源に向かうベクトル。光の方向と逆向きのベクトル。</param>
/// <param name="viewDir">視線に向かうベクトル。</param>
/// <param name="roughness">粗さ。0～1の範囲。</param>
float CalcDiffuseFromFresnel(float3 normal, float3 lightDir, float3 viewDir, float roughness)
{
    // 光源ベクトルと視線ベクトルのハーフベクトル
    float3 halfDir = normalize(lightDir + viewDir);

    float energyBias = lerp(0, 0.5, roughness);
    float energyFactor = lerp(1, 0.662, roughness); // 1/1.51 ≈ 0.662

    // 光源ベクトルとハーフベクトルの類似度
    float dotLH = saturate(dot(lightDir, halfDir));

    // 光源ベクトルとハーフベクトル、光が平行に入射した時の拡散反射率
    float Fd90 = energyBias + 2 * dotLH * dotLH * roughness;

    float dotNL = saturate(dot(normal, lightDir));
    float FL = 1 + (Fd90 - 1) * pow(1 - dotNL, 5);

    float dotNV = saturate(dot(normal, viewDir));
    float FV = 1 + (Fd90 - 1) * pow(1 - dotNV, 5);

    return FL * FV * energyFactor;
}

/// <summary>
/// Beckmann分布を計算する
/// </summary>
float CalcBeckmannDistribution(float microFacet, float dotNH)
{
    float cos2NH = dotNH * dotNH; // cosθ^2
    float sin2NH = 1 - cos2NH; // 1 - cosθ^2 = sinθ^2
    float tan2NH = sin2NH / cos2NH; // sinθ / cosθ = tanθ よって、 sinθ^2 / cosθ^2 = tanθ^2
    float cos4NH = dotNH * dotNH * dotNH * dotNH; // cosθ^4
    float m2 = microFacet * microFacet;
    return exp(-tan2NH / m2) / (4 * m2 * cos4NH);
}

/// <summary>
/// Schlick近似を計算する
/// </summary>
float CalcSchlickApproximation(float f0, float dotVH)
{
    return f0 + (1 - f0) * pow(1 - dotVH, 5);
}

/// <summary>
/// クックトランスモデルの鏡面反射を計算します
/// </summary>
/// <param name="normal">法線ベクトル</param>
/// <param name="lightDir">光源に向かうベクトル</param>
/// <param name="viewDir">視点に向かうベクトル</param>
/// <param name="metallic">金属度</param>
float CookTorranceSpecular(float3 normal, float3 lightDir, float3 viewDir, float metallic)
{
    // 表面の微細凹凸
    float microFacet = 0.76f;

    // 金属度を垂直入射の時のフレネル反射率として扱う
    // 金属度が高いほどフレネル反射は大きくなる
    float f0 = metallic;

    // 光源ベクトルと視線ベクトルのハーフベクトル
    float3 halfDir = normalize(lightDir + viewDir);

    // 各種ベクトルがどれくらい似ているかを内積を利用して求める
    float dotNH = saturate(dot(normal, halfDir));
    float dotVH = saturate(dot(viewDir, halfDir));
    float dotNL = saturate(dot(normal, lightDir));
    float dotNV = saturate(dot(normal, viewDir));

    // D項をBeckmann分布を用いて計算する
    float D = CalcBeckmannDistribution(microFacet, dotNH);

    // F項をSchlick近似を用いて計算する
    float F = CalcSchlickApproximation(f0, dotVH);

    // G項を求める
    float G = min(1.0f, min(2 * dotNH * dotNV / dotVH, 2 * dotNH * dotNL / dotVH));

    // m項を求める
    float m = UNITY_PI * dotNV * dotNH;

    // ここまで求めた値を利用して、クックトランスモデルの鏡面反射を求める
    return max(F * D * G / m, 0.0);
}
