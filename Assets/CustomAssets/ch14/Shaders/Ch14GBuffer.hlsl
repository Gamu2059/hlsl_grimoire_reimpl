#ifndef CH14_GBUFFER
#define CH14_GBUFFER
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
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
};

struct Output
{
    half4 albedo : SV_Target0;
    half4 normal : SV_Target1;
};

sampler2D _MainTex;
float4 _MainTex_ST;

Varyings GBufferVert(Attributes attributes)
{
    Varyings o = (Varyings)0;
    o.positionCS = UnityObjectToClipPos(attributes.positionOS);
    o.uv = TRANSFORM_TEX(attributes.uv, _MainTex);
    o.normal = UnityObjectToWorldNormal(attributes.normal);
    return o;
}

Output GBufferFrag(Varyings i)
{
    Output o = (Output)0;
    o.albedo = tex2D(_MainTex, i.uv);
    o.normal = half4(i.normal * 0.5 + 0.5, 1);
    return o;
}
