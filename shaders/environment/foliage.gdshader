shader_type spatial;
render_mode depth_prepass_alpha, depth_draw_opaque;

// TODO:
// Add specular lighting

group_uniforms albedo;
uniform vec4 albedo : source_color = vec4(1.0f);
uniform sampler2D albedo_texture : source_color;

group_uniforms shading;
uniform bool use_attenuation = true;
uniform bool use_billboarding = false;
uniform bool use_specular = true;

group_uniforms light_ramp;
uniform int cuts : hint_range(1, 8) = 3;
uniform float wrap : hint_range(-2.0f, 2.0f) = 0.0f;
uniform float steepness : hint_range(1.0f, 8.0f) = 1.0f;

varying vec3 instance_origin;

void vertex() {
	// save the instance origin value, later used to shade whole mesh as one
	instance_origin = MODEL_MATRIX[3].xyz;
	
	// Billboard foliage
	if (use_billboarding) {
		MODELVIEW_MATRIX = mat4(MODEL_MATRIX[0], MODEL_MATRIX[1], MODEL_MATRIX[2], VIEW_MATRIX * MODEL_MATRIX[3]);
	}
}

void fragment() {
	// Calculate light at instance origin
	//LIGHT_VERTEX = instance_origin;
	ALBEDO = albedo.rgb * texture(albedo_texture, UV).rgb;
}

void light() {
	// Attenuation.
	float attenuation = 1.0f;
	if (use_attenuation) {
		attenuation = ATTENUATION;
	}
	
	// Diffuse lighting.
	float NdotL = dot(NORMAL, LIGHT);
	float diffuse_amount = NdotL + (attenuation - 1.0) + wrap;
	//float diffuse_amount = NdotL * attenuation + wrap;
	diffuse_amount *= steepness;
	float cuts_inv = 1.0f / float(cuts);
	float diffuse_stepped = clamp(diffuse_amount + mod(1.0f - diffuse_amount, cuts_inv), 0.0f, 1.0f);
	
	// Add color of light
	vec3 light_color_normalized = normalize(LIGHT_COLOR);
	DIFFUSE_LIGHT += diffuse_stepped * light_color_normalized;
}
