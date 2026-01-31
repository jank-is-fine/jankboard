#version 330 core
out vec4 FragColor;

in vec2 TexCoords;

uniform sampler2D uscreenTexture;
uniform sampler2D uprotanope_lut;

//https://github.com/andrewwillmott/colour-blind-luts/tree/main
vec3 applyLUT(sampler2D lut, vec3 c)
{
    c = clamp(c, 0.0, 1.0);
    
    float u = (c.r * 31.0 + 0.5) / 32.0;
    float v = (c.b * 31.0 + 0.5) / 32.0;
    float slice = c.g * 31.0;

    float slice_floor = floor(slice);
    float slice_ceil = ceil(slice);
    float slice_fract = fract(slice);

    vec2 uv_g0 = vec2((u + slice_floor) / 32.0, v);
    vec2 uv_g1 = vec2((u + slice_ceil) / 32.0, v);

    uv_g0 = clamp(uv_g0, 0.0, 1.0);
    uv_g1 = clamp(uv_g1, 0.0, 1.0);

    vec4 lut_g0 = texture(lut, uv_g0);
    vec4 lut_g1 = texture(lut, uv_g1);

    return mix(lut_g0.rgb, lut_g1.rgb, slice_fract);
}

void main()
{     
    vec3 originalColor = texture(uscreenTexture, TexCoords).rgb;
    vec3 protanopeCorrected = applyLUT(uprotanope_lut, originalColor);
    
    FragColor = vec4(protanopeCorrected, 1.0);
}