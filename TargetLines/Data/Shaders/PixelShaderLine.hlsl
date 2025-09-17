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

// Depth texture
Texture2D<float> DepthTexture : register(t0);
SamplerState DepthSampler : register(s0);

// BackBuffer texture
Texture2D BackBufferTexture : register(t1);
SamplerState BackBufferSampler : register(s1);

struct PSInput
{
    float4 Position : SV_Position;
    float2 UV : TEXCOORD0;
    float4 WorldPos : TEXCOORD1;
};

float3 EvaluateQuadraticBezier(float3 p0, float3 p1, float3 p2, float t)
{
    float mt = 1.0f - t;
    float mt2 = mt * mt;
    float t2 = t * t;

    return mt2 * p0 + 2.0f * mt * t * p1 + t2 * p2;
}

float3 EvaluateCubicBezier(float3 p0, float3 p1, float3 p2, float3 p3, float t)
{
    float mt = 1.0f - t;
    float mt2 = mt * mt;
    float mt3 = mt2 * mt;
    float t2 = t * t;
    float t3 = t2 * t;

    return mt3 * p0 + 3.0f * mt2 * t * p1 + 3.0f * mt * t2 * p2 + t3 * p3;
}

float3 EvaluateCurve(float3 start, float3 middle, float3 end, float t, float useQuadratic)
{
    if (useQuadratic > 0.5f)
    {
        return EvaluateQuadraticBezier(start, middle, end, t);
    }
    else
    {
        return EvaluateCubicBezier(start, middle, middle, end, t);
    }
}

float PointToLineDistance2D(float2 pt, float2 lineStart, float2 lineEnd)
{
    float2 lineDir = lineEnd - lineStart;
    float lineLength = length(lineDir);

    if (lineLength < 0.0001f) {
        return length(pt - lineStart);
    }

    lineDir = lineDir / lineLength;

    float2 pointToStart = pt - lineStart;
    float projectionLength = dot(pointToStart, lineDir);

    projectionLength = clamp(projectionLength, 0.0f, lineLength);

    float2 closestPoint = lineStart + lineDir * projectionLength;
    return length(pt - closestPoint);
}

// Returns both distance and world position of closest point
float2 CurvedLineSDF(float2 screenPos, out float3 closestWorldPos, out float3 worldNormal, float maxThickness)
{
    float minDistance = 1000.0f;
    float closestT = 0.0f;
    float closestDepth = -1000.0f; // Track the nearest depth (highest value for inverse-Z)
    closestWorldPos = float3(0, 0, 0);
    worldNormal = float3(0, 1, 0);
    const int sampleCount = 31;

    // First pass: find all segments within threshold distance
    for (int index = 0; index < sampleCount; index++)
    {
        float t1 = (float)index / (float)sampleCount;
        float t2 = (float)(index + 1) / (float)sampleCount;

        float3 worldPos1 = EvaluateCurve(StartPoint, MiddlePoint, EndPoint, t1, UseQuadratic);
        float3 worldPos2 = EvaluateCurve(StartPoint, MiddlePoint, EndPoint, t2, UseQuadratic);
        float4 clipPos1 = mul(float4(worldPos1, 1.0f), ViewProjection);
        float4 clipPos2 = mul(float4(worldPos2, 1.0f), ViewProjection);
        if (clipPos1.w <= 0.0f || clipPos2.w <= 0.0f)
        {
            continue;
        }

        float2 ndc1 = (clipPos1.xy / clipPos1.w);
        float2 ndc2 = (clipPos2.xy / clipPos2.w);
        if (abs(ndc1.x) > 1.5f || abs(ndc1.y) > 1.5f || abs(ndc2.x) > 1.5f || abs(ndc2.y) > 1.5f)
        {
            continue;
        }

        float segmentDistance = PointToLineDistance2D(screenPos, ndc1, ndc2);

        // Calculate interpolation along segment
        float segmentT = length(screenPos - ndc1) / length(ndc2 - ndc1);
        segmentT = clamp(segmentT, 0.0f, 1.0f);
        float currentT = lerp(t1, t2, segmentT);
        float3 currentWorldPos = EvaluateCurve(StartPoint, MiddlePoint, EndPoint, currentT, UseQuadratic);
        float4 currentClipPos = mul(float4(currentWorldPos, 1.0f), ViewProjection);

        if (currentClipPos.w > 0.0f)
        {
            float currentDepth = currentClipPos.z / currentClipPos.w;

            // For segments at similar screen distance, choose the nearest in depth
            // This handles self-occlusion when the line overlaps itself
            if (segmentDistance < maxThickness)
            {
                // Among all segments within the line thickness, pick the nearest
                if (currentDepth > closestDepth || (segmentDistance < minDistance && abs(currentDepth - closestDepth) < 0.001f))
                {
                    // This segment is the nearest at this screen position
                    minDistance = segmentDistance;
                    closestT = currentT;
                    closestWorldPos = currentWorldPos;
                    closestDepth = currentDepth;

                    float3 tangent = normalize(worldPos2 - worldPos1);
                    float3 up = float3(0, 1, 0);
                    worldNormal = normalize(cross(tangent, up));
                }
            }
        }
    }

    return float2(minDistance, closestT);
}

float2 ScreenToDepthUV(float2 screenPos)
{
    float2 uv = screenPos * 0.5f + 0.5f;
    return uv;
}

float4 Main(PSInput input) : SV_Target
{
    float2 screenCoord = input.WorldPos.xy;
    float2 screenUV = input.UV;

    float thickness = Thickness * 0.001f;
    float outlineThickness = OutlineThickness * 0.001f;
    float maxDistance = (thickness + outlineThickness) * 0.5f;

    float3 closestWorldPos;
    float3 worldNormal;
    float2 sdfResult = CurvedLineSDF(screenCoord, closestWorldPos, worldNormal, maxDistance);
    float distance = sdfResult.x;
    float lineT = sdfResult.y;

    float coreAlpha = distance < thickness * 0.5f ? 1.0f : 0.0f;
    float outlineAlpha = distance < maxDistance ? 1.0f : 0.0f;

    if (outlineAlpha == 0.0f)
    {
        discard;
        return float4(0.0f, 0.0f, 0.0f, 0.0f);
    }

    float4 coreColor = LineColor;
    float4 outlineColor = OutlineColor;

    // Depth culling
    // The depth texture is viewport-sized, but only contains valid data in the region from rezo scale
    float4 lineClipPos = mul(float4(closestWorldPos, 1.0f), ViewProjection);
    float lineDepth = lineClipPos.z / lineClipPos.w;
    float2 scaledDepthUV = input.UV * RezoScale;
    if (!(scaledDepthUV.x < 0.0f || scaledDepthUV.x > RezoScale || scaledDepthUV.y < 0.0f || scaledDepthUV.y > RezoScale)) // Check if we're within the valid depth data region
    {
        float sampledSceneDepth = DepthTexture.Sample(DepthSampler, scaledDepthUV);
        const float depthBias = 0.0001f;
        if (lineDepth < (sampledSceneDepth - depthBias))
        {
            discard;
            return float4(0.0f, 0.0f, 0.0f, 0.0f);
        }
    }

    float4 finalColor;

    if (distance < thickness * 0.5f)
    {
        finalColor = coreColor;
        finalColor.a *= coreAlpha;
    }
    else if (distance < (thickness + outlineThickness) * 0.5f)
    {
        finalColor = outlineColor;
        finalColor.a *= (outlineAlpha - coreAlpha);
    }
    else
    {
        discard;
        return float4(0.0f, 0.0f, 0.0f, 0.0f);
    }

    // make line appear beneath transparent UI elements, and clipped by opaque UI elements
    float4 backBufferColor = BackBufferTexture.Sample(BackBufferSampler, screenUV);
    float uiOcclusion = saturate(1.0f - (backBufferColor.a * 1.25f));
    finalColor.a *= uiOcclusion;

    return finalColor;
}
