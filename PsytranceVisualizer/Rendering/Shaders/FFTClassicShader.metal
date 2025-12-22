//
//  FFTClassicShader.metal
//  PsytranceVisualizer
//
//  Classic FFT bar visualization with glow effects
//

#include <metal_stdlib>
using namespace metal;

// Include common definitions
#include "Common.metal"

fragment float4 fftClassicFragment(
    VertexOut in [[stage_in]],
    constant ShaderUniforms& uniforms [[buffer(0)]],
    constant float* fftData [[buffer(1)]]
) {
    float2 uv = in.uv;
    float2 resolution = uniforms.resolution;
    float time = uniforms.time;
    float reactivity = uniforms.reactivity;

    // Number of bars to display
    const int numBars = 64;
    const float barWidth = 1.0 / float(numBars);
    const float barGap = barWidth * 0.2;
    const float actualBarWidth = barWidth - barGap;

    // Determine which bar this pixel belongs to
    int barIndex = int(uv.x * float(numBars));
    barIndex = clamp(barIndex, 0, numBars - 1);

    // Get FFT magnitude for this bar (with some averaging for smoothness)
    float magnitude = fftData[barIndex];

    // Apply reactivity scaling
    magnitude = magnitude * (0.5 + reactivity * 1.5);
    magnitude = clamp(magnitude, 0.0, 1.0);

    // Calculate bar position within its cell
    float barCellX = fract(uv.x * float(numBars));
    float barCenterX = 0.5;

    // Distance from bar center (for width calculation)
    float distFromCenter = abs(barCellX - barCenterX);
    float halfWidth = actualBarWidth * 0.5 / barWidth;

    // Check if we're inside the bar horizontally
    bool insideBarX = distFromCenter < halfWidth;

    // Bar height from bottom
    float barHeight = magnitude;

    // Add some bounce on peaks
    if (uniforms.isPeak > 0.5) {
        barHeight += uniforms.peakIntensity * 0.1 * sin(time * 20.0 + float(barIndex) * 0.3);
    }

    // Check if we're inside the bar vertically (from bottom)
    float yFromBottom = 1.0 - uv.y;
    bool insideBarY = yFromBottom < barHeight;

    // Color based on frequency and magnitude
    float colorPhase = float(barIndex) / float(numBars) + time * 0.05;
    float3 barColor = psytrancePalette(colorPhase, time);

    // Intensity gradient from bottom to top
    float intensityGradient = yFromBottom / max(barHeight, 0.01);
    intensityGradient = clamp(intensityGradient, 0.0, 1.0);

    // Make top of bars brighter
    barColor = mix(barColor * 0.6, barColor * 1.5, intensityGradient);

    // Calculate glow
    float glowRadius = 0.05 * (1.0 + magnitude);
    float distToBar = 0.0;

    if (!insideBarX) {
        distToBar = (distFromCenter - halfWidth) * barWidth;
    }
    if (!insideBarY && yFromBottom >= barHeight) {
        float vertDist = yFromBottom - barHeight;
        distToBar = max(distToBar, vertDist);
    }

    float glow = exp(-distToBar * distToBar / (glowRadius * glowRadius * 2.0));
    glow *= magnitude;

    // Final color
    float3 finalColor = float3(0.0);

    if (insideBarX && insideBarY) {
        // Inside the bar
        finalColor = barColor;

        // Add peak cap (bright line at top)
        float capThickness = 0.01;
        if (abs(yFromBottom - barHeight) < capThickness) {
            finalColor = float3(1.0); // White cap
        }
    } else {
        // Add glow outside bars
        finalColor = barColor * glow * 0.5;
    }

    // Add subtle background pulse with sub-bass
    float bgPulse = uniforms.subBassEnergy * 0.05;
    finalColor += deepPurple * bgPulse;

    // Add overall glow at peaks
    if (uniforms.isPeak > 0.5) {
        finalColor += neonMagenta * uniforms.peakIntensity * 0.1;
    }

    return float4(finalColor, 1.0);
}
