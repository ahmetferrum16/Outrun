Shader "Custom/FOVMaskShader"
{
    Properties
    {
        _FOVColor ("Mask Color", Color) = (0,0,0,1)
        _PlayerPosition ("Player Position", Vector) = (0,0,0,0)
        _ViewDistance ("View Distance", Float) = 5
        _ViewAngle ("View Angle", Float) = 90
        _Direction ("Direction", Vector) = (1,0,0,0)
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            ZTest Always
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _FOVColor;
            float4 _PlayerPosition;
            float _ViewDistance;
            float _ViewAngle;
            float4 _Direction;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 worldPos : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 dirToPixel = i.worldPos - _PlayerPosition.xy;
                float dist = length(dirToPixel);
                float2 normDir = normalize(dirToPixel);

                float angle = degrees(acos(dot(normDir, normalize(_Direction.xy))));

                if (dist <= _ViewDistance && angle <= (_ViewAngle / 2.0))
                {
                    return fixed4(0, 0, 0, 0); // Transparent inside FOV
                }

                return _FOVColor; // Black outside FOV
            }
            ENDCG
        }
    }
}
