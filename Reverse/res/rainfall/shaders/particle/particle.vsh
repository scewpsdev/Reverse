$input a_position, i_data0, i_data1, i_data2
$output v_position, v_texcoord0, v_color0


#include "../common/common.shader"


uniform vec4 u_cameraAxisRight;
uniform vec4 u_cameraAxisUp;


void main()
{
	vec3 position = i_data0.xyz;
	float rotation = i_data0.w;
	vec4 color = i_data1;
	float size = i_data2.x;
	float xscale = i_data2.y;
	float animation = i_data2.z;
	
	vec2 localPosition = vec2(a_position.x * size * xscale * cos(rotation) - a_position.y * size * sin(rotation),
							  cos(rotation) * size * a_position.y + sin(rotation) * a_position.x * size * xscale);
	vec2 texcoord = a_position * vec2(1, -1) + 0.5;

	position += localPosition.x * u_cameraAxisRight.xyz + localPosition.y * u_cameraAxisUp.xyz;

	gl_Position = mul(u_viewProj, vec4(position, 1.0));

	v_position = position;
	v_texcoord0 = vec3(texcoord, animation);
	v_color0 = SRGBToLinear(color);
}
