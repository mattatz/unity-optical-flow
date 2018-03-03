Shader"OpticalFlow"
{

	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _Scale ("Scale", Range(1.0, 3.0)) = 1.0
        _Lambda ("Lambda", Range(0.0, 0.1)) = 0.01
        _Threshold ("Threshold", Range(0.000001, 0.1)) = 0.01
	}

    CGINCLUDE

    #include "UnityCG.cginc"

    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    const static float WEIGHTS[8] = { 0.013, 0.067, 0.194, 0.226, 0.226, 0.194, 0.067, 0.013 };
    const static float OFFSETS[8] = { -6.264, -4.329, -2.403, -0.649, 0.649, 2.403, 4.329, 6.264 };

    struct vsin
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct vs2psDown
    {
        float4 vertex : POSITION;
        float2 uv[4] : TEXCOORD0;
    };

    struct vs2psBlur
    {
        float4 vertex : POSITION;
        float2 uv[8] : TEXCOORD0;
    };

    vsin vert (appdata v)
    {
        vsin o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        return o;
    }

    struct v2f
    {
        float4 pos : SV_POSITION;
        float2 uv[2] : TEXCOORD0;
    };

    struct v2f_mt
    {
        float4 pos : SV_POSITION;
        float2 uv[5] : TEXCOORD0;
    };

    sampler2D _PrevTex;
    sampler2D _MainTex;
    float4 _MainTex_TexelSize;

    sampler2D _BlurTex;
    float4 _BlurTex_TexelSize;

    float _Scale, _Lambda, _Threshold;

    float4 grayScale(float4 col)
    {
        float gray = dot(float3(col.x, col.y, col.z), float3(0.3, 0.59, 0.11));
        return float4(gray, gray, gray, 1);
    }

    float4 gradient(sampler2D tex, float2 uv, float2 offset)
    {
        // return grayScale(tex2D(tex, uv + offset)) - grayScale(tex2D(tex, uv - offset));
        return (tex2D(tex, uv + offset)) - (tex2D(tex, uv - offset));
    }

    vs2psDown vertDownsample(vsin IN)
    {
        vs2psDown OUT;
        OUT.vertex = UnityObjectToClipPos(IN.vertex);
        OUT.uv[0] = IN.uv;
        OUT.uv[1] = IN.uv + float2(-0.5, -0.5) * _MainTex_TexelSize.xy;
        OUT.uv[2] = IN.uv + float2(0.5, -0.5) * _MainTex_TexelSize.xy;
        OUT.uv[3] = IN.uv + float2(-0.5, 0.5) * _MainTex_TexelSize.xy;
        return OUT;
    }

    vs2psBlur vertBlurH(vsin IN)
    {
        vs2psBlur OUT;
        OUT.vertex = UnityObjectToClipPos(IN.vertex);
        for (uint i = 0; i < 8; i++)
        {
            OUT.uv[i] = IN.uv + float2(OFFSETS[i], 0) * _MainTex_TexelSize.xy;
        }
        return OUT;
    }

    vs2psBlur vertBlurV(vsin IN)
    {
        vs2psBlur OUT;
        OUT.vertex = UnityObjectToClipPos(IN.vertex);
        for (uint i = 0; i < 8; i++)
        {
            OUT.uv[i] = IN.uv + float2(0, OFFSETS[i]) * _MainTex_TexelSize.xy;
        }
        return OUT;
    }

    float4 fragBlur(vs2psBlur IN) : COLOR
    {
        float4 c = 0;
        for (uint i = 0; i < 8; i++)
        {
            float4 col = tex2D(_MainTex, IN.uv[i]);
            c += col * WEIGHTS[i];
        }
        return c;
    }

    ENDCG

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

        // 0 : Calculate flow velocity
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragFlow
                               
            float4 fragFlow (vsin i) : SV_Target
			{
                float2 uv = i.uv;
				float4 current = tex2D(_MainTex, uv);
				float4 prev = tex2D(_PrevTex, uv);

                float2 dx = float2(_MainTex_TexelSize.x, 0);
                float2 dy = float2(0, _MainTex_TexelSize.y);

                float4 diff = current - prev;

                float4 gx = gradient(_PrevTex, uv, dx) + gradient(_MainTex, uv, dx);
                float4 gy = gradient(_PrevTex, uv, dy) + gradient(_MainTex, uv, dy);

                float4 gmag = sqrt(gx * gx + gy * gy + float4(_Lambda, _Lambda, _Lambda, _Lambda));
                float4 invGmag = 1.0 / gmag;
                float4 vx = diff * (gx * invGmag);
                float4 vy = diff * (gy * invGmag);

                float2 flow = float2(0, 0);
                const float inv3 = 0.33333;
                flow.x = -(vx.x + vx.y + vx.z) * inv3;
                flow.y = -(vy.x + vy.y + vy.z) * inv3;

                float w = length(flow);
                float nw = (w - _Threshold) / (1.0 - _Threshold);
                flow = lerp(float2(0, 0), normalize(flow) * nw * _Scale, step(_Threshold, w));
                return float4(flow, 0, 1);
            }

			ENDCG
		}

        // 1 : Downsample
		Pass
		{
			CGPROGRAM
			#pragma vertex vertDownsample
			#pragma fragment fragDownsample
			
            float4 fragDownsample(vs2psDown IN) : COLOR
            {
                float4 c = 0;
                for (uint i = 0; i < 4; i++)
                {
                    c += tex2D(_MainTex, IN.uv[i]) * 0.25;
                }
                return c;
            }

			ENDCG
		}

        // 2 : Horizontal Separable Gaussian
		Pass {
            CGPROGRAM
			#pragma vertex vertBlurH
			#pragma fragment fragBlur
			ENDCG
		}

		// 3 : Vertical Separable Gaussian
		Pass {
            CGPROGRAM
			#pragma vertex vertBlurV
			#pragma fragment fragBlur
			ENDCG
		}

		// 4 : Visualize
		Pass {
            CGPROGRAM
			#pragma vertex vert
			#pragma fragment fragVisualize

            float _Ratio;

            float4 fragVisualize (vsin i) : SV_Target
            {
                float2 uv = i.uv;
                uv.y *= _Ratio;
				float4 velocity = tex2D(_MainTex, uv);
                return float4(abs(velocity.xy), 0, 1);
            }

			ENDCG
		}

	}
}
