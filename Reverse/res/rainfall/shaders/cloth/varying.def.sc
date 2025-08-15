vec2 v_texcoord0 : TEXCOORD0 = vec2(0.0, 0.0);
vec3 v_position  : TEXCOORD1 = vec3(0.0, 0.0, 0.0);
vec3 v_view      : TEXCOORD2 = vec3(0.0, 0.0, 0.0);
vec3 v_normal    : NORMAL    = vec3(0.0, 0.0, 1.0);
vec3 v_tangent   : TANGENT   = vec3(1.0, 0.0, 0.0);
vec3 v_bitangent : BINORMAL  = vec3(0.0, 1.0, 0.0);
vec4 v_color0    : COLOR     = vec4(0.8, 0.8, 0.8, 1.0);

vec3 a_position  : POSITION;
vec3 a_normal    : NORMAL;
vec3 a_tangent   : TANGENT;
vec4 a_indices   : BLENDINDICES;
vec4 a_weight    : BLENDWEIGHT;
vec2 a_texcoord0 : TEXCOORD0;
vec4 a_texcoord1 : TEXCOORD1;
vec4 a_texcoord2 : TEXCOORD2;
vec4 a_color0    : COLOR0;

vec4 i_data0     : TEXCOORD7;
vec4 i_data1     : TEXCOORD6;
vec4 i_data2     : TEXCOORD5;
vec4 i_data3     : TEXCOORD4;
vec4 i_data4     : TEXCOORD3;