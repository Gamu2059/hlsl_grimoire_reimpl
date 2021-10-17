#ifndef CH14_DEFERRED_LIGHTING_PASS
#define CH14_DEFERRED_LIGHTING_PASS
#include "UnityCG.cginc"
#include "Ch14LightUtility.hlsl"
#include "Ch14ShadowUtility.hlsl"
#endif

struct Attributes
{
    float3 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionLVP : SV_POSITION;
    float2 uv : TEXCOORD0;
};

sampler2D _MainTex;
float4 _MainTex_ST;

sampler2D _GBufferAlbedoTex;
sampler2D _GBufferNormalTex;
sampler2D _GBufferWorldPosTex;
sampler2D _GBufferMetallicTex;
sampler2D _GBufferShadowTex;

Varyings DeferredLightingVert(Attributes attributes)
{
    Varyings o = (Varyings)0;
    o.positionLVP = UnityObjectToClipPos(attributes.positionOS);
    o.uv = TRANSFORM_TEX(attributes.uv, _MainTex);
    return o;
}

half4 DeferredLightingFrag(Varyings i) : COLOR0
{
    // GBufferからパラメータを取得
    half4 albedo = tex2D(_GBufferAlbedoTex, i.uv);
    half3 normal = tex2D(_GBufferNormalTex, i.uv).xyz * 2 - 1;
    float3 worldPos = tex2D(_GBufferWorldPosTex, i.uv).xyz;
    half2 metallicSmoothness = tex2D(_GBufferMetallicTex, i.uv).xy;
    half receiveShadow = tex2D(_GBufferShadowTex, i.uv).x;

    float3 viewDirWS = normalize(UnityWorldSpaceViewDir(worldPos));
    half metallic = metallicSmoothness.x;
    half smoothness = metallicSmoothness.y;
    half roughness = 1 - metallicSmoothness.y;

    Light light = GetMainLight();
    float dotNL = saturate(dot(normal, light.lightDir));
    half shadowFactor = SampleShadow(worldPos, dotNL);

    half3 finalColor = albedo;
    half3 lightColor = 0;

    // 影を受けない、または影の影響がほぼ無い時はライティングを行う
    if (receiveShadow == 0 || shadowFactor > 0.9)
    {
        //// ディズニーベースの拡散反射 ////
        // フレネル反射を考慮した拡散反射を計算
        float diffuseFromFresnel = CalcDiffuseFromFresnel(normal, light.lightDir, viewDirWS, roughness);

        // 正規化Lambert拡散反射を計算
        float3 normalizedLambertDiffuse = light.color * dotNL * UNITY_INV_PI;

        // 最終的な拡散反射を計算
        float3 diffuse = normalizedLambertDiffuse * diffuseFromFresnel;

        //// クックトランスモデルを利用した鏡面反射 ////
        // クックトランスモデルの鏡面反射を計算
        float3 specular = CookTorranceSpecular(normal, light.lightDir, viewDirWS, metallic);

        // 金属度が高ければ、鏡面反射はスペキュラカラー、低ければ白
        specular *= lerp(float3(1, 1, 1), light.color, metallic);

        lightColor = lerp(diffuse, specular, smoothness);
    }

    //// 影の反映 ////
    // 影を受けないならshadowFactorを無視、受けるならshadowFactorを考慮する
    half3 shadow = receiveShadow > 0.9 ? shadowFactor : 1;

    //// 光を反映 ////
    finalColor.xyz *= lightColor * shadow + GetAmbientLightColor();

    return half4(finalColor, 1);
}
