#ifndef CH14_DEPTH
#define CH14_DEPTH
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
    float3 positionWS : TEXCOORD1;
};

sampler2D _MainTex;
float4 _MainTex_ST;

Varyings DepthVert(Attributes attributes)
{
    Varyings o = (Varyings)0;
    o.positionCS = UnityObjectToClipPos(attributes.positionOS);
    o.uv = TRANSFORM_TEX(attributes.uv, _MainTex);
    return o;
}

float DepthFrag(Varyings i) : SV_Target
{
    return 1 - Linear01Depth(length(i.positionCS.z));
}
