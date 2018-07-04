Shader "Unlit/TextureAnimPlayer"
{
				
    Properties
    {
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
		_PosTex("Position Texture", 2D) = "black"{}
		_NmlTex("Normal Texture", 2D) = "white"{}
		_TotalAnimations("Total Animation", float) = 0
		_CurrentAnimation("Current Animation", float) = 0
		_AnimationFPS("Animation FPS", float) = 30
		[HideInInspector] _TotalFrames("Total Frames", float) = 0
		[HideInInspector] _AnimationFrameCount("Animation Frame Count", float) = 0
    }

    SubShader
    {
        Pass
        {
            Tags {"LightMode"="ForwardBase"}
            CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"
				#include "Lighting.cginc"
				#include "AutoLight.cginc"
				#pragma multi_compile_instancing
				#define ts _PosTex_TexelSize
				
				struct v2f
				{
					float2 uv : TEXCOORD0;
					SHADOW_COORDS(1) // put shadows data into TEXCOORD1
					float3 normal : TEXCOORD2;
					fixed3 diff : COLOR0;
					fixed3 ambient : COLOR1;
					float4 pos : SV_POSITION;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};
				sampler2D _MainTex, _PosTex, _NmlTex;
				float4 _PosTex_TexelSize, _Color;
				float _AnimationFPS;
				float _TotalFrames;
				float _AnimationFrameCount;
				float _CurrentAnimation;

				v2f vert (appdata_base v, uint vid : SV_VertexID)
				{
					float x = (vid + 0.5) * ts.x;
					float y = fmod(_Time.y * _AnimationFPS / _AnimationFrameCount, 1) * _AnimationFrameCount / _TotalFrames + (_AnimationFrameCount / _TotalFrames) * _CurrentAnimation;
					float4 pos = tex2Dlod(_PosTex, float4(x, y, 0, 0));
					float3 normal = tex2Dlod(_NmlTex, float4(x, y, 0, 0));
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
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
		
		// Pass to render object as a shadow caster
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 2.0
				#pragma multi_compile_shadowcaster
				#pragma multi_compile_instancing // allow instanced shadow pass for most of the shaders
				#include "UnityCG.cginc"
				#pragma multi_compile ___ ANIM_LOOP
				#define ts _PosTex_TexelSize

				struct v2f {
					V2F_SHADOW_CASTER;
					UNITY_VERTEX_OUTPUT_STEREO
                	UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				sampler2D _MainTex, _PosTex, _NmlTex;
				float4 _PosTex_TexelSize, _Color;
				float _AnimationFPS;
				float _TotalFrames;
				float _AnimationFrameCount;
				float _CurrentAnimation;

				v2f vert( appdata_base v, uint vid : SV_VertexID)
				{
					float x = (vid + 0.5) * ts.x;
					float y = fmod(_Time.y * _AnimationFPS / _AnimationFrameCount, 1) * _AnimationFrameCount / _TotalFrames + (_AnimationFrameCount / _TotalFrames) * _CurrentAnimation;
					float4 pos = tex2Dlod(_PosTex, float4(x, y, 0, 0));
					float3 normal = tex2Dlod(_NmlTex, float4(x, y, 0, 0));
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					v.vertex = pos;
					v.normal = UnityObjectToWorldNormal(normal);
					UNITY_SETUP_INSTANCE_ID(v);
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
