// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "UI/Default_OverlayNoZTest"
 {
     Properties
     {
         [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
         _Color ("Tint", Color) = (1,1,1,1)
          
         _StencilComp ("Stencil Comparison", Float) = 8
         _Stencil ("Stencil ID", Float) = 0
         _StencilOp ("Stencil Operation", Float) = 0
         _StencilWriteMask ("Stencil Write Mask", Float) = 255
         _StencilReadMask ("Stencil Read Mask", Float) = 255
  
         _ColorMask ("Color Mask", Float) = 15
     }
  
     SubShader
     {
         Tags
         { 
             "Queue"="Overlay" 
             "IgnoreProjector"="True" 
             "RenderType"="Transparent" 
             "PreviewType"="Plane"
             "CanUseSpriteAtlas"="True"
         }
          
         Stencil
         {
             Ref [_Stencil]
             Comp [_StencilComp]
             Pass [_StencilOp] 
             ReadMask [_StencilReadMask]
             WriteMask [_StencilWriteMask]
         }
  
         Cull Off
         Lighting Off
         ZWrite Off
         ZTest Off
         Blend SrcAlpha OneMinusSrcAlpha
         ColorMask [_ColorMask]
  
         Pass
         {
         CGPROGRAM
             #pragma vertex vert
             #pragma fragment frag
             #include "UnityCG.cginc"
              
             struct appdata_t
             {
                 float4 vertex   : POSITION;
                 float4 color    : COLOR;
                 float2 texcoord : TEXCOORD0;

				 UNITY_VERTEX_INPUT_INSTANCE_ID 
             };
  
             struct v2f
             {
                 float4 vertex   : SV_POSITION;
                 fixed4 color    : COLOR;
                 half2 texcoord  : TEXCOORD0;

				 UNITY_VERTEX_OUTPUT_STEREO
             };
              
             fixed4 _Color;
             fixed4 _TextureSampleAdd; //Added for font color support
  
             v2f vert(appdata_t IN)
             {
                 v2f OUT;

				UNITY_SETUP_INSTANCE_ID(IN); //Insert
				UNITY_INITIALIZE_OUTPUT(v2f, OUT); //Insert
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT); //Insert

                 OUT.vertex = UnityObjectToClipPos(IN.vertex);
                 OUT.texcoord = IN.texcoord;
 #ifdef UNITY_HALF_TEXEL_OFFSET
                 OUT.vertex.xy += (_ScreenParams.zw-1.0)*float2(-1,1);
 #endif
                 OUT.color = IN.color * _Color;
                 return OUT;
             }
  
             sampler2D _MainTex;
  
             fixed4 frag(v2f IN) : SV_Target
             {
	        	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
             half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;  //Added for font color support
             clip (color.a - 0.01);
             return color;
             }
         ENDCG
         }
     }
 }
