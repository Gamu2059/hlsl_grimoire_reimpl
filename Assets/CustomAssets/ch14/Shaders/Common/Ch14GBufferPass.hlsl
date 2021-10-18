#ifndef CH14_GBUFFER_PASS
#define CH14_GBUFFER_PASS
#include "UnityCG.cginc"
#endif

struct Attributes
{
    float3 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCORRD0;
    float2 uv : TEXCOORD1;
    float3 normal : NORMAL;
};

struct Output
{
    half4 albedo : COLOR0;
    half4 normal : COLOR1;
    half4 worldPos : COLOR2;
    half4 metalSmooth : COLOR3;
    half4 shadowParam : COLOR4;
};

#if !defined(DONT_USE_GBUFFER_PARAM)
CBUFFER_START(UnityPerMaterial)
sampler2D _MainTex;
float4 _MainTex_ST;
half4 _Color;
half _Metallic;
half _Smoothness;
half _ReceiveShadow;
CBUFFER_END

Varyings GBufferVert(Attributes attributes)
{
    Varyings o = (Varyings)0;
    o.positionCS = UnityObjectToClipPos(attributes.positionOS);
    o.positionWS = mul(unity_ObjectToWorld, float4(attributes.positionOS.xyz, 1)).xyz;
    o.uv = TRANSFORM_TEX(attributes.uv, _MainTex);
    o.normal = UnityObjectToWorldNormal(attributes.normal);
    return o;
}

Output GBufferFrag(Varyings i)
{
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
#endif
