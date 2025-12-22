//
//  SidechainPumpShader.metal
//  PsytranceVisualizer
//
//  Visualizes sidechain pumping with breathing zoom effect
//

#include <metal_stdlib>
using namespace metal;

#include "Common.metal"

fragment float4 sidechainPumpFragment(
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

    float pump = uniforms.sidechainPump;
    float envelope = uniforms.sidechainEnvelope;
    float subBass = uniforms.subBassEnergy;

    // Center and aspect ratio correction
    float2 center = float2(0.5, 0.5);
    float aspectRatio = resolution.x / resolution.y;

    float2 p = uv - center;
    p.x *= aspectRatio;

    // Apply breathing zoom effect
    float zoomAmount = 1.0 + pump * 0.3 * (0.5 + reactivity * 0.5);
    p /= zoomAmount;

    // Radial distortion synchronized with pump
    float dist = length(p);
    float angle = atan2(p.y, p.x);

    // Pump-synced radial waves
    float radialWave = sin(dist * 15.0 - time * 3.0 + envelope * 10.0);
    radialWave *= pump * 0.3;

    // Apply distortion
    float2 distortedP = p;
    distortedP *= 1.0 + radialWave * 0.1;

    // Create concentric pulse rings
    float rings = 0.0;
    const int numRings = 5;

    for (int i = 0; i < numRings; i++) {
        float ringPhase = fract(time * 0.5 + float(i) * 0.2 - envelope * 0.5);
        float ringRadius = ringPhase * 0.6;
        float ringWidth = 0.02 + pump * 0.03;

        float ringDist = abs(dist - ringRadius);
        float ring = exp(-ringDist * ringDist / (ringWidth * ringWidth));
        ring *= 1.0 - ringPhase; // Fade out as it expands
        ring *= pump;

        rings += ring;
    }

    // Breathing glow in center
    float breathIntensity = 0.5 + 0.5 * sin(time * 4.0 + envelope * 6.28318);
    breathIntensity *= pump;

    float centerGlow = exp(-dist * dist * 8.0);
    centerGlow *= breathIntensity;

    // Color based on pump phase
    float3 pumpColor = mix(uvViolet, neonMagenta, envelope);
    float3 ringColor = mix(neonCyan, hotPink, pump);

    // Background pattern - angular sectors that pulse
    float sectors = 8.0;
    float sectorAngle = fract(angle / (2.0 * 3.14159) * sectors);
    float sectorPulse = smoothstep(0.4, 0.5, sectorAngle) - smoothstep(0.5, 0.6, sectorAngle);
    sectorPulse *= pump * 0.3;
    sectorPulse *= exp(-dist * 3.0);

    // Spiral pattern
    float spiral = fract(angle / (2.0 * 3.14159) * 3.0 + dist * 5.0 - time * 0.5);
    spiral = smoothstep(0.4, 0.5, spiral) - smoothstep(0.5, 0.6, spiral);
    spiral *= pump * 0.2;
    spiral *= exp(-dist * 2.0);

    // Compose final color
    float3 finalColor = float3(0.0);

    // Base gradient
    float3 bgGradient = mix(deepPurple, uvViolet * 0.3, dist);
    finalColor += bgGradient;

    // Add rings
    finalColor += ringColor * rings;

    // Add center glow
    finalColor += pumpColor * centerGlow;

    // Add sector pulse
    finalColor += neonGreen * sectorPulse;

    // Add spiral
    finalColor += electricBlue * spiral;

    // Screen flash on strong pump
    if (pump > 0.7) {
        float flash = (pump - 0.7) / 0.3;
        flash *= 0.2;
        finalColor += neonMagenta * flash;
    }

    // Peak highlight
    if (uniforms.isPeak > 0.5) {
        float peakFlash = uniforms.peakIntensity * 0.2;
        finalColor += float3(1.0) * peakFlash * exp(-dist * 5.0);
    }

    // Vignette
    float vignette = 1.0 - smoothstep(0.4, 0.8, dist);
    finalColor *= 0.7 + vignette * 0.3;

    return float4(finalColor, 1.0);
}
