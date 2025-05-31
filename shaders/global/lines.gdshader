shader_type spatial;
render_mode unshaded, depth_prepass_alpha;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform sampler2D depth_texture : hint_depth_texture, repeat_disable, filter_nearest;
uniform sampler2D normal_roughness_texture : hint_normal_roughness_texture, repeat_disable, filter_nearest;

group_uniforms relative_color;
uniform bool relative_color = true; // outline darker, edges lighter
uniform float outline_strength : hint_range(-1.0, 1.0, 0.01) = -0.04;
uniform float edge_strength : hint_range(-1.0, 1.0, 0.01) = 0.12;

group_uniforms color;
uniform vec4 albedo : source_color = vec4(0.75, 0.75, 0.75, 1.0);
uniform vec4 outline_color : source_color = vec4(0, 0, 1, 1.0);
uniform vec4 edge_color : source_color = vec4(1, 0, 0, 1.0);

group_uniforms outline;
uniform float depth_threshold : hint_range(0.0, 1.0, 0.01) = 0.14;
uniform float depth_normal_threshold : hint_range(0.0, 1.0, 0.01) = 0.04;
uniform float depth_normal_threshold_scale = 2.0;

group_uniforms edge;
uniform float convex_threshold = 0.2;
uniform float convex_normal_threshold : hint_range(0.0, 1.0, 0.01) = 0.25;
uniform float convex_normal_threshold_scale = 2.0;

group_uniforms debug;
uniform bool normals_debug = false;
uniform bool depth_debug = false;
uniform bool convex_normal_threshold_debug = false;
uniform bool convex_normal_threshold_scale_debug = false;
uniform bool depth_normal_threshold_scale_debug = false;
uniform bool depth_threshold_debug = false;

// a little function/macro to encapsulate getting the view position
vec3 _view_from_screen(vec2 _uv, float _depth, mat4 _inv_proj) {
	vec4 _upos = _inv_proj * vec4(_uv * 2.0 - 1.0, _depth, 1.0);
	return _upos.xyz / _upos.w;
}
#define view_from_screen(uv, depth_tex) _view_from_screen((uv), textureLod((depth_tex), (uv), 0.0).r, INV_PROJECTION_MATRIX)

float remap(float value, float low1, float high1, float low2, float high2) {
    return low2 + (value - low1) * (high2 - low2) / (high1 - low1);
}

void fragment() {
	vec4 screen_color = texture(screen_texture, SCREEN_UV);
	ALBEDO = screen_color.rgb;
	
	vec2 texel_size = 1.0 / VIEWPORT_SIZE;
	
	vec2 offset[5] = {
					 vec2(-1, 0),
		vec2(0, -1), vec2( 0, 0), vec2(0, 1),
					 vec2( 1, 0)
	};
	vec3 pos[5];
	vec3 norm[5];
	
	// loop over offsets
	for (int i = 0; i < 5; i++) {
		// fragment uv + neighbour offset
		vec2 uv = SCREEN_UV + offset[i] * texel_size;
		// fragment position in view space
		pos[i] = view_from_screen(uv, depth_texture);
		// fragment normal in view space
		norm[i] = texture(normal_roughness_texture, uv).xyz * 2.0 - 1.0;
	}
	
	// scale depth threshold based on view normal; remap normal from min..1 -> 1..max
	float depth_threshold_normal_bias = remap(
		// NOTE(david): in orthogonal projections, normal.z is equivalent to dot(normal, -view_forward)
		max(depth_normal_threshold, 1.0 - norm[2].z),
		depth_normal_threshold, 1.0,
		1.0, depth_normal_threshold_scale
	);
	// final depth threshold
	float depth_thresh = depth_threshold * depth_threshold_normal_bias;
	
	// same for convexity; remap normal from 0..max -> low..1
	float convex_threshold_normal_bias = remap(
		min(norm[2].z, convex_normal_threshold),
		0.0, convex_normal_threshold,
		convex_normal_threshold_scale, 1.0
	);
	float convex_thresh = convex_threshold * convex_threshold_normal_bias;
	
	// ===== outline ===== //
	bool is_outline = 
		pos[2].z - pos[0].z > depth_thresh ||
		pos[2].z - pos[1].z > depth_thresh ||
		pos[2].z - pos[3].z > depth_thresh ||
		pos[2].z - pos[4].z > depth_thresh;
	
	if (is_outline) {
		if (relative_color) {
			// Make outlines darker than the original color
			ALBEDO = mix(ALBEDO, vec3(1.0), outline_strength);
		} else {
			// Use fixed outline color
			ALBEDO = outline_color.rgb;
			ALPHA = outline_color.a;
		}
	}
	
		// ===== edge ===== //
	float is_edge = 0.0;
	for (int i = 0; i < 5; i++) {
		if (i == 2) continue;
		// cross product gets convexity/concavity (mostly)
		vec3 cp = cross(norm[2], norm[i]);
		float convexity = dot(cp, vec3(offset[i].yx, 0));
		float norm_bias = i < 2 ? 1e-5 : 0.0;
		is_edge +=
			// is this below the depth threshold
			abs(pos[i].z - pos[2].z) < depth_thresh
			// is the normal more oblique; less aligned with the camera (up/left takes priority when even)
			&& norm[i].z + norm_bias > norm[2].z
			// is this a convex edge
			&& convexity > convex_thresh
			? 1.0 : 0.0;
	}
	
	if (is_edge > 0.0) {
		if (relative_color) {
			// Make edges lighter than the original color
			ALBEDO = mix(ALBEDO, vec3(1.0), edge_strength);
		} else {
			// Use fixed edge color
			ALBEDO = edge_color.rgb;
			ALPHA = edge_color.a;
		}
	}
	
	// Neither edge nor outline, safely discard.
	if (ALBEDO == vec3(0.0)) {
		discard;
	}
	
	// ===== DEBUGS ===== //
	float depth = texture(depth_texture, SCREEN_UV).r;
	depth = remap(depth, 0.12, 1.0, 0.0, 1.0);
	
	// make it easier to see
	if (depth_debug) {
		ALBEDO = vec3(depth);
	}
	
	if (normals_debug) {
		ALBEDO = texture(normal_roughness_texture, SCREEN_UV).xyz * 2.0 - 1.0;
	}
	
	if (convex_normal_threshold_debug) {
		ALBEDO = vec3(mix(1.0, norm[2].z, convex_normal_threshold_scale));
	}
	
	if (convex_normal_threshold_scale_debug) {
		ALBEDO = vec3(norm[2].z);
	}
	
	//ALBEDO = vec3(remap(min(norm[2].z, 0.5), 0.0, 0.5, 0.1, 1.0));
	
	if (depth_normal_threshold_scale_debug) {
		ALBEDO = vec3(mix(1.0, 1.0 - norm[2].z, depth_normal_threshold_scale));
	}
	
	if (depth_threshold_debug) {
		ALBEDO = vec3(depth_thresh);
	}
}