Shader "Hovl/Particles/Add_CenterGlow"
{
	Properties
	{	
		[MainTexture] _MainTex("MainTex", 2D) = "white" {}
		_SpeedMainTexUV("Speed MainTex U/V", Vector) = (0,0,0,0)
		[HDR] _Color("Color", Color) = (0.5,0.5,0.5,1)
		_Emission("Emission", Float) = 2
		[Toggle(USE_DEPTH)] _Usedepth ("Use depth?", Float) = 0
		_Depthpower ("Depth power", Float) = 1
		[Enum(UnityEngine.Rendering.CullMode)] _CullMode("Culling", Float) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1 // One
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 1 // One
		
		// Stencil (for UI compatibility)
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		[Enum(UnityEngine.Rendering.ColorWriteMask)] _ColorMask ("Color Mask", Float) = 15
		
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
	}

	SubShader
	{
		Tags 
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"RenderPipeline" = "UniversalPipeline"
		}
		
		Blend [_SrcBlend] [_DstBlend]
		ColorMask [_ColorMask]
		Cull [_CullMode]
		Lighting Off 
		ZWrite Off
		ZTest LEqual
		
		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Pass 
		{
			Name "ParticlePass"
			
			HLSLPROGRAM
			#pragma target 2.0
			
			// Shader features
			#pragma shader_feature_local USE_DEPTH
			#pragma shader_feature_local UNITY_UI_ALPHACLIP
			
			// Multi-compile variants
			#pragma multi_compile_fog
			#pragma multi_compile_instancing
			
			// Vertex and fragment functions
			#pragma vertex vert
			#pragma fragment frag
			
			// URP Core includes
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			
			// Depth texture for soft particles (outside CBUFFER)
			#if defined(USE_DEPTH)
				TEXTURE2D_X_FLOAT(_CameraDepthTexture);
				SAMPLER(sampler_CameraDepthTexture);
			#endif
			
			// Main texture (outside CBUFFER for SRP Batcher compatibility)
			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			
			// ============================================
			// SRP Batcher: All material properties in UnityPerMaterial CBUFFER
			// ============================================
			CBUFFER_START(UnityPerMaterial)
				half4 _MainTex_ST;
				half4 _SpeedMainTexUV;
				half4 _Color;
				half _Emission;
				half _Depthpower;
			CBUFFER_END
			
			// Vertex input
			struct Attributes
			{
				float4 positionOS : POSITION;
				half4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			// Vertex output
			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				half4 color : COLOR;
				float2 uv : TEXCOORD0;
				#if defined(USE_DEPTH)
					float4 screenPos : TEXCOORD1;
				#endif
				half fogFactor : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			// ============================================
			// Vertex Shader
			// ============================================
			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				// Transform to clip space
				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionCS = vertexInput.positionCS;
				
				#if defined(USE_DEPTH)
					output.screenPos = vertexInput.positionNDC;
				#endif
				
				// Vertex color
				output.color = input.color;
				
				// UV with tiling/offset + panning (optimized - done in vertex shader)
				output.uv = input.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				output.uv += _Time.y * _SpeedMainTexUV.xy;
				
				// Fog
				output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
				
				return output;
			}

			// ============================================
			// Fragment Shader
			// ============================================
			half4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				
				half4 color = input.color;
				
				// Soft particles depth fade
				#if defined(USE_DEPTH)
					float2 screenUV = input.screenPos.xy / input.screenPos.w;
					float sceneDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r;
					float sceneZ = LinearEyeDepth(sceneDepth, _ZBufferParams);
					float particleZ = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
					half depthFade = saturate((sceneZ - particleZ) / _Depthpower);
					color.a *= depthFade;
				#endif

				// Sample main texture once
				half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
				
				// Optimized color calculation
				// Original: mainTex * _Color * color * mainTex.a * _Color.a * color.a * _Emission
				// Simplified: mainTex * _Color * color * (mainTex.a * _Color.a * color.a) * _Emission
				half alpha = mainTex.a * _Color.a * color.a;
				half4 finalColor = mainTex * _Color * color * alpha * _Emission;
				
				// Apply fog
				finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);
				
				// Alpha clip for UI
				#ifdef UNITY_UI_ALPHACLIP
					clip(finalColor.a - 0.001);
				#endif
				
				return finalColor;
			}
			ENDHLSL
		}
	}
	
	// Fallback for older systems
	FallBack "Hidden/Universal Render Pipeline/FallbackError"
}