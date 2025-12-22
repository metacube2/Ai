//
//  Common.metal
//  PsytranceVisualizer
//
//  Shared shader functions, types, and psytrance color palette
//

#include <metal_stdlib>
using namespace metal;

// MARK: - Uniforms Structure

struct ShaderUniforms {
    float time;
    float2 resolution;
    float reactivity;

    float subBassEnergy;
    float sidechainPump;
    float sidechainEnvelope;
    float hnrRatio;
    float isPeak;
    float peakIntensity;
    float spectralCentroid;
    float rmsLevel;

    int mode;
    float2 padding;
};

// MARK: - Vertex Data

struct VertexOut {
    float4 position [[position]];
    float2 uv;
};

// MARK: - Psytrance Color Palette

constant float3 neonMagenta = float3(1.0, 0.0, 1.0);
constant float3 neonCyan = float3(0.0, 1.0, 1.0);
constant float3 neonGreen = float3(0.224, 1.0, 0.078);
constant float3 uvViolet = float3(0.482, 0.0, 1.0);
constant float3 hotPink = float3(1.0, 0.2, 0.6);
constant float3 electricBlue = float3(0.0, 0.5, 1.0);
constant float3 deepPurple = float3(0.1, 0.0, 0.15);

// MARK: - Palette Functions

inline float3 getPaletteColor(int index) {
    switch (index % 6) {
        case 0: return neonMagenta;
        case 1: return neonCyan;
        case 2: return neonGreen;
        case 3: return uvViolet;
        case 4: return hotPink;
        default: return electricBlue;
    }
}

inline float3 rainbowPalette(float t) {
    float3 a = float3(0.5, 0.5, 0.5);
    float3 b = float3(0.5, 0.5, 0.5);
    float3 c = float3(1.0, 1.0, 1.0);
    float3 d = float3(0.0, 0.33, 0.67);
    return a + b * cos(6.28318 * (c * t + d));
}

inline float3 psytrancePalette(float t, float time) {
    // Cycle through psytrance colors
    float phase = fract(t + time * 0.1);

    if (phase < 0.2) {
        return mix(uvViolet, neonMagenta, phase * 5.0);
    } else if (phase < 0.4) {
        return mix(neonMagenta, hotPink, (phase - 0.2) * 5.0);
    } else if (phase < 0.6) {
        return mix(hotPink, neonCyan, (phase - 0.4) * 5.0);
    } else if (phase < 0.8) {
        return mix(neonCyan, neonGreen, (phase - 0.6) * 5.0);
    } else {
        return mix(neonGreen, uvViolet, (phase - 0.8) * 5.0);
    }
}

// MARK: - Heatmap for Spectrogram

inline float3 heatmap(float t) {
    // Low energy: dark purple
    // High energy: white through neon colors
    if (t < 0.2) {
        return mix(float3(0.05, 0.0, 0.1), uvViolet, t * 5.0);
    } else if (t < 0.4) {
        return mix(uvViolet, neonMagenta, (t - 0.2) * 5.0);
    } else if (t < 0.6) {
        return mix(neonMagenta, hotPink, (t - 0.4) * 5.0);
    } else if (t < 0.8) {
        return mix(hotPink, neonCyan, (t - 0.6) * 5.0);
    } else {
        return mix(neonCyan, float3(1.0), (t - 0.8) * 5.0);
    }
}

// MARK: - Noise Functions

// Simplex-like noise
inline float hash(float2 p) {
    float3 p3 = fract(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.x + p3.y) * p3.z);
}

inline float noise(float2 p) {
    float2 i = floor(p);
    float2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);

    float a = hash(i);
    float b = hash(i + float2(1.0, 0.0));
    float c = hash(i + float2(0.0, 1.0));
    float d = hash(i + float2(1.0, 1.0));

    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

inline float fbm(float2 p, int octaves) {
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    for (int i = 0; i < octaves; i++) {
        value += amplitude * noise(p * frequency);
        frequency *= 2.0;
        amplitude *= 0.5;
    }

    return value;
}

// 3D noise for volumetric effects
inline float noise3D(float3 p) {
    float3 i = floor(p);
    float3 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);

    float2 uv = i.xy + float2(37.0, 17.0) * i.z;
    float a = hash(uv);
    float b = hash(uv + float2(1.0, 0.0));
    float c = hash(uv + float2(0.0, 1.0));
    float d = hash(uv + float2(1.0, 1.0));

    float2 uv2 = uv + float2(37.0, 17.0);
    float e = hash(uv2);
    float ff = hash(uv2 + float2(1.0, 0.0));
    float g = hash(uv2 + float2(0.0, 1.0));
    float h = hash(uv2 + float2(1.0, 1.0));

    float x1 = mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
    float x2 = mix(mix(e, ff, f.x), mix(g, h, f.x), f.y);

    return mix(x1, x2, f.z);
}

// MARK: - Utility Functions

inline float2 rotate(float2 p, float angle) {
    float c = cos(angle);
    float s = sin(angle);
    return float2(p.x * c - p.y * s, p.x * s + p.y * c);
}

inline float map(float value, float inMin, float inMax, float outMin, float outMax) {
    return outMin + (outMax - outMin) * (value - inMin) / (inMax - inMin);
}

inline float smoothstepEdge(float edge0, float edge1, float x) {
    float t = clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
    return t * t * (3.0 - 2.0 * t);
}

// MARK: - Glow Effect

inline float3 addGlow(float3 color, float intensity, float3 glowColor) {
    return color + glowColor * intensity * intensity;
}

// MARK: - SDF Functions for Geometry

inline float sdCircle(float2 p, float r) {
    return length(p) - r;
}

inline float sdBox(float2 p, float2 b) {
    float2 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}

inline float sdHexagon(float2 p, float r) {
    const float3 k = float3(-0.866025404, 0.5, 0.577350269);
    p = abs(p);
    p -= 2.0 * min(dot(k.xy, p), 0.0) * k.xy;
    p -= float2(clamp(p.x, -k.z * r, k.z * r), r);
    return length(p) * sign(p.y);
}

inline float sdStar(float2 p, float r, int n, float m) {
    float an = 3.141593 / float(n);
    float en = 3.141593 / m;
    float2 acs = float2(cos(an), sin(an));
    float2 ecs = float2(cos(en), sin(en));

    float bn = fmod(atan2(p.x, p.y), 2.0 * an) - an;
    p = length(p) * float2(cos(bn), abs(sin(bn)));
    p -= r * acs;
    p += ecs * clamp(-dot(p, ecs), 0.0, r * acs.y / ecs.y);
    return length(p) * sign(p.x);
}

// MARK: - Vertex Shader (Fullscreen Quad)

vertex VertexOut vertexShader(uint vertexID [[vertex_id]]) {
    // Generate fullscreen quad
    float2 positions[4] = {
        float2(-1.0, -1.0),
        float2( 1.0, -1.0),
        float2(-1.0,  1.0),
        float2( 1.0,  1.0)
    };

    float2 uvs[4] = {
        float2(0.0, 1.0),
        float2(1.0, 1.0),
        float2(0.0, 0.0),
        float2(1.0, 0.0)
    };

    VertexOut out;
    out.position = float4(positions[vertexID], 0.0, 1.0);
    out.uv = uvs[vertexID];
    return out;
}
