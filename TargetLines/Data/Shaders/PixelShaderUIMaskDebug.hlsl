cbuffer UIMaskConstantBuffer : register(b0)
{
    float RezoScale;
    bool UseRezoScale;
}

Texture2D BackBufferTexture : register(t0);
SamplerState BackBufferSampler : register(s0);

Texture2D BackBufferNoUITexture : register(t1);
SamplerState BackBufferNoUISampler : register(s1);

struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

float4 Main(PS_INPUT input) : SV_TARGET
{
    float4 backBufferColor = BackBufferTexture.Sample(BackBufferSampler, input.TexCoord);
    float4 backBufferNoUIColor = BackBufferNoUITexture.Sample(BackBufferNoUISampler, input.TexCoord);

    float vfxAlpha = backBufferNoUIColor.a;
    float combinedAlpha = backBufferColor.a;

    float uiAlpha = 0.0f;
    if (vfxAlpha < 1.0f) // avoid division by zero
    {
        uiAlpha = (combinedAlpha - vfxAlpha) / (1.0f - vfxAlpha);
    }

    return float4(1.0f, 0.0f, 0.0f, 1.0f - uiAlpha);
}
