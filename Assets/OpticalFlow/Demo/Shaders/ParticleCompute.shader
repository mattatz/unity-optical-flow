Shader"OpticalFlow/Demo/ParticleCompute"
{

	Properties
	{
		_Position ("Position", 2D) = "" {}
		_Velocity ("Velocity", 2D) = "" {}
		_Flow ("Flow", 2D) = "black" {}

        _Noise ("Noise", Vector) = (0.5, 1.25, 1, 1)
        _Decay ("Decay", Range(0.75, 0.999)) = 0.9
	}

    CGINCLUDE

    #include "UnityCG.cginc"
    #include "Random.cginc"
    #include "./Noise/SimplexNoiseGrad3D.cginc"

    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct v2f
    {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
    };

    v2f vert (appdata v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        return o;
    }

    sampler2D _Position, _Velocity, _Flow;

    float4 _Noise;
    float _Decay;

    const float epsilon = 1e-12;

    ENDCG

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

        // 0 : Position Update
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragPositionUpdate
			
			float4 fragPositionUpdate (v2f i) : SV_Target
			{
                float2 uv = i.uv;
				float4 pos = tex2D(_Position, uv);
				float4 vel = tex2D(_Velocity, uv);
				float2 flow = tex2D(_Flow, uv).xy;

                float life = pos.w;
                float v = dot(flow.xy, flow.xy);

                float3 ip = float3(uv - 0.5, 0);
                float3 vp = pos.xyz + vel.xyz * unity_DeltaTime.x;

                float ini = step(life, epsilon) * step(0.01, v);
                pos.xyz = lerp(vp, ip, ini);
                pos.w = lerp(life - unity_DeltaTime.x, 1.0, ini);
				return pos;
			}
			ENDCG
		}

        // 1 : Velocity Update
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragVelocityUpdate
			
			float4 fragVelocityUpdate (v2f i) : SV_Target
			{
                float2 uv = i.uv;
				float4 pos = tex2D(_Position, uv);
				float4 vel = tex2D(_Velocity, uv);

                float2 flow = saturate(tex2D(_Flow, uv).xy);

                float3 iv = float3(0, 0, 0);

                float3 nv = float3(flow.xy, 0);
                nv += snoise_grad(float3(pos.xyz * _Noise.y + float3(0, _Time.x * _Noise.z, 0))) * _Noise.x;
                float3 vv = vel.xyz * _Decay + nv * vel.w * unity_DeltaTime.x;

                float ini = step(pos.w - unity_DeltaTime.x, epsilon);
                vel.xyz = lerp(vv, iv, ini);

				return vel;
			}
			ENDCG
		}

        // 2 : Position Init
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragPositionInit
			
			float4 fragPositionInit (v2f i) : SV_Target
			{
                float2 uv = i.uv;
				float4 pos = tex2D(_Position, uv);
                pos.xyz = float3(uv - 0.5, 0);
                pos.w = 0;
				return pos;
			}
			ENDCG
		}

        // 3 : Velocity Init
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragVelocityInit
			
			float4 fragVelocityInit (v2f i) : SV_Target
			{
                float2 uv = i.uv;
				float4 vel = tex2D(_Velocity, uv);
                vel.xyz = float3(0, 0, 0);
                vel.w = lerp(0.5, 1.0, saturate(nrand(uv)));
				return vel;
			}
			ENDCG
		}

	}
}
