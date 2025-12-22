//
//  SubBassShader.metal
//  PsytranceVisualizer
//
//  Pulsating rings visualizing sub-bass energy below 100Hz
//

#include <metal_stdlib>
using namespace metal;

#include "Common.metal"

fragment float4 subBassFragment(
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
    float subBass = uniforms.subBassEnergy;

    // Center coordinates
    float2 center = float2(0.5, 0.5);
    float aspectRatio = resolution.x / resolution.y;

    // Correct for aspect ratio
    float2 p = uv - center;
    p.x *= aspectRatio;

    float dist = length(p);
    float angle = atan2(p.y, p.x);

    // Main pulsating circle
    float baseRadius = 0.15;
    float pulseAmount = subBass * (0.5 + reactivity * 0.5);
    float mainRadius = baseRadius + pulseAmount * 0.2;

    // Add wobble based on angle
    float wobble = sin(angle * 4.0 + time * 2.0) * 0.02 * subBass;
    mainRadius += wobble;

    // Core circle
    float coreDist = abs(dist - mainRadius);
    float coreGlow = exp(-coreDist * coreDist * 200.0);

    // Inner fill with gradient
    float innerFill = smoothstep(mainRadius, mainRadius * 0.3, dist);
    innerFill *= 0.5 + 0.5 * subBass;

    // Expanding rings
    const int numRings = 6;
    float ringIntensity = 0.0;

    for (int i = 0; i < numRings; i++) {
        // Each ring expands outward over time
        float ringPhase = fract(time * 0.3 - float(i) * 0.15);
        float ringRadius = mainRadius + ringPhase * 0.5;

        // Get historical sub-bass value for this ring
        int histIndex = clamp(int(ringPhase * 64.0), 0, 63);
        float histValue = historyData[histIndex];

        // Ring thickness based on historical energy
        float thickness = 0.005 + histValue * 0.01;
        float ringDist = abs(dist - ringRadius);

        // Ring visibility
        float ring = exp(-ringDist * ringDist / (thickness * thickness));
        ring *= (1.0 - ringPhase); // Fade as it expands
        ring *= histValue; // Intensity based on history

        ringIntensity += ring;
    }

    // Color composition
    float3 coreColor = mix(uvViolet, neonMagenta, subBass);
    float3 ringColor = mix(neonMagenta, hotPink, 0.5 + 0.5 * sin(time));

    float3 finalColor = float3(0.0);

    // Add core
    finalColor += coreColor * (innerFill + coreGlow * 2.0);

    // Add rings
    finalColor += ringColor * ringIntensity * 0.8;

    // Add central glow
    float centerGlow = exp(-dist * dist * 10.0) * subBass;
    finalColor += uvViolet * centerGlow * 0.5;

    // Add angular rays on peaks
    if (uniforms.isPeak > 0.5) {
        float rays = abs(sin(angle * 8.0 + time * 5.0));
        rays = pow(rays, 4.0) * exp(-dist * 2.0);
        rays *= uniforms.peakIntensity;
        finalColor += neonCyan * rays * 0.5;
    }

    // Outer vignette
    float vignette = 1.0 - smoothstep(0.3, 0.8, dist);
    finalColor *= vignette;

    // Background pulse
    float bgPulse = subBass * 0.1;
    finalColor += deepPurple * bgPulse;

    // Add noise texture for organic feel
    float noiseVal = noise(p * 20.0 + time);
    finalColor += uvViolet * noiseVal * 0.02 * subBass;

    return float4(finalColor, 1.0);
}
