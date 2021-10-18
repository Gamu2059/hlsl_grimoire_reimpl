Shader "Hidden/hlsl_grimoire/ch14/deferred" {
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off
        ZWrite Off
        
        // ディファードライティングの描画パス :0
        Pass
        {
            HLSLPROGRAM
            #include "Ch14DeferredLightingPass.hlsl"
            #pragma vertex DeferredLightingVert
            #pragma fragment DeferredLightingFrag
            ENDHLSL
        }
    }
}