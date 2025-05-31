shader_type spatial;

void fragment() {
    // Convert normals from [-1, 1] range to [0, 1] range for color display
    vec3 normal_color = NORMAL * 0.5 + 0.5;
    ALBEDO = normal_color;
}