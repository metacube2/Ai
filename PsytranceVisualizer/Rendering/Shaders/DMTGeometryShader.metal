//
//  DMTGeometryShader.metal
//  PsytranceVisualizer
//
//  Sacred geometry patterns: Flower of Life, Metatron's Cube, Sri Yantra, Hexagonal
//

#include <metal_stdlib>
using namespace metal;

#include "Common.metal"

// === SACRED GEOMETRY PRIMITIVES ===

// Flower of Life - overlapping circles
float flowerOfLife(float2 p, float scale, float time) {
    p *= scale;

    float result = 0.0;
    float circleRadius = 0.5;

    // Center circle
    result = max(result, 1.0 - smoothstep(circleRadius - 0.02, circleRadius, length(p)));

    // 6 circles around center
    for (int i = 0; i < 6; i++) {
        float angle = float(i) * 3.14159 / 3.0 + time * 0.1;
        float2 offset = float2(cos(angle), sin(angle)) * circleRadius;
        float d = length(p - offset);
        result = max(result, 1.0 - smoothstep(circleRadius - 0.02, circleRadius, d));
    }

    // Second ring of 12 circles
    for (int i = 0; i < 12; i++) {
        float angle = float(i) * 3.14159 / 6.0 + time * 0.05;
        float2 offset = float2(cos(angle), sin(angle)) * circleRadius * 2.0;
        float d = length(p - offset);
        result = max(result, 0.5 * (1.0 - smoothstep(circleRadius - 0.02, circleRadius, d)));
    }

    return result;
}

// Metatron's Cube - 13 circles with connecting lines
float metatronsCube(float2 p, float scale, float time) {
    p *= scale;

    float result = 0.0;
    float nodeRadius = 0.08;
    float lineWidth = 0.01;

    // Define the 13 points of Metatron's Cube
    float2 points[13];
    points[0] = float2(0.0, 0.0); // Center

    // Inner hexagon
    for (int i = 0; i < 6; i++) {
        float angle = float(i) * 3.14159 / 3.0 + time * 0.1;
        points[i + 1] = float2(cos(angle), sin(angle)) * 0.5;
    }

    // Outer hexagon (rotated)
    for (int i = 0; i < 6; i++) {
        float angle = float(i) * 3.14159 / 3.0 + 3.14159 / 6.0 + time * 0.1;
        points[i + 7] = float2(cos(angle), sin(angle)) * 0.866;
    }

    // Draw nodes
    for (int i = 0; i < 13; i++) {
        float d = length(p - points[i]);
        float node = 1.0 - smoothstep(nodeRadius - 0.01, nodeRadius, d);
        result = max(result, node);
    }

    // Draw connecting lines
    for (int i = 0; i < 13; i++) {
        for (int j = i + 1; j < 13; j++) {
            float2 a = points[i];
            float2 b = points[j];
            float2 pa = p - a;
            float2 ba = b - a;
            float t = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
            float d = length(pa - ba * t);
            float line = 1.0 - smoothstep(lineWidth, lineWidth + 0.005, d);
            result = max(result, line * 0.5);
        }
    }

    return result;
}

// Sri Yantra - 9 interlocking triangles
float sriYantra(float2 p, float scale, float time) {
    p *= scale;

    float result = 0.0;
    float lineWidth = 0.015;

    // Rotating factor
    float rot = time * 0.05;

    // Draw 4 upward triangles
    for (int i = 0; i < 4; i++) {
        float size = 0.3 + float(i) * 0.15;
        float yOffset = -0.1 + float(i) * 0.05;

        float2 tp = p - float2(0.0, yOffset);
        tp = rotate(tp, rot);

        // Triangle SDF
        float2 a = float2(0.0, size);
        float2 b = float2(-size * 0.866, -size * 0.5);
        float2 c = float2(size * 0.866, -size * 0.5);

        float d1 = dot(tp - a, normalize(float2(b.y - a.y, a.x - b.x)));
        float d2 = dot(tp - b, normalize(float2(c.y - b.y, b.x - c.x)));
        float d3 = dot(tp - c, normalize(float2(a.y - c.y, c.x - a.x)));

        float triangleDist = max(max(d1, d2), d3);
        float edge = 1.0 - smoothstep(0.0, lineWidth, abs(triangleDist));
        result = max(result, edge * (1.0 - float(i) * 0.15));
    }

    // Draw 5 downward triangles
    for (int i = 0; i < 5; i++) {
        float size = 0.25 + float(i) * 0.12;
        float yOffset = 0.1 - float(i) * 0.04;

        float2 tp = p - float2(0.0, yOffset);
        tp = rotate(tp, -rot);

        float2 a = float2(0.0, -size);
        float2 b = float2(-size * 0.866, size * 0.5);
        float2 c = float2(size * 0.866, size * 0.5);

        float d1 = dot(tp - a, normalize(float2(b.y - a.y, a.x - b.x)));
        float d2 = dot(tp - b, normalize(float2(c.y - b.y, b.x - c.x)));
        float d3 = dot(tp - c, normalize(float2(a.y - c.y, c.x - a.x)));

        float triangleDist = max(max(d1, d2), d3);
        float edge = 1.0 - smoothstep(0.0, lineWidth, abs(triangleDist));
        result = max(result, edge * (1.0 - float(i) * 0.12));
    }

    // Central bindu (point)
    float bindu = 1.0 - smoothstep(0.03, 0.04, length(p));
    result = max(result, bindu);

    return result;
}

// Hexagonal grid pattern
float hexagonalPattern(float2 p, float scale, float time) {
    p *= scale;

    // Hexagonal grid transformation
    float2 s = float2(1.0, 1.732);
    float2 h = s * 0.5;

    float2 a = fmod(p, s) - h;
    float2 b = fmod(p + h, s) - h;

    float2 gv = dot(a, a) < dot(b, b) ? a : b;

    float hexDist = max(abs(gv.x), dot(abs(gv), normalize(float2(1.0, 1.732))));

    float edge = 1.0 - smoothstep(0.4, 0.42, hexDist);
    float fill = smoothstep(0.38, 0.4, hexDist);

    // Animate individual hexagons
    float2 cellId = floor(p / s);
    float cellPhase = hash(cellId + floor(time * 0.5)) * 2.0 * 3.14159;
    float pulse = 0.5 + 0.5 * sin(time * 3.0 + cellPhase);

    return edge + fill * pulse * 0.3;
}

// === MAIN FRAGMENT SHADER ===

fragment float4 dmtGeometryFragment(
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
    float hnr = uniforms.hnrRatio;
    float peak = uniforms.isPeak;
    float peakIntensity = uniforms.peakIntensity;

    // Aspect ratio correction
    float aspectRatio = resolution.x / resolution.y;
    float2 p = (uv - 0.5) * 2.0;
    p.x *= aspectRatio;

    // Scale pulsing with sub-bass
    float scale = 2.0 + subBass * 0.5 * (0.5 + reactivity * 0.5);
    p *= scale;

    // Rotation
    float rotation = time * 0.1;
    p = rotate(p, rotation);

    // Determine which geometry to show
    // Changes on peaks or every few seconds
    float cycleTime = 8.0; // Seconds per geometry
    float cyclePhase = fmod(time, cycleTime * 4.0) / cycleTime;
    int geometryIndex = int(cyclePhase);

    // Force change on strong peaks
    if (peak > 0.5 && peakIntensity > 0.7) {
        geometryIndex = int(fmod(float(geometryIndex) + 1.0, 4.0));
    }

    // Calculate all geometries (for blending)
    float flower = flowerOfLife(p, 1.0, time);
    float metatron = metatronsCube(p, 1.5, time);
    float yantra = sriYantra(p, 1.2, time);
    float hexGrid = hexagonalPattern(p, 3.0, time);

    // Select primary and secondary for blending
    float primary = 0.0;
    float secondary = 0.0;
    float blendPhase = fract(cyclePhase);

    switch (geometryIndex) {
        case 0:
            primary = flower;
            secondary = metatron;
            break;
        case 1:
            primary = metatron;
            secondary = yantra;
            break;
        case 2:
            primary = yantra;
            secondary = hexGrid;
            break;
        default:
            primary = hexGrid;
            secondary = flower;
            break;
    }

    // Smooth transition
    float transitionWindow = 0.2; // 20% of cycle for transition
    float blend = smoothstep(1.0 - transitionWindow, 1.0, blendPhase);
    float geometry = mix(primary, secondary, blend);

    // Complexity based on HNR (more harmonic = more detail)
    geometry *= 0.7 + hnr * 0.3;

    // Color based on geometry and audio
    float colorPhase = time * 0.1 + geometry * 0.5;
    float3 geometryColor = psytrancePalette(colorPhase, time);

    // Glow intensity from peak
    float glowIntensity = 0.5 + peakIntensity * 0.5;
    float3 glowColor = mix(neonMagenta, neonCyan, 0.5 + 0.5 * sin(time));

    // Compose final color
    float3 finalColor = geometryColor * geometry;

    // Add glow
    finalColor = addGlow(finalColor, geometry * glowIntensity, glowColor);

    // Background - subtle pulsing gradient
    float dist = length(uv - 0.5);
    float3 bgColor = mix(deepPurple, uvViolet * 0.3, dist);
    bgColor *= 0.8 + 0.2 * subBass;

    finalColor = mix(bgColor, finalColor, clamp(geometry * 1.5, 0.0, 1.0));

    // Peak flash
    if (peak > 0.5) {
        finalColor += float3(1.0) * peakIntensity * 0.2;
    }

    // Outer glow
    float outerGlow = exp(-dist * 3.0);
    finalColor += neonMagenta * outerGlow * 0.1 * subBass;

    return float4(finalColor, 1.0);
}
