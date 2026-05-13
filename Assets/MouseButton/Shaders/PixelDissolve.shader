Shader "MouseButton/PixelDissolve"
{
    Properties
    {
        _Color     ("Color", Color) = (0,0,0,1)
        _Threshold ("Threshold", Range(0,1)) = 0
        _RefWidth  ("Ref Width", Float) = 320
        _RefHeight ("Ref Height", Float) = 180
    }

    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType" = "Transparent" }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex    : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            fixed4 _Color;
            float  _Threshold;
            float  _RefWidth;
            float  _RefHeight;

            float hash(float2 p)
            {
                p = frac(p * float2(0.1031, 0.1030));
                p += dot(p, p + 33.33);
                return frac(p.x * p.y);
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex    = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv         = i.screenPos.xy / i.screenPos.w;
                float2 pixelCoord = floor(uv * float2(_RefWidth, _RefHeight));
                clip(_Threshold - hash(pixelCoord));
                return _Color;
            }
            ENDCG
        }
    }
}
