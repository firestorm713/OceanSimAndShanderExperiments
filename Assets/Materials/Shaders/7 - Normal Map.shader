Shader "unityCookie/introduction/7 - Normal Map"
{
	Properties{
		_Color ("Color Tint", Color) = (1.0,1.0,1.0,1.0)
		_MainTex ("Diffuse Texture", 2D) = "white"{}
		_BumpMap ("Normal Texture", 2D) = "white"{}
		_BumpDepth ("Bump Depth", Range(-2.0, 2.0)) = 1
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
			uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform sampler2D _BumpMap;
			uniform float4 _BumpMap_ST;
			uniform float4 _Color;
			uniform float4 _SpecColor;
			uniform float4 _RimColor;
			uniform float _RimPower;
			uniform float _Shininess;
			uniform float _BumpDepth;

			// Unity Defined Variables
			uniform float4 _LightColor0;

			struct vertexInput{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 texcoord : TEXCOORD0;
				float4 tangent : TANGENT;
			};

			struct vertexOutput{
				float4 pos : SV_POSITION;
				float4 tex : TEXCOORD0;
				float4 posWorld : TEXCOORD1;
				float3 normalWorld : TEXCOORD2;
				float3 tangentWorld : TEXCOORD3;
				float3 binormalWorld : TEXCOORD4;
			};

			vertexOutput vert(vertexInput v)
			{
				vertexOutput o;

				o.normalWorld = normalize(mul(float4(v.normal, 0.0), _World2Object).xyz);
				o.tangentWorld = normalize(mul (_Object2World, v.tangent).xyz);
				o.binormalWorld = normalize(cross(o.normalWorld, o.tangentWorld) * v.tangent.w);

				o.posWorld = mul(_Object2World, v.vertex);

				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				o.tex = v.texcoord;

				return o;
			}

			float4 frag(vertexOutput i) : COLOR
			{

				float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - mul(_Object2World, i.posWorld.xyz));
				float3 fragmentToLightSource = float3(_WorldSpaceLightPos0.xyz - float3(i.posWorld.xyz));
				float dist = length(fragmentToLightSource);
				float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, fragmentToLightSource,_WorldSpaceLightPos0.w));
				float atten = lerp(1.0, 1.0/dist, _WorldSpaceLightPos0.w);

				// Texture stuff
				float4 tex = tex2D(_MainTex, i.tex.xy * _MainTex_ST.xy + _MainTex_ST.zw);
				float4 texN = tex2D(_BumpMap, i.tex.xy * _BumpMap_ST.xy + _BumpMap_ST.zw);

				//unpack normal function
				float3 localCoords = float3(2.0 * texN.ag - float2(1.0, 1.0), 0.0);
				localCoords.z = _BumpDepth; //1.0 - 0.5 * dot(localCoords, localCoords);

				// normal transpose matrix
				float3x3 local2WorldTranspose = float3x3(
					i.tangentWorld,
					i.binormalWorld,
					i.normalWorld
				);

				float3 normalDirection = normalize(mul(localCoords, local2WorldTranspose));

				// Lighting
				float3 diffuseReflection = atten * _LightColor0.xyz * saturate(dot(normalDirection, lightDirection));
				float3 specularReflection = diffuseReflection * _SpecColor * pow(saturate( dot(reflect(-lightDirection, normalDirection), viewDirection)), _Shininess);
				
				float rim = 1 - saturate(dot(viewDirection, normalDirection));
				float3 rimLighting = saturate(dot(normalDirection, lightDirection)) * pow(rim, _RimPower);

				float3 lightFinal = rimLighting + diffuseReflection + specularReflection + UNITY_LIGHTMODEL_AMBIENT;

				return float4(tex.xyz * lightFinal*_Color, 1.0);
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
			uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform sampler2D _BumpMap;
			uniform float4 _BumpMap_ST;
			uniform float4 _Color;
			uniform float4 _SpecColor;
			uniform float4 _RimColor;
			uniform float _RimPower;
			uniform float _Shininess;
			uniform float _BumpDepth;

			// Unity Defined Variables
			uniform float4 _LightColor0;

			struct vertexInput{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 texcoord : TEXCOORD0;
				float4 tangent : TANGENT;
			};

			struct vertexOutput{
				float4 pos : SV_POSITION;
				float4 tex : TEXCOORD0;
				float4 posWorld : TEXCOORD1;
				float3 normalWorld : TEXCOORD2;
				float3 tangentWorld : TEXCOORD3;
				float3 binormalWorld : TEXCOORD4;
			};

			vertexOutput vert(vertexInput v)
			{
				vertexOutput o;

				o.normalWorld = normalize(mul(float4(v.normal, 0.0), _World2Object).xyz);
				o.tangentWorld = normalize(mul (_Object2World, v.tangent).xyz);
				o.binormalWorld = normalize(cross(o.normalWorld, o.tangentWorld) * v.tangent.w);

				o.posWorld = mul(_Object2World, v.vertex);

				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				o.tex = v.texcoord;

				return o;
			}

			float4 frag(vertexOutput i) : COLOR
			{

				float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - mul(_Object2World, i.posWorld.xyz));
				float3 fragmentToLightSource = float3(_WorldSpaceLightPos0.xyz - float3(i.posWorld.xyz));
				float dist = length(fragmentToLightSource);
				float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, fragmentToLightSource,_WorldSpaceLightPos0.w));
				float atten = lerp(1.0, 1.0/dist, _WorldSpaceLightPos0.w);

				// Texture stuff
				//float4 tex = tex2D(_MainTex, i.tex.xy * _MainTex_ST.xy + _MainTex_ST.zw);
				float4 texN = tex2D(_BumpMap, i.tex.xy * _BumpMap_ST.xy + _BumpMap_ST.zw);

				//unpack normal function
				float3 localCoords = float3(2.0 * texN.ag - float2(1.0, 1.0), 0.0);
				localCoords.z = _BumpDepth; //1.0 - 0.5 * dot(localCoords, localCoords);

				// normal transpose matrix
				float3x3 local2WorldTranspose = float3x3(
					i.tangentWorld,
					i.binormalWorld,
					i.normalWorld
				);

				float3 normalDirection = normalize(mul(localCoords, local2WorldTranspose));

				// Lighting
				float3 diffuseReflection = atten * _LightColor0.xyz * saturate(dot(normalDirection, lightDirection));
				float3 specularReflection = diffuseReflection * _SpecColor * pow(saturate( dot(reflect(-lightDirection, normalDirection), viewDirection)), _Shininess);
				
				float rim = 1 - saturate(dot(viewDirection, normalDirection));
				float3 rimLighting = saturate(dot(normalDirection, lightDirection)) * pow(rim, _RimPower);

				float3 lightFinal = rimLighting + diffuseReflection + specularReflection; // + UNITY_LIGHTMODEL_AMBIENT;

				return float4(lightFinal*_Color, 1.0);
			}

			ENDCG
		}
	}
}