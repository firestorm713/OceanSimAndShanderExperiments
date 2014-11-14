Shader "unityCookie/introduction/5 - Multiple Lighting"
{
	Properties{
		_Color ("Color", Color) = (1.0,1.0,1.0,1.0)
		_SpecColor ("Specular Color", Color) = (1.0,1.0,1.0,1.0)
		_Shininess ("Shininess", float) = 10.0
		_RimColor ("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_RimPower ("Rim Power", Range(0.1, 10)) = 3.0
	}
	SubShader{
		Pass{
			Tags{"LightMode" = "ForwardBase"}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			// User Defined Variables
			uniform float4 _Color;
			uniform float4 _SpecColor;
			uniform float4 _RimColor;
			uniform float _RimPower;
			uniform float _Shininess;

			// Unity Defined Variables
			uniform float4 _LightColor0;

			struct vertexInput{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct vertexOutput{
				float4 pos : SV_POSITION;
				float4 posWorld : TEXCOORD0;
				float3 normalDir : TEXCOORD1;
			};

			vertexOutput vert(vertexInput v)
			{
				vertexOutput o;

				o.posWorld = mul(_Object2World, v.vertex);
				o.normalDir = normalize(mul(float4(v.normal, 0.0), _World2Object).xyz);

				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);

				return o;
			}

			float4 frag(vertexOutput i) : COLOR
			{
				float3 normalDirection = i.normalDir;
				float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - mul(_Object2World, i.posWorld.xyz));
				float3 fragmentToLightSource = float3(_WorldSpaceLightPos0.xyz - float3(i.posWorld.xyz));
				float dist = length(fragmentToLightSource);
				float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, fragmentToLightSource,_WorldSpaceLightPos0.w));
				float atten = lerp(1.0, 1.0/dist, _WorldSpaceLightPos0.w);

				// Lighting
				float3 diffuseReflection = atten * _LightColor0.xyz * saturate(dot(normalDirection, lightDirection));
				float3 specularReflection = atten * _SpecColor * saturate(dot(normalDirection, lightDirection)) * pow(saturate( dot(reflect(-lightDirection, normalDirection), viewDirection)), _Shininess);
				
				float3 rim = 1 - saturate(dot(viewDirection, normalDirection));
				float3 rimLighting = saturate(dot(normalDirection, lightDirection)) * pow(rim, _RimPower);

				float3 lightFinal = rimLighting + diffuseReflection + specularReflection + UNITY_LIGHTMODEL_AMBIENT;

				return float4(lightFinal*_Color, 1.0);
			}

			ENDCG
		}
		Pass{
			Tags{"LightMode" = "ForwardAdd"}
			Blend One One
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			// User Defined Variables
			uniform float4 _Color;
			uniform float4 _SpecColor;
			uniform float4 _RimColor;
			uniform float _RimPower;
			uniform float _Shininess;

			// Unity Defined Variables
			uniform float4 _LightColor0;

			struct vertexInput{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct vertexOutput{
				float4 pos : SV_POSITION;
				float4 posWorld : TEXCOORD0;
				float3 normalDir : TEXCOORD1;
			};

			vertexOutput vert(vertexInput v)
			{
				vertexOutput o;

				o.posWorld = mul(_Object2World, v.vertex);
				o.normalDir = normalize(mul(float4(v.normal, 0.0), _World2Object).xyz);

				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);

				return o;
			}

			float4 frag(vertexOutput i) : COLOR
			{
				float3 normalDirection = i.normalDir;
				float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - mul(_Object2World, i.posWorld.xyz));

				float3 fragmentToLightSource = _WorldSpaceLightPos0.xyz - i.posWorld.xyz;
				float dist = length(fragmentToLightSource);
				float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, fragmentToLightSource,_WorldSpaceLightPos0.w));
				float atten = lerp(1.0, 1.0/dist, _WorldSpaceLightPos0.w);

				

				// Lighting
				float3 diffuseReflection = atten * _LightColor0.xyz * saturate(dot(normalDirection, lightDirection));
				float3 specularReflection = atten * _SpecColor * saturate(dot(normalDirection, lightDirection)) * pow(saturate( dot(reflect(-lightDirection, normalDirection), viewDirection)), _Shininess);
				
				float3 rim = 1 - saturate(dot(viewDirection, normalDirection));
				float3 rimLighting = saturate(dot(normalDirection, lightDirection)) * pow(rim, _RimPower);

				float3 lightFinal = rimLighting + diffuseReflection + specularReflection;

				return float4(lightFinal*_Color, 1.0);
			}

			ENDCG
		}
	}
}