shader_type spatial;

render_mode depth_prepass_alpha, depth_draw_opaque;

uniform vec4 albedo : source_color;

uniform sampler2D texture_albedo : source_color, filter_nearest, repeat_enable;

uniform float wrap : hint_range(-2.0f, 2.0f) = 0.0f;

uniform float steepness : hint_range(1.0f, 8.0f) = 1.0f;

uniform int cuts : hint_range(1, 8) = 3;

uniform bool billboard = true;

varying vec3 instance_origin;

void vertex() {

// Billboard Mode: Enabled
if (billboard) {
	MODELVIEW_MATRIX = mat4(MODEL_MATRIX[0], MODEL_MATRIX[1], MODEL_MATRIX[2], VIEW_MATRIX * MODEL_MATRIX[3]);
}

instance_origin = MODEL_MATRIX[3].xyz;

}

void fragment() {

vec2 base_uv = UV;

LIGHT_VERTEX = instance_origin;

vec4 albedo_tex = texture(texture_albedo, base_uv);

ALBEDO = albedo.rgb * albedo_tex.rgb;

ALPHA *= albedo.a * albedo_tex.a;

}

void light() {

float attenuation = ATTENUATION;

float NdotL = dot(NORMAL, LIGHT);

float diffuse_amount = NdotL + (attenuation - 1.0) + wrap;

diffuse_amount *= steepness;

float cuts_inv = 1.0f / float(cuts);

float diffuse_stepped = clamp(diffuse_amount + mod(1.0f - diffuse_amount, cuts_inv), 0.0f, 1.0f);

// Apply diffuse result

vec3 diffuse = ALBEDO.rgb * LIGHT_COLOR / PI;

diffuse *= diffuse_stepped;

DIFFUSE_LIGHT += diffuse;

}