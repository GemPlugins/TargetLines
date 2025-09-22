Texture2D BackBufferTexture : register(t0);
SamplerState BackBufferSampler : register(s0);

struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

float4 Main(PS_INPUT input) : SV_TARGET
{
    float4 backBufferColor = BackBufferTexture.Sample(BackBufferSampler, input.TexCoord);
    return float4(1.0f, 0.0f, 0.0f, backBufferColor.a);
}
