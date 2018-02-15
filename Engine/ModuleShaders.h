#ifndef MODULE_SHADERS_H_
#define MODULE_SHADERS_H_
#include"Module.h"
#include "MathGeoLib.h"


#include <string>
#include <list>
#include <vector>
#include "GL3W\include\glew.h"
#include "Resource_.h"
#include "ResourceMaterial.h"
#include "ShadersLib.h"

#pragma comment (lib, "GL3W/libx86/glew32.lib") 
typedef unsigned int uint;


struct Light {
	Light() {
		position = float3::zero;
		color = float4(255, 255, 255, 255);
		type = 0;
		attenuation = 1;
		ambientCoefficient = 1;
	}

	float3 position;
	float4 color; //a.k.a. the color of the light
	int type;
	float attenuation;
	float ambientCoefficient;

};

class ModuleShaders:public Module
{
public:

	ModuleShaders();
	~ModuleShaders();

	bool Init(JSON_Object* node);
	bool Start();
	update_status PreUpdate(float dt);
	update_status Update(float dt);
	update_status PostUpdate(float dt);
	update_status UpdateConfig(float dt);
	bool SaveConfig(JSON_Object* node);

	bool CleanUp();

	ShaderProgram * CreateShader(const char* name);
	char* GetShaderText(std::string path);
	ShaderProgram*   CreateDefaultShader();

	Shader*   CompileShader(std::string path, std::string name, ShaderType type);
	std::string GetShaderError(uint ID);

	
	void   AddShaderList(Shader* newShader);

	void ImportShaderObjects();

	void ImportShaderMaterials();

	Shader* GetShaderByName(const char* name, ShaderType type);

	template <typename T>
	void SetLightUniform(uint ID, const char* propertyName, size_t lightIndex, const T& value) {

		std::ostringstream ss;
		ss << "_lights[" << lightIndex << "]." << propertyName;
		std::string uniformName = ss.str();

		SetUniform(ID, uniformName.c_str(), (T)value);

	}

	void SetUniform(uint ID, const GLchar* uniformName, float3& v);
	void SetUniform(uint ID, const GLchar* uniformName, float4& v);
	void SetUniform(uint ID, const GLchar* uniformName, int& v);
	void SetUniform(uint ID, const GLchar* uniformName, float& v);

	//Event system test
	bool SetEventListenrs();
	void OnEvent(Event& event);

	std::string Shader_Directory_fs;
	std::vector<Shader*> shaders;
	std::vector<ShaderProgram*> programs;

public:
	std::string last_shader_error = "";
	std::vector<Light> lights;

};


#endif


