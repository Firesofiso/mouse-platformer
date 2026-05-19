Shader "MouseButton/DustParticleLitAdditive"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Intensity ("Light Intensity", Float) = 1
        [Toggle] _DebugThreshold ("Debug Threshold View", Float) = 0
        _Threshold ("Visibility Threshold", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Plane"
        }

        Cull Off
        ZWrite Off
        Blend One One

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                float2 lightingUV : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            half4 _Color;
            half4 _MainTex_ST;
            half _Intensity;
            half _DebugThreshold;
            half _Threshold;

            #if USE_SHAPE_LIGHT_TYPE_0
            SHAPE_LIGHT(0)
            #endif
            #if USE_SHAPE_LIGHT_TYPE_1
            SHAPE_LIGHT(1)
            #endif
            #if USE_SHAPE_LIGHT_TYPE_2
            SHAPE_LIGHT(2)
            #endif
            #if USE_SHAPE_LIGHT_TYPE_3
            SHAPE_LIGHT(3)
            #endif

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                o.lightingUV = half2(ComputeScreenPos(o.positionCS).xy);
                return o;
            }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            half4 frag(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 col = tex * i.color;

                SurfaceData2D surfaceData;
                InputData2D inputData;

                surfaceData.albedo = col.rgb;
                surfaceData.alpha = col.a;
                surfaceData.mask = half4(1, 0, 0, 0);

                inputData.uv = i.uv;
                inputData.lightingUV = i.lightingUV;

                half4 lit = CombinedShapeLightShared(surfaceData, inputData);

                half luminance = dot(lit.rgb, half3(0.299, 0.587, 0.114));

                if (_DebugThreshold > 0.5)
                {
                    // With additive blend, we need bright RGB values to be visible
                    // Mode 1 (Threshold=0): raw lit output boosted hard
                    if (_Threshold < 0.01)
                        return half4(lit.rgb * 20.0, 0);

                    // Mode 2 (Threshold=1): lightingUV as color — should be a smooth red/green gradient across screen
                    if (_Threshold > 0.99)
                        return half4(i.lightingUV.x, i.lightingUV.y, 0, 0);

                    // Mode 3 (Threshold 0.01-0.99): bright red/green threshold
                    half3 debug = luminance > _Threshold ? half3(0, 1, 0) : half3(1, 0, 0);
                    return half4(debug, 0);
                }

                return half4(lit.rgb * col.a * _Intensity, 0);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "NormalsRendering" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(0, 0, 1, 1);
            }
            ENDHLSL
        }
    }
}
