Shader "MouseButton/ElectricPulse"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0.4, 0.7, 1, 0.3)
        _PulseSpeed ("Pulse Speed", Float) = 3.0
        _PulseMin ("Pulse Min", Range(0, 1)) = 0.1
        _PulseMax ("Pulse Max", Range(0, 1)) = 0.5
        _FlickerSpeed ("Flicker Speed", Float) = 15.0
        _FlickerAmount ("Flicker Amount", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Stencil
        {
            Ref 1
            Comp Always
            Pass Replace
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _MainTex_ST;
                float _PulseSpeed;
                float _PulseMin;
                float _PulseMax;
                float _FlickerSpeed;
                float _FlickerAmount;
            CBUFFER_END

            float hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                clip(texCol.a - 0.5);

                float pulse = lerp(_PulseMin, _PulseMax,
                    sin(_Time.y * _PulseSpeed) * 0.5 + 0.5);

                float flicker = 1.0 - _FlickerAmount * step(0.92,
                    hash(floor(_Time.y * _FlickerSpeed)));

                half4 col = _Color;
                col.a *= pulse * flicker * texCol.a;
                return col;
            }
            ENDHLSL
        }
    }
}
