#ifndef CH14_PRE_DEPTH_PASS
#define CH14_PRE_DEPTH_PASS
#include "UnityCG.cginc"
#endif

struct Attributes
{
    float3 positionOS : POSITION;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float4 positionVS : TEXCOORD0;
};

struct Output
{
    float4 depth : COLOR;
    float actDepth : DEPTH;
};

Varyings PreDepthVert(Attributes attributes)
{
    Varyings o = (Varyings)0;
    o.positionCS = UnityObjectToClipPos(attributes.positionOS);
    o.positionVS = mul(unity_MatrixV, mul(unity_ObjectToWorld, float4(attributes.positionOS, 1)));
    return o;
}

float DecodeLinear01Depth(float linear01Depth)
{
    return (1 / (1 - linear01Depth) - _ZBufferParams.y) / _ZBufferParams.x;
}

Output PreDepthFrag(Varyings i)
{
    const float depth = 1 - Linear01Depth(i.positionCS.z / i.positionCS.w);
    Output o = (Output)0;
    o.depth.x = depth;
    o.depth.y = i.positionVS.z / i.positionVS.w;
    o.depth.z = DecodeLinear01Depth(depth);
    // o.depth.w = i.positionVS.z;
    o.actDepth = depth;
    return o;
}
