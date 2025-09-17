cbuffer DebugConstantBuffer : register(b0)
{
    float RezoScale;
    float Near;
    float Far;
}

struct VSInput
{
    float3 Position : POSITION;
    float2 UV : TEXCOORD0;
};

struct VSOutput
{
    float4 Position : SV_Position;
    float2 UV : TEXCOORD0;
};

VSOutput Main(VSInput input)
{
    VSOutput output;
    output.Position = float4(input.Position, 1.0f);
    output.UV = input.UV;
    return output;
}
