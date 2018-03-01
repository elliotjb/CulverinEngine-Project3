#version 330 core
layout (location = 0) in vec3 position;
layout (location = 1) in vec2 texCoord;
layout (location = 2) in vec3 normal;
layout (location = 3) in vec4 color;


out vec4 ourColor;
out vec2 TexCoord;
out vec3 ourNormal;
out vec3 ourPos;
out float wave_height;

uniform float waveHeight;
uniform float num_waves_x;
uniform float num_waves_y;

uniform vec4 _color;
uniform float _time;
uniform mat4 model;
uniform mat4 viewproj;
uniform mat4 view;

vec3 CalcNormal(in float x, in float y){

float A = cos(x +y+ _time/2) +  cos(3*x +y+ _time/2);// sin(x*4.46 + cos(2.9*x + _time)+_time);// + 4.46*cos( 4.46* x);// + 4.6*cos(4.6*x);
float B =  cos(y+x/2 + _time/2) + cos(y*4+x/2 +_time/2);

return vec3(A,B/2,0);
}

void main()
{


float x =  position.x * num_waves_x;
float y = position.z * num_waves_y;
float t = _time/2;
float distortion_x = sin(x+y + t )+ sin(3*x +y+ t );//+ sin(2.9*x + t);// + sin(4.46*x );
float distortion_y = sin(y +x/2 + t ) + sin(4*y + x/2 + t );
gl_Position =  viewproj* model * vec4(position.x,waveHeight * (position.y + distortion_x + distortion_y ), position.z, 1.0f);
mat4 test = view;
ourColor = _color;
ourNormal = CalcNormal(x, y);
TexCoord = texCoord;
ourPos = position;
wave_height = position.y + distortion_x + distortion_y;

}
