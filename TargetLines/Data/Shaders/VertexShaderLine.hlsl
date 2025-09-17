cbuffer LineConstantBuffer : register(b0)
{
    matrix ViewProjection;
    float4 LineColor;
    float4 OutlineColor;
    float3 StartPoint;
    float Thickness;
    float3 EndPoint;
    float OutlineThickness;
    float3 MiddlePoint;
    float UseQuadratic; // 1.0 for quadratic, 0.0 for cubic
    float RezoScale;
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
    float4 WorldPos : TEXCOORD1;
};

VSOutput Main(VSInput input)
{
    VSOutput output;
    output.Position = float4(input.Position, 1.0f);
    output.UV = input.UV;
    output.WorldPos = float4(input.Position, 1.0f);
    return output;
}
