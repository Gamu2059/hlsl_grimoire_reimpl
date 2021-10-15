#ifndef CH14_DEFERRED_LIGHTING
#define CH14_DEFERRED_LIGHTING
#include "UnityCG.cginc"
#endif

struct Attributes
{
    float3 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

sampler2D _MainTex;
float4 _MainTex_ST;

sampler2D _GBufferAlbedoTex;
sampler2D _GBufferNormalTex;

float4 _LightColor;
float3 _LightVector;

Varyings DeferredLightingVert(Attributes attributes)
{
    Varyings o = (Varyings)0;
    o.positionCS = UnityObjectToClipPos(attributes.positionOS);
    o.uv = TRANSFORM_TEX(attributes.uv, _MainTex);
    return o;
}

half4 DeferredLightingFrag(Varyings i) : SV_Target
{
    half4 albedo = tex2D(_GBufferAlbedoTex, i.uv);
    half3 normal = tex2D(_GBufferNormalTex, i.uv).xyz * 2 - 1;
    float ldn = dot(_LightVector, normal);
    ldn = clamp(ldn, 0, 1);
    albedo.rgb *= ldn;
    return albedo;
}
