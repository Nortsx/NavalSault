#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_3
	#define PS_SHADERMODEL ps_4_0_level_9_3
#endif

float KSpecular;
float Shininess;
float3 LightPosition;
float3 LightColor;
float3 CameraPosition;

float MoveFactor;
float2 Tiling;
float WaveStrength;
float FresnelPower;

float4x4 ReflectionView;
float4x4 Projection;
float4x4 WorldViewProjection;
float4x4 World;

texture RefractionTexture;
sampler2D refractionSampler = sampler_state
{
    Texture = <RefractionTexture>;
    AddressU = Clamp;
    AddressV = Clamp;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
};

texture ReflectionTexture;
sampler2D reflectionSampler = sampler_state
{
    Texture = <ReflectionTexture>;
    AddressU = Clamp;
    AddressV = Clamp;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
};

texture DistortionMap;
sampler2D distortionSampler = sampler_state
{
    Texture = <DistortionMap>;
    AddressU = Wrap;
    AddressV = Wrap;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
};

texture NormalMap;
sampler2D normalMapSampler = sampler_state
{
    Texture = <NormalMap>;
    AddressU = Wrap;
    AddressV = Wrap;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
};

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float2 TextureCoordinates : TEXCOORD0;
    float4 WorldPosition : TEXCOORD1;
    float3 Normal : TEXCOORD2;
    float4 ReflectionPosition : TEXCOORD3;
    float4 RefractionPosition : TEXCOORD4;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;

    output.Position = mul(input.Position, WorldViewProjection);
    output.WorldPosition = mul(input.Position, World);
    output.Normal = mul(input.Normal, (float3x3)World);
    output.TextureCoordinates = input.TextureCoordinates * Tiling;

    float4x4 reflectProjectWorld = mul(World, mul(ReflectionView, Projection));
    output.ReflectionPosition = mul(input.Position, reflectProjectWorld);
    output.RefractionPosition = output.Position;

    return output;
}

float2 ProjectToTexCoord(float4 clipPosition)
{
    float2 ndc = clipPosition.xy / clipPosition.w;
    return float2(0.5 * ndc.x + 0.5, -0.5 * ndc.y + 0.5);
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float2 refractionTexCoord = ProjectToTexCoord(input.RefractionPosition);
    float2 reflectionTexCoord = ProjectToTexCoord(input.ReflectionPosition);

    float2 distortedTexCoords = tex2D(distortionSampler, float2(input.TextureCoordinates.x + MoveFactor, input.TextureCoordinates.y)).rg * 0.01;
    distortedTexCoords = input.TextureCoordinates + float2(distortedTexCoords.x, distortedTexCoords.y + MoveFactor);
    float2 totalDistortion = (tex2D(distortionSampler, distortedTexCoords).rg * 2.0 - 1.0) * WaveStrength;

    // Clamp keeps distorted lookups from smearing the render-target edge texels.
    reflectionTexCoord = clamp(reflectionTexCoord + totalDistortion, 0.001, 0.999);
    refractionTexCoord = clamp(refractionTexCoord + totalDistortion, 0.001, 0.999);

    float4 reflectionColor = tex2D(reflectionSampler, reflectionTexCoord);
    float4 refractionColor = tex2D(refractionSampler, refractionTexCoord);

    float3 viewDirection = normalize(CameraPosition - input.WorldPosition.xyz);
    float refractiveFactor = pow(saturate(dot(viewDirection, normalize(input.Normal))), FresnelPower);

    float4 normalMapColor = tex2D(normalMapSampler, distortedTexCoords);
    float3 normal = normalize(float3(normalMapColor.r * 2.0 - 1.0, normalMapColor.b, normalMapColor.g * 2.0 - 1.0));

    float3 lightDirection = normalize(LightPosition - input.WorldPosition.xyz);
    float3 halfVector = normalize(lightDirection + viewDirection);
    float NdotL = saturate(dot(normal, lightDirection));
    float NdotH = saturate(dot(normal, halfVector));
    float3 specularLight = sign(NdotL) * KSpecular * LightColor * pow(NdotH, Shininess);

    float4 finalColor = lerp(reflectionColor, refractionColor, refractiveFactor) + float4(specularLight, 0.0);
    finalColor.a = 1.0;
    return finalColor;
}

technique Water
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
