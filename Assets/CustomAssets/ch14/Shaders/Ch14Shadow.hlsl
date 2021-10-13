#ifndef CH14_SHADOW
#define CH14_SHADOW
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
    float4 lightView : TEXCOORD1;
};

sampler2D _MainTex;
float4 _MainTex_ST;
float4x4 _LvpMat;

Varyings ShadowVert(Attributes attributes)
{
    Varyings o = (Varyings)0;
    o.lightView = mul(unity_ObjectToWorld, float4(attributes.positionOS, 1.0));
    o.lightView = mul(_LvpMat, o.lightView);
    o.positionCS = o.lightView;
    o.uv = TRANSFORM_TEX(attributes.uv, _MainTex);
    return o;
}

half4 ShadowFrag(Varyings i) : SV_Target
{
    return 1;
}
