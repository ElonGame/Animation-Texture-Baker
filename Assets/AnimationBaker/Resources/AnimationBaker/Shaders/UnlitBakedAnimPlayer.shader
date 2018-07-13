Shader "BakedAnimation/Unlit"
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
            CGPROGRAM
				#pragma target 4.5
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"
				#pragma multi_compile_instancing
				#define ts _PosTex_TexelSize
				RWStructuredBuffer<float4> _RandomWrite : register(u1);
				
				struct v2f
				{
					float2 uv : TEXCOORD0;
					float3 normal : TEXCOORD1;
					float4 pos : SV_POSITION;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};
				sampler2D _MainTex, _PosTex, _NmlTex;
				float4 _PosTex_TexelSize, _Color;
				float _PlaybackSpeed;
				float _TexHeight;
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
					return o;
				}
				fixed4 frag (v2f i) : SV_Target
				{
					fixed4 col = tex2D(_MainTex, i.uv) * _Color;
					return col;
				}
            ENDCG
        }
    }
}
