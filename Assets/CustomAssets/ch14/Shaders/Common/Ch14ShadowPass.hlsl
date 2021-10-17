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
    float viewDistance : TEXCOOORD0;
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
    o.viewDistance = length(TransformObjectToLightView(attributes.positionOS).xyz);
    return o;
}

Output ShadowFrag(Varyings i)
{
    const float depth = i.positionLVP.z / i.positionLVP.w;
    const float lightViewDistance = Linear01LightDepth(i.viewDistance);
    Output o = (Output)0;
    // ライトスクリーン空間での深度値(x,y)とライトまでの線形な深度値を[0,1]にしたもの(z,w)
    o.depth.x = depth;
    o.depth.y = depth * depth;
    o.depth.z = lightViewDistance;
    o.depth.w = lightViewDistance * lightViewDistance;
    o.actDepth = depth;
    return o;
}
