Shader "hlsl_grimoire/ch14/deferred/lit"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0
        [Toggle] _ReceiveShadow("Is Receive Shadow", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry" "RenderType" = "Opaque"
        }

        Blend One Zero

        // シャドウ描画パス
        Pass
        {
            Tags
            {
                "LightMode" = "CustomCh14Shadow"
            }

            HLSLPROGRAM
            #include "../Common/Ch14ShadowPass.hlsl"
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            ENDHLSL
        }

        // デプス描画パス
        Pass
        {
            Tags
            {
                "LightMode" = "CustomCh14PreDepth"
            }

            HLSLPROGRAM
            #include "../Common/Ch14PreDepthPass.hlsl"
            #pragma vertex PreDepthVert
            #pragma fragment PreDepthFrag
            ENDHLSL
        }

        // GBuffer描画パス
        Pass
        {
            Tags
            {
                "LightMode" = "CustomCh14GBuffer"
            }

            HLSLPROGRAM
            #include "../Common/Ch14GBufferPass.hlsl"
            #pragma vertex GBufferVert
            #pragma fragment GBufferFrag
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}