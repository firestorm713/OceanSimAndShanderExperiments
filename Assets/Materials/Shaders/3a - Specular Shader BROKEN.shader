Shader "unityCookie/introduction/3 - Specular Shading"
{
	Properties { 
		_Color ("Color", Color)=( 1.0, 1.0, 1.0, 1.0 ) 
		_SpecColor ("Specular Color", Color) = (1, 1, 1, 1)
		_Shininess ("Shininess", Float) = 10
	}
	SubShader {
		Pass {
			Tags{"LightMode" = "ForwardBase"}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			//user defined variable passed from above
			uniform float4 _Color;
			uniform float4 _SpecColor;
			uniform float4 _Shininess;

			//Unity Defined Variables
			uniform float4 _LightColor0;

			//Base Input Structs
			struct vertexInput {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct vertexOutput {
				float4 pos : SV_POSITION;
				float4 col : COLOR;
				//float4 posWorld : TEXCOORD0;
				//float3 normalDir : TEXCOORD1;
			};

			//vertex function
			vertexOutput vert(vertexInput v)
			{
				vertexOutput o;
				// per vertex lighting
				float3 normalDirection = normalize( mul(float4(v.normal, 0.0), _World2Object).xyz );
				float3 viewDirection = normalize(float3(float4(_WorldSpaceCameraPos.xyz, 1.0) - mul(_Object2World, v.vertex).xyz ));
				float3 lightDirection;
				float atten = 1.0;

				lightDirection = normalize(float3( _WorldSpaceLightPos0.xyz ));

				float3 diffuseReflection = atten * _LightColor0.xyz  * max(0.0, dot(normalDirection,  lightDirection));
				float3 specularReflection =  atten * _SpecColor.rgb * max(0.0, dot(normalDirection, lightDirection)) * pow( max( 0.0, dot(reflect(-lightDirection, normalDirection), viewDirection)), _Shininess);

				float3 lightFinal = diffuseReflection + specularReflection + UNITY_LIGHTMODEL_AMBIENT;

				o.col = float4(lightFinal * _Color.rgb, 1.0);
				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);

				// per pixel lighting
				//o.posWorld = mul(_Object2World, v.vertex);
				//o.normalDir = normalize(float3(mul(float4(v.normal, 0.0),_World2Object).xyz));
				//o.pos = mul(UNITY_MATRIX_MVP, v.vertex);

				return o;
			}

			//fragment function
			float4 frag(vertexOutput i) : COLOR
			{
				/*float3 normalDirection = normalize(i.normalDir);
				float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - float3(i.posWorld.xyz));
				float3 lightDirection;
				float atten = 1.0;

				float3 ambientLight = UNITY_LIGHTMODEL_AMBIENT.rgb;
				lightDirection = normalize(float3(_WorldSpaceLightPos0.xyz));

				float3 diffuseReflection = atten * float3(_LightColor0.rgb) * max(0.0, dot(normalDirection, lightDirection));
				float3 specularReflection = max(0.0, dot(normalDirection, lightDirection)) * atten * 
											float3(_LightColor0.rgb) * float3(_SpecColor.rgb) * 
											pow(max(0.0,dot(reflect(-lightDirection, normalDirection), viewDirection)), 
											_Shininess);
				float3 lightFinal = ambientLight + diffuseReflection + specularReflection;

				return float4(lightFinal * float3(_Color.rgb), 1.0);*/
				return i.col;
			}
			ENDCG
		}
	}
}