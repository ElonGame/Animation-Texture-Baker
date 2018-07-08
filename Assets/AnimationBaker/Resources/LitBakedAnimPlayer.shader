Shader "BakedAnimation/Lit"
{
    Properties
    {
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_PosTex("Position Texture", 2D) = "black"{}
		_NmlTex("Normal Texture", 2D) = "white"{}
		_TotalAnimations("Total Animations", float) = 0
		_Clip("Current Animation", float) = 0
		_PlaybackSpeed("Playback Speed", float) = 1
		[HideInInspector] _TexHeight("Texture Height", float) = 0
		[HideInInspector] _Yoff("Y Offset", float) = 0
    }
    SubShader
    {
        Pass
        {
            Tags {"LightMode"="ForwardBase"}
			Cull Off
            CGPROGRAM
				#pragma target 4.5
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"
				#include "Lighting.cginc"
				#include "AutoLight.cginc"
				#pragma multi_compile_instancing
				#define ts _PosTex_TexelSize
				RWStructuredBuffer<float4> _RandomWrite : register(u1);

				struct v2f
				{
					float2 uv : TEXCOORD0;
					SHADOW_COORDS(1)
					float3 normal : TEXCOORD2;
					fixed3 diff : COLOR0;
					fixed3 ambient : COLOR1;
					float4 pos : SV_POSITION;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};
				sampler2D _MainTex, _PosTex, _NmlTex;
				float4 _PosTex_TexelSize, _Color;
				float _PlaybackSpeed;
				float _TotalFrames;
				float _AnimationFrameCount;
				float _Clip;
				float _Yoff;

				UNITY_INSTANCING_BUFFER_START(Props)
					UNITY_DEFINE_INSTANCED_PROP(float, _CurrentAnimation)
					UNITY_DEFINE_INSTANCED_PROP(float, _EntityID)
				UNITY_INSTANCING_BUFFER_END(Props)

				v2f vert (appdata_base v, uint vid : SV_VertexID, uint iid: SV_INSTANCEID)
				{
					#include "GetPosNorm.cginc"
					v2f o;
					v.vertex = o.pos = UnityObjectToClipPos(pos);
					v.normal = o.normal = UnityObjectToWorldNormal(normal);
					o.uv = v.texcoord;
					half nl = max(0, dot(o.normal, _WorldSpaceLightPos0.xyz));
					o.diff = nl * _LightColor0.rgb;
					o.ambient = ShadeSH9(half4(o.normal,1));
					TRANSFER_SHADOW(o)
					return o;
				}
				fixed4 frag (v2f i) : SV_Target
				{
					fixed4 col = tex2D(_MainTex, i.uv) * _Color * 1.5;
					fixed shadow = SHADOW_ATTENUATION(i);
					fixed3 lighting = i.diff * shadow + i.ambient;
					col.rgb *= lighting;
					return col;
				}
            ENDCG
        }
		
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 4.5
				#pragma multi_compile_shadowcaster
				#pragma multi_compile_instancing
				#include "UnityCG.cginc"
				#pragma multi_compile ___ ANIM_LOOP
				#define ts _PosTex_TexelSize
				RWStructuredBuffer<float4> _RandomWrite : register(u1);

				struct v2f {
					V2F_SHADOW_CASTER;
					UNITY_VERTEX_OUTPUT_STEREO
                	UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				sampler2D _MainTex, _PosTex, _NmlTex;
				float4 _PosTex_TexelSize, _Color;
				float _PlaybackSpeed;
				float _TotalFrames;
				float _AnimationFrameCount;
				float _Clip;
				float _Yoff;
				
				UNITY_INSTANCING_BUFFER_START(Props)
					// UNITY_DEFINE_INSTANCED_PROP(float, _OverrideFrame)
					UNITY_DEFINE_INSTANCED_PROP(float, _CurrentAnimation)
					UNITY_DEFINE_INSTANCED_PROP(float, _EntityID)
				UNITY_INSTANCING_BUFFER_END(Props)

				v2f vert( appdata_base v, uint vid : SV_VertexID, uint iid: SV_INSTANCEID)
				{
					#include "GetPosNorm.cginc"
					v2f o;
					o.pos = pos;
					v.vertex = pos;
					v.normal = UnityObjectToWorldNormal(normal);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
					return o;
				}

				float4 frag( v2f i ) : SV_Target
				{
					SHADOW_CASTER_FRAGMENT(i)
				}
			ENDCG
		}
    }
}
