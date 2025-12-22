//
//  TunnelWarpShader.metal
//  PsytranceVisualizer
//
//  Infinite tunnel effect with warp distortion
//

#include <metal_stdlib>
using namespace metal;

#include "Common.metal"

fragment float4 tunnelWarpFragment(
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
    float pump = uniforms.sidechainPump;
    float hnr = uniforms.hnrRatio;

    // Center and aspect correction
    float aspectRatio = resolution.x / resolution.y;
    float2 p = (uv - 0.5) * 2.0;
    p.x *= aspectRatio;

    // Convert to polar coordinates for tunnel
    float dist = length(p);
    float angle = atan2(p.y, p.x);

    // Avoid division by zero at center
    dist = max(dist, 0.001);

    // Tunnel depth (inverse of distance)
    float depth = 1.0 / dist;

    // Speed controlled by sub-bass
    float baseSpeed = 2.0;
    float audioSpeed = subBass * 3.0 * (0.5 + reactivity * 0.5);
    float speed = baseSpeed + audioSpeed;

    // Warp distortion from sidechain pump
    float warpAmount = pump * 0.5;
    depth += sin(angle * 4.0 + time * 2.0) * warpAmount * 0.5;
    angle += sin(depth * 2.0 + time) * warpAmount * 0.3;

    // Create tunnel coordinates
    float2 tunnelUV = float2(
        angle / (2.0 * 3.14159) + 0.5, // Angular coordinate [0, 1]
        depth + time * speed            // Depth with movement
    );

    // === TUNNEL WALL PATTERNS ===

    // Hexagonal grid pattern
    float2 hexUV = tunnelUV * float2(8.0, 2.0);
    float2 hexCell = floor(hexUV);
    float2 hexFrac = fract(hexUV);

    // Offset every other row
    if (fmod(hexCell.y, 2.0) > 0.5) {
        hexFrac.x = fract(hexFrac.x + 0.5);
    }

    float hexDist = length(hexFrac - 0.5);
    float hexPattern = smoothstep(0.4, 0.35, hexDist);

    // Add concentric rings
    float rings = sin(tunnelUV.y * 20.0) * 0.5 + 0.5;
    rings = smoothstep(0.3, 0.7, rings);

    // Angular segments
    float segments = 8.0;
    float angularLines = abs(sin(angle * segments));
    angularLines = smoothstep(0.95, 1.0, angularLines);

    // Combine patterns
    float pattern = hexPattern * 0.5 + rings * 0.3 + angularLines * 0.2;

    // === COLORING ===

    // Base color cycles with depth and time
    float colorPhase = tunnelUV.y * 0.1 + time * 0.2;
    float3 tunnelColor = psytrancePalette(colorPhase, time);

    // Depth fog (darker towards center/infinity)
    float fog = exp(-dist * 2.0);
    tunnelColor *= fog;

    // Pattern overlay
    float3 patternColor = mix(uvViolet, neonCyan, rings);
    tunnelColor = mix(tunnelColor, patternColor, pattern * 0.5);

    // Edge glow (bright at tunnel edges)
    float edgeGlow = exp(-dist * 5.0);
    tunnelColor = addGlow(tunnelColor, (1.0 - edgeGlow) * 0.3, neonMagenta);

    // Center light (looking into the tunnel)
    float centerLight = exp(-dist * dist * 50.0);
    tunnelColor += float3(1.0) * centerLight * 0.5;

    // HNR affects pattern complexity
    float patternIntensity = hnr;
    tunnelColor *= 0.7 + patternIntensity * 0.3;

    // Add noise for texture
    float noiseVal = noise(tunnelUV * 10.0 + time);
    tunnelColor += uvViolet * noiseVal * 0.1;

    // Pump flash
    if (pump > 0.5) {
        float pumpFlash = (pump - 0.5) * 2.0;
        tunnelColor += neonMagenta * pumpFlash * 0.2;
    }

    // Peak flash
    if (uniforms.isPeak > 0.5) {
        float peakFlash = uniforms.peakIntensity;
        tunnelColor += float3(1.0) * peakFlash * 0.15 * (1.0 - edgeGlow);
    }

    // Speed lines effect
    float speedLines = fract(tunnelUV.y * 50.0 - time * speed * 2.0);
    speedLines = smoothstep(0.95, 1.0, speedLines);
    speedLines *= subBass * 0.5;
    tunnelColor += neonCyan * speedLines;

    return float4(tunnelColor, 1.0);
}
