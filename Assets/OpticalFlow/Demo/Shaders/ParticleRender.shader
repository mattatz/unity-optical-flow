Shader "OpticalFlow/Demo/ParticleRender"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "" {}
		_ShapeTex ("Shape", 2D) = "" {}

		_Color ("Color", Color) = (1, 1, 1, 1)
        _Size ("Size", Float) = 5.0

		_Position ("Position", 2D) = "" {}
		_Velocity ("Velocity", 2D) = "" {}
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }
        Cull Off
        ZWrite On
        ZTest Always
        Blend One One
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

			sampler2D _MainTex, _ShapeTex;
			float4 _Color;

            float _Size;

			sampler2D _Position, _Velocity;
			
			v2f vert (appdata IN)
			{
				v2f OUT;

                float4 pos = tex2Dlod(_Position, float4(IN.uv, 0, 0));
                float4 vertex = mul(unity_ObjectToWorld, float4(pos.xyz, 1));
                vertex = mul(UNITY_MATRIX_V, vertex);

                vertex.xy += (IN.vertex.xy) * _Size * saturate(vertex.w);

                OUT.vertex = mul(UNITY_MATRIX_P, vertex);
                OUT.color = _Color * tex2Dlod(_MainTex, float4(IN.uv, 0, 0)) * pos.w;
                OUT.uv = IN.uv2;
				return OUT;
			}
			
			float4 frag (v2f IN) : SV_Target {
                return tex2D(_ShapeTex, IN.uv) * IN.color;
            }

			ENDCG
		}
	}
}
