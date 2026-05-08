Shader "MouseButton/BackgroundFog"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Fog)]
        _FogColor ("Fog Color", Color) = (1,1,1,1)
        _FogStrength ("Fog Strength", Range(0,1)) = 0

        [Header(Blur)]
        _BlurStrength ("Blur Strength", Range(0,8)) = 0

        [Header(Atmosphere)]
        _DesatAmount ("Desaturation", Range(0,1)) = 0
        _ContrastAmount ("Contrast Reduction", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
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
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _MainTex_TexelSize;
            fixed4    _Color;
            fixed4    _FogColor;
            float     _FogStrength;
            float     _BlurStrength;
            float     _DesatAmount;
            float     _ContrastAmount;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                o.color  = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 ts = _MainTex_TexelSize.xy * _BlurStrength;

                fixed4 c =  tex2D(_MainTex, uv);
                c += tex2D(_MainTex, uv + float2( ts.x,    0));
                c += tex2D(_MainTex, uv + float2(-ts.x,    0));
                c += tex2D(_MainTex, uv + float2(    0,  ts.y));
                c += tex2D(_MainTex, uv + float2(    0, -ts.y));
                c += tex2D(_MainTex, uv + float2( ts.x,  ts.y));
                c += tex2D(_MainTex, uv + float2(-ts.x,  ts.y));
                c += tex2D(_MainTex, uv + float2( ts.x, -ts.y));
                c += tex2D(_MainTex, uv + float2(-ts.x, -ts.y));
                c /= 9.0;

                c *= i.color;

                // Desaturate
                float lum = dot(c.rgb, float3(0.299, 0.587, 0.114));
                c.rgb = lerp(c.rgb, float3(lum, lum, lum), _DesatAmount);

                // Reduce contrast toward mid-grey
                c.rgb = lerp(c.rgb, float3(0.5, 0.5, 0.5), _ContrastAmount);

                // Fog color
                c.rgb = lerp(c.rgb, _FogColor.rgb, _FogStrength);

                return c;
            }
            ENDCG
        }
    }
}
