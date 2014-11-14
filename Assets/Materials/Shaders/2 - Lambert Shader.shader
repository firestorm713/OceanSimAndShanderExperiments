Shader "unityCookie/introduction/2 - Lambert Shading"
{
	Properties { _Color ("Color", Color)=( 1.0, 1.0, 1.0, 1.0 ) }
	SubShader {
		Pass {
			Tags{"LightMode" = "ForwardBase"}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			//user defined variable passed from above
			uniform float4 _Color;
			uniform float4 _LightColor0;

			//Base Input Structs
			struct vertexInput {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct vertexOutput {
				float4 pos : SV_POSITION;
				float4 col : COLOR;
			};

			//vertex function
			vertexOutput vert(vertexInput v)
			{
				vertexOutput o;
				
				float3 normalDirection = normalize(float3(mul(float4(v.normal, 0.0), _World2Object).rgb ));
				float3 lightDirection;
				float atten = 1.0;

				float3 ambientLight = UNITY_LIGHTMODEL_AMBIENT.rgb;
				lightDirection = normalize(float3( _WorldSpaceLightPos0.rgb ));

				float3 diffuseReflection = atten * float3(_LightColor0.rgb ) * float3(_Color.rgb )  * max(0.0, dot(normalDirection, lightDirection));

				float3 lightFinal = ambientLight + diffuseReflection;

				o.col = float4(lightFinal * float3(_Color.rgb), 1.0);
				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				return o;
			}

			//fragment function
			float4 frag(vertexOutput i) : COLOR
			{
				return i.col;
			}
			ENDCG
		}
	}
}