// Assets/Shaders/FOV_StencilDarken.shader
Shader "FOV/StencilDarken"
{
    Properties
    {
        _Color("Color", Color) = (0,0,0,0.9)
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Stencil
            {
                Ref 1
                Comp NotEqual   // stencil != 1 ise çiz
                Pass Keep
            }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct appdata { float4 vertex:POSITION; };
            struct v2f { float4 pos:SV_POSITION; };
            v2f vert(appdata v){ v2f o; o.pos = UnityObjectToClipPos(v.vertex); return o; }
            fixed4 frag(v2f i):SV_Target{ return _Color; }
            ENDCG
        }
    }
}
