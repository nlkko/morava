shader_type canvas_item;
render_mode unshaded;

void fragment(){
	// box filter size in texel units
	vec2 box_size = clamp(fwidth(UV) / TEXTURE_PIXEL_SIZE, 1e-5, 1);
	
	// scale UV by texture size to get texel coordinate
	vec2 tx = UV / TEXTURE_PIXEL_SIZE - 0.5 * box_size;
	
	// compute offset for pixel-sized box filter
	vec2 tx_offset = smoothstep(vec2(1) - box_size, vec2(1), fract(tx));
	
	// compute bilinear sample UV coordinates
	vec2 uv = (floor(tx) + 0.5 + tx_offset) * TEXTURE_PIXEL_SIZE;
	
	// sample the texture
	COLOR = textureGrad(TEXTURE, uv, dFdx(UV), dFdy(UV));
}