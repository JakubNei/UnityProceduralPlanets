// Deffered + shadow passes copied from Unity built-in shader source
// modified to take normal in model space from _BumpMap and put it into world normal
// Unity's normal Standart shader has normal in tangent space

Shader "Custom/Procedural Planets/Chunk"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
        _GlossMapScale("Smoothness Scale", Range(0.0, 1.0)) = 1.0
        [Enum(Metallic Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel ("Smoothness texture channel", Float) = 0

        [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0

        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        _Parallax ("Height Scale", Range (0.005, 0.08)) = 0.02
        _ParallaxMap ("Height Map", 2D) = "black" {}

        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}

        _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        _DetailMask("Detail Mask", 2D) = "white" {}

        _DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
        _DetailNormalMapScale("Scale", Float) = 1.0
        _DetailNormalMap("Normal Map", 2D) = "bump" {}

        [Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0


        // Blending state
        [HideInInspector] _Mode ("__mode", Float) = 0.0
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
    }

    CGINCLUDE
        #define UNITY_SETUP_BRDF_INPUT MetallicSetup
    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
        LOD 300
    
        // ------------------------------------------------------------------
        //  Shadow rendering pass
        Pass {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On ZTest LEqual

            CGPROGRAM
            #pragma target 3.0

            // -------------------------------------


            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature _PARALLAXMAP
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            // Uncomment the following line to enable dithering LOD crossfade. Note: there are more in the file to uncomment for other passes.
            //#pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma vertex vertShadowCaster
            #pragma fragment fragShadowCaster

            #include "UnityStandardShadow.cginc"

            ENDCG
        }

        // ------------------------------------------------------------------
        //  Deferred pass
        Pass
        {
            Name "DEFERRED"
            Tags { "LightMode" = "Deferred" }

            CGPROGRAM
            #pragma target 3.0
            #pragma exclude_renderers nomrt


            // -------------------------------------

            //#pragma shader_feature _NORMALMAP
			#define _NORMALMAP 1

            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _EMISSION
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature ___ _DETAIL_MULX2
            #pragma shader_feature _PARALLAXMAP

            #pragma multi_compile_prepassfinal
            #pragma multi_compile_instancing
            // Uncomment the following line to enable dithering LOD crossfade. Note: there are more in the file to uncomment for other passes.
            //#pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma vertex vertDeferred
            #pragma fragment customFragDeferred

            #include "UnityStandardCore.cginc"

		#define CUSTOM_FRAGMENT_SETUP(x) FragmentCommonData x = \
			CustomFragmentSetup(i.tex, i.eyeVec, IN_VIEWDIR4PARALLAX(i), IN_WORLDPOS(i));

			// parallax transformed texcoord is used to sample occlusion
			inline FragmentCommonData CustomFragmentSetup(inout float4 i_tex, half3 i_eyeVec, half3 i_viewDirForParallax, half3 i_posWorld)
			{
				i_tex = Parallax(i_tex, i_viewDirForParallax);

				half alpha = Alpha(i_tex.xy);
		#if defined(_ALPHATEST_ON)
				clip(alpha - _Cutoff);
		#endif

				FragmentCommonData o = UNITY_SETUP_BRDF_INPUT(i_tex);

				// DEBUG
				//o = (FragmentCommonData)0;
				//o.diffColor = float3(1, 1, 1);
				//o.specColor = float3(0, 0, 0);
				//o.specColor = o.diffColor;
				//o.oneMinusReflectivity = 0;
				//o.smoothness = 0;

		#ifdef _NORMALMAP
				//o.normalWorld = UnityObjectToWorldDir(NormalInTangentSpace(i_tex).xyz);
				//o.normalWorld = NormalInTangentSpace(i_tex);
				//o.normalWorld = UnpackScaleNormal(tex2D(_BumpMap, i_tex.xy), 1);
				//o.normalWorld = tex2D(_BumpMap, i_tex.xy).xyz * 2 - 1;
				o.normalWorld = NormalizePerPixelNormal(UnityObjectToWorldDir(tex2D(_BumpMap, i_tex.xy).xyz * 2 - 1));
		#else
				o.normalWorld = float3(1, 0, 0);
		#endif
				o.eyeVec = NormalizePerPixelNormal(i_eyeVec);
				o.posWorld = i_posWorld;

				// NOTE: shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
				o.diffColor = PreMultiplyAlpha(o.diffColor, alpha, o.oneMinusReflectivity, /*out*/ o.alpha);
				return o;
			}


			void customFragDeferred(
				VertexOutputDeferred i,
				out half4 outGBuffer0 : SV_Target0,
				out half4 outGBuffer1 : SV_Target1,
				out half4 outGBuffer2 : SV_Target2,
				out half4 outEmission : SV_Target3          // RT3: emission (rgb), --unused-- (a)
			)
			{

				UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

				CUSTOM_FRAGMENT_SETUP(s)

				// no analytic lights in this pass
				UnityLight dummyLight = DummyLight();
				half atten = 1;

				// only GI
				half occlusion = Occlusion(i.tex.xy);
		#if UNITY_ENABLE_REFLECTION_BUFFERS
				bool sampleReflectionsInDeferred = false;
		#else
				bool sampleReflectionsInDeferred = true;
		#endif

				UnityGI gi = FragmentGI(s, occlusion, i.ambientOrLightmapUV, atten, dummyLight, sampleReflectionsInDeferred);

				half3 emissiveColor = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect).rgb;

		#ifdef _EMISSION
				emissiveColor += Emission(i.tex.xy);
		#endif

		#ifndef UNITY_HDR_ON
				emissiveColor.rgb = exp2(-emissiveColor.rgb);
		#endif

				UnityStandardData data;
				data.diffuseColor = s.diffColor;
				data.occlusion = occlusion;
				data.specularColor = s.specColor;
				data.smoothness = s.smoothness;
				data.normalWorld = s.normalWorld;

				UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

				// Emissive lighting buffer
				outEmission = half4(emissiveColor, 1);

			}

            ENDCG
        }

    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
        LOD 150

        // ------------------------------------------------------------------
        //  Shadow rendering pass
        Pass {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On ZTest LEqual

            CGPROGRAM
            #pragma target 2.0

            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma skip_variants SHADOWS_SOFT
            #pragma multi_compile_shadowcaster

            #pragma vertex vertShadowCaster
            #pragma fragment fragShadowCaster

            #include "UnityStandardShadow.cginc"

            ENDCG
        }

    }


    FallBack "VertexLit"
    //CustomEditor "StandardShaderGUI"
}
