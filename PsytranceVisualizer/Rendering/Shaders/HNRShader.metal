//
//  HNRShader.metal
//  PsytranceVisualizer
//
//  Harmonic-to-Noise ratio visualization with geometric shapes vs chaos
//

#include <metal_stdlib>
using namespace metal;

#include "Common.metal"

fragment float4 hnrFragment(
    VertexOut in [[stage_in]],
    constant ShaderUniforms& uniforms [[buffer(0)]],
    constant float* fftData [[buffer(1)]],
    constant float* melData [[buffer(2)]],
    constant float* historyData [[buffer(3)]]
) {
    float2 uv = in.uv;
    float2 resolution = uniforms.resolution;
    float time = uniforms.time;
    float reactivity = uniforms.reactivity;
    float hnr = uniforms.hnrRatio;
    float subBass = uniforms.subBassEnergy;

    // Center coordinates
    float2 center = float2(0.5, 0.5);
    float aspectRatio = resolution.x / resolution.y;

    float2 p = uv - center;
    p.x *= aspectRatio;

    float dist = length(p);
    float angle = atan2(p.y, p.x);

    // === HARMONIC SIDE (High HNR = Clear geometric shapes) ===

    // Rotating hexagon
    float2 rotP = rotate(p, time * 0.5);
    float hexDist = sdHexagon(rotP, 0.2 + subBass * 0.1);
    float hexEdge = 1.0 - smoothstep(0.0, 0.02, abs(hexDist));

    // Inner rotating triangle (star)
    float2 rotP2 = rotate(p, -time * 0.3);
    float starDist = sdStar(rotP2, 0.12 + subBass * 0.05, 3, 2.5);
    float starEdge = 1.0 - smoothstep(0.0, 0.015, abs(starDist));

    // Concentric circles
    float circles = 0.0;
    for (int i = 0; i < 4; i++) {
        float radius = 0.1 + float(i) * 0.08 + sin(time + float(i)) * 0.02;
        float circleDist = abs(dist - radius);
        float circle = 1.0 - smoothstep(0.0, 0.008, circleDist);
        circles += circle;
    }

    // Combine harmonic shapes
    float harmonicShapes = hexEdge + starEdge * 0.8 + circles * 0.5;
    harmonicShapes = clamp(harmonicShapes, 0.0, 1.0);

    // Harmonic color - clean neon
    float3 harmonicColor = mix(neonCyan, neonMagenta, 0.5 + 0.5 * sin(angle * 2.0 + time));

    // === NOISE SIDE (Low HNR = Chaotic particles) ===

    // Noise-based particles
    float noiseField = 0.0;
    for (int i = 0; i < 5; i++) {
        float2 noiseP = p * (3.0 + float(i) * 2.0);
        noiseP += time * float(i + 1) * 0.1;
        float n = noise(noiseP);
        n = pow(n, 2.0);
        noiseField += n * (1.0 / float(i + 1));
    }
    noiseField = clamp(noiseField, 0.0, 1.0);

    // Turbulent swirls
    float2 turbP = p * 4.0;
    float turbulence = fbm(turbP + time * 0.5, 4);

    // Chaotic speckles
    float speckles = 0.0;
    for (int i = 0; i < 30; i++) {
        float2 specklePos = float2(
            hash(float2(float(i) * 0.1, time * 0.01)) - 0.5,
            hash(float2(float(i) * 0.2, time * 0.01 + 0.5)) - 0.5
        );
        specklePos *= 0.8;
        specklePos.x *= aspectRatio;

        float speckleDist = length(p - specklePos);
        float speckle = exp(-speckleDist * speckleDist * 500.0);
        speckle *= hash(float2(float(i), floor(time * 2.0)));
        speckles += speckle;
    }

    float noiseVisual = noiseField * 0.4 + turbulence * 0.3 + speckles * 0.3;
    noiseVisual = clamp(noiseVisual, 0.0, 1.0);

    // Noise color - harsh, flickering
    float3 noiseColor = mix(hotPink, uvViolet, turbulence);
    noiseColor *= 0.8 + 0.2 * sin(time * 20.0 + noise(p * 10.0) * 10.0);

    // === BLEND based on HNR ===

    // HNR determines the mix: 1.0 = pure harmonic, 0.0 = pure noise
    float harmonicAmount = hnr;
    float noiseAmount = 1.0 - hnr;

    // Apply reactivity to make transition more dramatic
    harmonicAmount = pow(harmonicAmount, 1.0 / (1.0 + reactivity));

    float3 harmonicContrib = harmonicColor * harmonicShapes * harmonicAmount;
    float3 noiseContrib = noiseColor * noiseVisual * noiseAmount;

    float3 finalColor = harmonicContrib + noiseContrib;

    // Add center indicator showing current HNR
    float indicator = smoothstep(0.25, 0.24, dist) - smoothstep(0.24, 0.23, dist);
    float indicatorFill = smoothstep(0.23, 0.22, dist);

    // Split indicator by HNR
    float harmonicSide = step(0.0, p.x);
    float noiseSide = 1.0 - harmonicSide;

    finalColor += neonCyan * indicator * 0.3;
    finalColor += neonCyan * indicatorFill * harmonicSide * hnr * 0.2;
    finalColor += hotPink * indicatorFill * noiseSide * (1.0 - hnr) * 0.2;

    // Background glow
    float bgGlow = exp(-dist * dist * 4.0);
    float3 bgColor = mix(deepPurple, uvViolet * 0.3, dist);
    finalColor += bgColor * (1.0 - clamp(harmonicShapes + noiseVisual, 0.0, 1.0));

    // Peak flash
    if (uniforms.isPeak > 0.5) {
        finalColor += float3(1.0) * uniforms.peakIntensity * 0.15 * exp(-dist * 3.0);
    }

    return float4(finalColor, 1.0);
}
