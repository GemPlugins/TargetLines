struct PS_INPUT
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
};

float4 Main(PS_INPUT input) : SV_TARGET
{
    return input.Color;
}
