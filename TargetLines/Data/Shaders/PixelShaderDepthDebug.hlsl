cbuffer DebugConstantBuffer : register(b0)
{
    float RezoScale;
    float Near;
    float Far;
}

Texture2D<float> DepthTexture : register(t0);
SamplerState DepthSampler : register(s0);

struct PSInput
{
    float4 Position : SV_Position;
    float2 UV : TEXCOORD0;
};

float LinearizeDepth(float depth, float near, float far)
{
    return near / (depth + (near / far) * (1.0f - depth));
}

float4 Main(PSInput input) : SV_Target
{
    float2 depthUV = input.UV * RezoScale;
    float depth = DepthTexture.Sample(DepthSampler, depthUV);
    float linearDepth = LinearizeDepth(depth, Near, Far);
    float normalizedDepth = (linearDepth - Near) / (Far - Near);
    float visualDepth = saturate(normalizedDepth);
    return float4(visualDepth, visualDepth, visualDepth, 1.0f);
}
