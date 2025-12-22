//
//  MelSpectrogramShader.metal
//  PsytranceVisualizer
//
//  Mel spectrogram with scrolling waterfall display
//

#include <metal_stdlib>
using namespace metal;

#include "Common.metal"

fragment float4 melSpectrogramFragment(
    VertexOut in [[stage_in]],
    constant ShaderUniforms& uniforms [[buffer(0)]],
    constant float* fftData [[buffer(1)]],
    constant float* melData [[buffer(2)]],
    constant float* historyData [[buffer(3)]]
) {
    float2 uv = in.uv;
    float time = uniforms.time;
    float reactivity = uniforms.reactivity;

    // Configuration
    const int numBands = 64;
    const int historyLength = 128;

    // Map UV to mel band and history position
    int bandIndex = int(uv.x * float(numBands));
    bandIndex = clamp(bandIndex, 0, numBands - 1);

    // Scrolling effect - newer data at bottom
    float scrollOffset = fract(time * 0.5); // Scroll speed
    float yPos = fract(uv.y + scrollOffset);

    // Get mel magnitude
    float magnitude = melData[bandIndex];
    magnitude = magnitude * (0.5 + reactivity * 1.5);
    magnitude = clamp(magnitude, 0.0, 1.0);

    // Create waterfall effect using history
    int historyIndex = int(yPos * float(historyLength));
    historyIndex = clamp(historyIndex, 0, historyLength - 1);

    // Combine current and historical data for waterfall
    float historicalValue = historyData[historyIndex];

    // Blend between current magnitude and position-based intensity
    float intensity = magnitude;

    // Add some variance based on band position
    float bandPhase = float(bandIndex) / float(numBands);
    intensity *= 0.8 + 0.2 * sin(bandPhase * 6.28318 + time);

    // Apply fade for older data (top of screen)
    float ageFade = 1.0 - uv.y * 0.3;
    intensity *= ageFade;

    // Generate color using heatmap
    float3 color = heatmap(intensity);

    // Add frequency-dependent hue shift
    float hueShift = bandPhase * 0.3;
    color = psytrancePalette(intensity + hueShift, time);

    // Modulate by actual intensity
    color *= 0.3 + intensity * 0.7;

    // Add grid lines for visual reference
    float gridX = abs(fract(uv.x * float(numBands)) - 0.5) * 2.0;
    float gridY = abs(fract(uv.y * 16.0) - 0.5) * 2.0;

    float gridLine = smoothstep(0.95, 1.0, gridX) + smoothstep(0.95, 1.0, gridY);
    gridLine *= 0.1;

    color += float3(gridLine) * uvViolet;

    // Add glow on high energy
    if (intensity > 0.7) {
        float glow = (intensity - 0.7) / 0.3;
        color = addGlow(color, glow * 0.5, neonCyan);
    }

    // Peak flash
    if (uniforms.isPeak > 0.5) {
        color += neonMagenta * uniforms.peakIntensity * 0.15;
    }

    // Sub-bass emphasis on lower bands
    if (bandIndex < 8) {
        color += uvViolet * uniforms.subBassEnergy * 0.3;
    }

    return float4(color, 1.0);
}
