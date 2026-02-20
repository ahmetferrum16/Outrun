// Assets/Shaders/FOV_StencilWrite.shader
Shader "FOV/StencilWrite"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always
        Blend One OneMinusSrcAlpha

        Pass
        {
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }
            // Renk yazma kapalý (sadece stencil)
            ColorMask 0
        }
    }
}
