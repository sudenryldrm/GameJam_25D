Shader "Storybook/ForestWind"
{
    Properties
    {
        _BaseMap("Leaves", 2D) = "white" {}
        _BaseColor("Leaf colour", Color) = (0.2,0.35,0.1,1)
        _WindStrength("Gentle wind", Float) = 0.12
        _WindBaseY("Foliage base", Float) = 0
        _WindHeight("Foliage height", Float) = 10
        _UseVertexWeights("Combined canopy weights", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor, _BaseMap_ST;
                float _UseVertexWeights;
            CBUFFER_END
            UNITY_INSTANCING_BUFFER_START(WindProperties)
                UNITY_DEFINE_INSTANCED_PROP(float, _WindStrength)
                UNITY_DEFINE_INSTANCED_PROP(float, _WindBaseY)
                UNITY_DEFINE_INSTANCED_PROP(float, _WindHeight)
            UNITY_INSTANCING_BUFFER_END(WindProperties)
            float _StoryWindTime;
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; float2 wind:TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float2 uv:TEXCOORD1; float3 positionWS:TEXCOORD2; half fog:TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes input)
            {
                Varyings output=(Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input); UNITY_TRANSFER_INSTANCE_ID(input,output); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 p=TransformObjectToWorld(input.positionOS.xyz);
                float baseY=UNITY_ACCESS_INSTANCED_PROP(WindProperties,_WindBaseY);
                float height=UNITY_ACCESS_INSTANCED_PROP(WindProperties,_WindHeight);
                float strength=UNITY_ACCESS_INSTANCED_PROP(WindProperties,_WindStrength);
                float weight=lerp(saturate((p.y-baseY)/max(.1,height)),input.wind.x,_UseVertexWeights);
                weight*=weight;
                float phase=p.x*.19+p.z*.13;
                float breeze=sin(_StoryWindTime*.85+phase)+.23*sin(_StoryWindTime*1.7+phase*1.3);
                p.x+=breeze*strength*weight;
                p.z+=cos(_StoryWindTime*.63+phase)*strength*.3*weight;
                output.positionWS=p;output.positionCS=TransformWorldToHClip(p);
                output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                output.uv=TRANSFORM_TEX(input.uv,_BaseMap);output.fog=ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half3 normal=normalize(input.normalWS);
                Light sun=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 light=SampleSH(normal)+sun.color*saturate(dot(normal,sun.direction))*sun.shadowAttenuation;
                half3 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).rgb*_BaseColor.rgb*light;
                return half4(MixFog(color,input.fog),1);
            }
            ENDHLSL
        }
    }
}
