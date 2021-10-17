#ifndef CH14_SHADOW_PASS
#define CH14_SHADOW_PASS
#include "UnityCG.cginc"
#include "Ch14ShadowUtility.hlsl"
#endif

struct Attributes
{
    float3 positionOS : POSITION;
};

struct Varyings
{
    float4 positionLVP : SV_POSITION;
};

struct Output
{
    float4 depth : COLOR;
    float actDepth : DEPTH;
};

Varyings ShadowVert(Attributes attributes)
{
    Varyings o = (Varyings)0;
    o.positionLVP = TransformObjectToLightViewProjection(attributes.positionOS);
    return o;
}

Output ShadowFrag(Varyings i)
{
    const float depth = i.positionLVP.z / i.positionLVP.w;
    Output o = (Output)0;
    o.depth = depth;
    o.actDepth = depth;
    return o;
}
