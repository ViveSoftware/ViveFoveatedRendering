//========= Copyright 2020, HTC Corporation. All rights reserved. ===========

Shader "Hidden/VisualizeViveFoveatedRendering"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_GazeData("GazeData", Vector) = (0, 0, 0, 0)
		_GazePointRadii("GazePointRadii", Vector) = (0, 0, 0, 0)
		_InnerRadii("InnerRadii", Vector) = (0, 0, 0, 0)
		_MiddleRadii("MiddleRadii", Vector) = (0, 0, 0, 0)
		_PeripheralRadii("PeripheralRadii", Vector) = (0, 0, 0, 0)
	}

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

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

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert(appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
			half4 _MainTex_ST;

			float4 _GazeData;
			float4 _GazePointRadii;
			float4 _InnerRadii;
			float4 _MiddleRadii;
			float4 _PeripheralRadii;

			bool InsideEllipse(float2 pt, float2 orgin, float2 radius)
			{
				float2 calc = pow((pt - orgin) / radius, 2);
				return (calc.x + calc.y) <= 1;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float2 pixelPos = i.uv;
				float2 center = _GazeData.xy;

#if UNITY_SINGLE_PASS_STEREO
				pixelPos = UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST);
				center = UnityStereoScreenSpaceUVAdjust(_GazeData.xy, _MainTex_ST);
				center.x += lerp(0.0125f, -0.0125f, step(0.5f, center.x));
#else
				//	For multi pass and single pass stereo instanced.
				center.x += lerp(0.025, -0.025, unity_StereoEyeIndex);
#endif 

				bool isGaze = InsideEllipse(pixelPos, center, _GazePointRadii.xy);
				bool isInner = InsideEllipse(pixelPos, center, _InnerRadii.xy);
				bool isMiddle = InsideEllipse(pixelPos, center, _MiddleRadii.xy);
				bool isPeripheral = InsideEllipse(pixelPos, center, _PeripheralRadii.xy);

				fixed4 blendColor = 0.0f;
				if (isGaze)
				{
					return fixed4(1.0f, 0.0f, 0.0f, 0.0f);
				}
				else if (isInner)
				{
					blendColor = fixed4(0, 0.635f, 0.91f, 0.0f);
				}
				else if (isMiddle)
				{
					blendColor = fixed4(0.25f, 0.76f, 0.0f, 0.0f);
				}
				else if (isPeripheral)
				{
					blendColor = fixed4(1.0f, 0.788f, 0.055f, 0.0f);
				} 
				else
				{
					return 0.0f;
				}

				return lerp(UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, pixelPos), blendColor, 0.3f);
			}
			ENDCG
		}
	}
}
