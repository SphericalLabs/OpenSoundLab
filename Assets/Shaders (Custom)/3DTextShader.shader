Shader "GUI/3D Text Shader Occluded URP" {
    Properties {
        _MainTex ("Font Texture", 2D) = "white" {}
        _Color ("Text Color", Color) = (1,1,1,1)
    }

    SubShader {
        Tags { 
            "Queue" = "Transparent" 
            "IgnoreProjector" = "True" 
            "RenderType" = "Transparent" 
        }
        // Standard Alpha Blending
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION

            // Include URP's core library
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Include occlusion for URP
            #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/URP/EnvironmentOcclusionURP.hlsl"

            // Define sampler and texture
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // Define color property
            float4 _Color;

            // Vertex input structure
            struct Attributes {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // Vertex output structure with occlusion
            struct Varyings {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                META_DEPTH_VERTEX_OUTPUT(1) // Adds necessary fields for occlusion
                UNITY_VERTEX_OUTPUT_STEREO // Supports stereo rendering
            };

            // Vertex shader
            Varyings vert (Attributes IN) {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                // Transform object space position to clip space
                OUT.position = TransformObjectToHClip(IN.position.xyz);
                OUT.uv = IN.uv;
                META_DEPTH_INITIALIZE_VERTEX_OUTPUT(OUT, IN.position.xyz); // Initializes occlusion data
                return OUT;
            }

            // Fragment shader
            half4 frag (Varyings IN) : SV_Target {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN); // Supports stereo rendering

                // Sample the main texture
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                // Set RGB to _Color and preserve texture's alpha
                half4 finalColor = half4(_Color.rgb, texColor.a);

                // Apply occlusion
                META_DEPTH_OCCLUDE_OUTPUT_PREMULTIPLY(IN, finalColor, 0.0); // 0.0 for no depth bias

                return finalColor;
            }

            ENDHLSL
        }
    }

    // Fallback for unsupported render pipelines
    Fallback "Diffuse"
}
