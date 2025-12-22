//
//  MandelbrotShader.metal
//  PsytranceVisualizer
//
//  Audio-reactive Mandelbrot fractal with zoom and color cycling
//

#include <metal_stdlib>
using namespace metal;

#include "Common.metal"

fragment float4 mandelbrotFragment(
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
    float centroid = uniforms.spectralCentroid;

    // Aspect ratio correction
    float aspectRatio = resolution.x / resolution.y;

    // Map UV to complex plane
    float2 c = (uv - 0.5) * 2.0;
    c.x *= aspectRatio;

    // Audio-reactive zoom level
    // Base zoom increases over time, modulated by sub-bass
    float baseZoom = 1.0 + time * 0.02;
    float audioZoom = subBass * 0.5 * (0.5 + reactivity * 0.5);
    float zoom = pow(2.0, baseZoom + audioZoom);

    // Zoom center - drifts based on sidechain
    float2 zoomCenter = float2(-0.7, 0.0);
    zoomCenter.x += sin(time * 0.1) * 0.3 + pump * 0.1 * sin(time);
    zoomCenter.y += cos(time * 0.13) * 0.2 + pump * 0.1 * cos(time);

    // Apply zoom
    c = c / zoom + zoomCenter;

    // Mandelbrot iteration
    float2 z = float2(0.0);
    int maxIterations = int(50.0 + reactivity * 100.0);
    int iterations = 0;

    float smoothIter = 0.0;

    for (int i = 0; i < 150; i++) {
        if (i >= maxIterations) break;

        // z = z^2 + c
        float2 zNew = float2(
            z.x * z.x - z.y * z.y + c.x,
            2.0 * z.x * z.y + c.y
        );
        z = zNew;

        float mag2 = dot(z, z);
        if (mag2 > 256.0) {
            // Smooth iteration count
            smoothIter = float(i) - log2(log2(mag2)) + 4.0;
            break;
        }

        iterations = i;
    }

    // Normalize iteration count
    float normalizedIter = smoothIter / float(maxIterations);

    // Color based on iterations
    float3 color;

    if (iterations >= maxIterations - 1) {
        // Inside the set - deep color
        color = deepPurple * (0.5 + 0.5 * subBass);
    } else {
        // Outside - color cycling based on iterations and audio
        float colorPhase = normalizedIter + time * 0.1 + centroid;

        // Use psytrance palette with color rotation
        color = psytrancePalette(colorPhase, time);

        // Modulate brightness by iteration depth
        float brightness = 0.5 + 0.5 * sin(smoothIter * 0.3);
        color *= brightness;

        // Add glow at boundary
        float edgeFactor = 1.0 - normalizedIter;
        edgeFactor = pow(edgeFactor, 3.0);
        color = addGlow(color, edgeFactor * 0.5, neonCyan);
    }

    // Sub-bass pulse effect
    color *= 0.8 + 0.2 * subBass;

    // Sidechain breathing
    float breathe = 1.0 + pump * 0.1;
    color *= breathe;

    // Peak flash in bright areas
    if (uniforms.isPeak > 0.5 && iterations < maxIterations - 1) {
        color += neonMagenta * uniforms.peakIntensity * 0.2 * normalizedIter;
    }

    // Subtle vignette
    float2 vignetteuv = uv - 0.5;
    float vignette = 1.0 - dot(vignetteuv, vignetteuv) * 0.5;
    color *= vignette;

    return float4(color, 1.0);
}
