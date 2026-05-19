Shader "MouseButton/ElectricParticleStencil"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+1"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Stencil
        {
            Ref 1
            Comp Equal
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
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _MainTex_ST;
            CBUFFER_END

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.color = i.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color * _Color;
                return col;
            }
            ENDHLSL
        }
    }
}
