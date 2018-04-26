#include "JSONSerialization.h"
#include "Application.h"
#include "ResourceMaterial.h"
#include "ResourceScript.h"
#include "ResourceFont.h"
#include "ResourceAnimation.h"
#include "CompScript.h"
#include "ModuleFS.h"
#include "ModuleResourceManager.h"
#include "Scene.h"
#include "GameObject.h"
#include "ModuleAudio.h"
#include "InputManager.h"
#include "InputAction.h"
#include "PlayerActions.h"
#include "ModuleAnimation.h"
#include "ResourceMesh.h"

std::string JSONSerialization::SaveSceneBinary()
{
	LOG("SAVING SCENE BINARY -----");
	std::string ret;
	uint buffer_size = 0;
	GetBufferSizeRoot(buffer_size);
	if (App->scene->root->GetNumChilds() > 0)
	{
		for (int i = 0; i < App->scene->root->GetNumChilds(); i++)
		{
			App->scene->root->GetChildbyIndex(i)->GetOwnBufferSize(buffer_size);
		}
	}
	buffer_size += sizeof(int);			//identifier IDENTIFICATOR_END
	char* buffer = new char[buffer_size];
	char* cursor = buffer;


	SaveIntBinary(&cursor, IDENTIFICATOR_ROOT);
	SaveIntBinary(&cursor, App->scene->TagsSize());
	std::vector<std::string>* tag_ptr = App->scene->GetTagsVec();
	uint size = tag_ptr->size();
	for (uint k = 0; k < size; k++)
	{
		SaveStringBinary(&cursor, tag_ptr->at(k));
	}
	SaveStringBinary(&cursor, App->scene->root->GetName());

	if (App->scene->root->GetNumChilds() > 0)
	{
		for (int i = 0; i < App->scene->root->GetNumChilds(); i++)
		{
			SaveChildGameObjectBinary(*App->scene->root->GetChildbyIndex(i), &cursor);
		}
	}


	SaveIntBinary(&cursor, IDENTIFICATOR_END);
	App->fs->SaveFile(buffer, "JoanTest", buffer_size, DIRECTORY_IMPORT::IMPORT_DIRECTORY_ASSETS);
	RELEASE_ARRAY(buffer);
	return ret;
}

void JSONSerialization::SaveChildGameObjectBinary(const GameObject& gameObject, char** cursor)
{
	// Identificator GameObject
	SaveIntBinary(cursor, IDENTIFICATOR_GAMEOBJECT);
	// UUID--------
	SaveIntBinary(cursor, gameObject.GetUUID());
	// Parent UUID------------
	int uuidParent = -1;
	if (gameObject.GetParent() != nullptr)
	{
		uuidParent = gameObject.GetParent()->GetUUID();
	}
	SaveIntBinary(cursor, uuidParent);
	// Name- --------
	SaveStringBinary(cursor, gameObject.GetName());
	// Tag ----------
	SaveStringBinary(cursor, gameObject.GetTag());
	// Bounding Box ---------
	SaveBoolBinary(cursor, gameObject.IsAABBActive());
	// Static ---------
	SaveBoolBinary(cursor, gameObject.IsStatic());

	SaveBoolBinary(cursor, gameObject.AreTranslationsActivateds());
	SaveBoolBinary(cursor, gameObject.AreRotationsActivateds());
	SaveBoolBinary(cursor, gameObject.AreScalesActivateds());

	// Components  ------------
	if (gameObject.GetNumComponents() > 0)
	{
		for (int i = 0; i < gameObject.GetNumComponents(); i++)
		{
			gameObject.GetComponentbyIndex(i)->SaveBinary(cursor, i);
		}
	}
	if (gameObject.GetNumChilds() > 0)
	{
		for (int i = 0; i < gameObject.GetNumChilds(); i++)
		{
			SaveChildGameObjectBinary(*gameObject.GetChildbyIndex(i), cursor);
		}
	}
}

void JSONSerialization::LoadSceneBinary(std::string scene)
{
	char* buffer;
	uint size_file = App->fs->LoadFile(scene.c_str(), &buffer, DIRECTORY_IMPORT::IMPORT_DIRECTORY_ASSETS);
	char* cursor = buffer;

	App->scene->DeleteAllGameObjects(App->scene->root);
	App->scene->ClearAllTags();
	int identificator = LoadIntBinary(&cursor);
	std::vector<LoadSceneSt> gameobjects;
	std::vector<LoadSceneCp> components;
	if (identificator == IDENTIFICATOR_ROOT)
	{
		int number_of_tags = LoadIntBinary(&cursor);
		for (uint i = 0; i < number_of_tags; i++)
		{
			std::string tag = LoadStringBinary(&cursor);
			if (!App->scene->FindTag(tag.c_str()))
			{
				App->scene->AddTag(tag.c_str());
			}
		}
		App->scene->root->SetName(LoadStringBinary(&cursor).c_str());

		// Load GameObject and Components
		int next_identificator = 0;
		while (next_identificator != IDENTIFICATOR_END)
		{
			next_identificator = LoadIntBinary(&cursor);

			switch (next_identificator)
			{
			case IDENTIFICATOR_GAMEOBJECT:
			{
				LoadSceneSt obj = LoadGameObejctBinary(&cursor);
				if (obj.go != nullptr)
				{
					gameobjects.push_back(obj);
				}
				else
				{
					LOG("[error] File seems corrupted");
					identificator = IDENTIFICATOR_ERROR;
					next_identificator = IDENTIFICATOR_END;
				}
				break;
			}
			case IDENTIFICATOR_COMPONENT:
			{
				LoadSceneCp comp = LoadComponentsBinary(&cursor);
				if (comp.comp != nullptr)
				{
					components.push_back(comp);
				}
				else
				{
					LOG("[error] File seems corrupted");
					identificator = IDENTIFICATOR_ERROR;
					next_identificator = IDENTIFICATOR_END;
				}
				break;
			}
			case IDENTIFICATOR_END:
				LOG("[green]%s scene succesfully loaded", scene.c_str());
				identificator = IDENTIFICATOR_END;
				break;
			default:
				LOG("[error]File seems corrupted");
				identificator = IDENTIFICATOR_ERROR;
				next_identificator = IDENTIFICATOR_END;
				break;
			}
		}
	}
	else
	{
		LOG("[error]Error With Load %s", scene.c_str());
	}

	if (identificator == IDENTIFICATOR_END)
	{
		const int num_components = components.size();
		LoadSceneCp* scene_comp = (num_components > 0) ? components.data() : nullptr;

		const int num_objects = gameobjects.size();
		LoadSceneSt* scene_ptr = (num_objects > 0) ? gameobjects.data() : nullptr;

		// Add Components
		for (int i = 0; i < num_components; i++)
		{
			if (scene_comp[i].uid_parent != -1)
			{
				for (int j = 0; j < num_objects; j++)
				{
					if (scene_comp[i].uid_parent == scene_ptr[j].go->GetUUID())
					{
						scene_ptr[j].go->AddComponent(scene_comp[i].comp, scene_comp[i].position_parent);
					}
				}
			}
			else
			{
				LOG("[error] with load a Component!");
			}
		}

		// Now with uid parent add childs.
		for (int i = 0; i < num_objects; i++)
		{
			if (scene_ptr[i].uid_parent != -1)
			{
				for (int j = 0; j < num_objects; j++)
				{
					if (scene_ptr[i].uid_parent == scene_ptr[j].go->GetUUID())
					{
						scene_ptr[j].go->AddChildGameObject(scene_ptr[i].go);
					}
				}
			}
		}
		// Now pass vector to root in scene
		for (int i = 0; i < num_objects; i++)
		{
			if (scene_ptr[i].uid_parent == -1)
			{
				App->scene->root->AddChildGameObject(scene_ptr[i].go);
			}
		}
		//Add static objects to scene
		for (int i = 0; i < num_objects; i++)
		{
			if (scene_ptr[i].go->IsStatic())
			{
				App->scene->octree.Insert(scene_ptr[i].go);
			}
			else
			{
				App->scene->dynamic_objects.push_back(scene_ptr[i].go);
			}
		}
	}
	gameobjects.clear();
	components.clear();
	RELEASE_ARRAY(buffer);
}

LoadSceneSt JSONSerialization::LoadGameObejctBinary(char** cursor)
{
	// UUID--------
	uint uid = LoadIntBinary(cursor);
	// Parent UUID------------
	uint uid_parent = LoadIntBinary(cursor);
	// Name- --------
	std::string nameGameObject = LoadStringBinary(cursor);

	// Create GameObject
	GameObject* obj = new GameObject(nameGameObject.c_str(), uid);
	if (obj == nullptr)
	{
		LoadSceneSt temp;
		temp.go = nullptr;
		temp.uid_parent = 0;
		return temp;
	}

	obj->AddComponent(Comp_Type::C_TRANSFORM);

	// Tag ----------
	std::string tagGameObject = LoadStringBinary(cursor);
	if (App->scene->FindTag(tagGameObject.c_str()))obj->SetTag(tagGameObject.c_str());
	// Bounding Box ---------
	
	bool aabb_active = LoadBoolBinary(cursor);
	obj->SetAABBActive(aabb_active);
	bool static_obj = LoadBoolBinary(cursor);
	obj->SetStatic(static_obj);

	bool anim_translations = LoadBoolBinary(cursor);
	obj->ToggleAnimationTranslations(anim_translations);

	bool anim_rotations = LoadBoolBinary(cursor);
	obj->ToggleAnimationRotation(anim_rotations);

	bool anim_scales = LoadBoolBinary(cursor);
	obj->ToggleAnimationScale(anim_scales);

	LoadSceneSt temp;
	temp.go = obj;
	temp.uid_parent = uid_parent;

	return temp;
}

LoadSceneCp JSONSerialization::LoadComponentsBinary(char** cursor)
{
	// type--------
	Comp_Type type = (Comp_Type)LoadIntBinary(cursor);
	// UID Parent
	uint uid_parent = LoadIntBinary(cursor);
	// Position in Parent
	uint position_parent = LoadIntBinary(cursor);

	Component* comp = new Component(type, nullptr);
	if (comp == nullptr)
	{
		LoadSceneCp temp;
		temp.comp = nullptr;
		return temp;
	}
	comp->LoadBinary(cursor);

	LoadSceneCp temp;
	temp.comp = comp;
	temp.position_parent = position_parent;
	temp.uid_parent = uid_parent;
	return temp;
}

void JSONSerialization::GetBufferSizeRoot(uint &buffer_size)
{
	buffer_size += sizeof(int);			//identifier IDENTIFICATOR_ROOT
	buffer_size += sizeof(int);			//Number of Tags
	std::vector<std::string>* tag_ptr = App->scene->GetTagsVec();
	uint size = tag_ptr->size();
	for (uint k = 0; k < size; k++)
	{
		buffer_size += sizeof(int);
		buffer_size += tag_ptr->at(k).size();
	}

	buffer_size += sizeof(int);			//Name Root
	buffer_size += strlen(App->scene->root->GetName());
}

void JSONSerialization::SaveIntBinary(char** cursor, int value)
{
	uint bytes_to_copy = sizeof(value);
	memcpy((*cursor), &value, bytes_to_copy);
	(*cursor) += bytes_to_copy;
}

void JSONSerialization::SaveFloatBinary(char** cursor, float value)
{
	uint bytes_to_copy = sizeof(value);
	memcpy((*cursor), &value, bytes_to_copy);
	(*cursor) += bytes_to_copy;
}

void JSONSerialization::SaveBoolBinary(char** cursor, bool value)
{
	uint bytes_to_copy = sizeof(value);
	memcpy((*cursor), &value, bytes_to_copy);
	(*cursor) += bytes_to_copy;
}

void JSONSerialization::SaveStringBinary(char** cursor, std::string value)
{
	uint bytes_to_copy = sizeof(int);
	int name_size = value.size();
	memcpy((*cursor), &name_size, bytes_to_copy);
	(*cursor) += bytes_to_copy;
	memcpy((*cursor), value.c_str(), name_size);
	(*cursor) += name_size;

}

void JSONSerialization::SaveConstCharBinary(char** cursor, const char* value)
{
	uint bytes_to_copy = sizeof(int);
	int name_size = strlen(value);
	memcpy((*cursor), &name_size, bytes_to_copy);
	(*cursor) += bytes_to_copy;
}

void JSONSerialization::SaveFloat2Binary(char** cursor, math::float2 value)
{
	uint bytes_to_copy = sizeof(float);
	memcpy((*cursor), &value.x, bytes_to_copy);
	(*cursor) += bytes_to_copy;
	memcpy((*cursor), &value.y, bytes_to_copy);
	(*cursor) += bytes_to_copy;
}

void JSONSerialization::SaveFloat3Binary(char** cursor, math::float3 value)
{
	uint bytes_to_copy = sizeof(float);
	memcpy((*cursor), &value.x, bytes_to_copy);
	(*cursor) += bytes_to_copy;
	memcpy((*cursor), &value.y, bytes_to_copy);
	(*cursor) += bytes_to_copy;
	memcpy((*cursor), &value.z, bytes_to_copy);
	(*cursor) += bytes_to_copy;
}

void JSONSerialization::SaveFloat4Binary(char** cursor, math::float4 value)
{
	uint bytes_to_copy = sizeof(float);
	memcpy((*cursor), &value.x, bytes_to_copy);
	(*cursor) += bytes_to_copy;
	memcpy((*cursor), &value.y, bytes_to_copy);
	(*cursor) += bytes_to_copy;
	memcpy((*cursor), &value.z, bytes_to_copy);
	(*cursor) += bytes_to_copy;
	memcpy((*cursor), &value.w, bytes_to_copy);
	(*cursor) += bytes_to_copy;
}

int JSONSerialization::LoadIntBinary(char ** cursor)
{
	uint bytes_to_copy = sizeof(int);
	int ret = 0;
	memcpy(&ret, (*cursor), bytes_to_copy);
	(*cursor) += bytes_to_copy;
	return ret;
}

float JSONSerialization::LoadFloatBinary(char ** cursor)
{
	uint bytes_to_copy = sizeof(float);
	float ret = 0;
	memcpy(&ret, (*cursor), bytes_to_copy);
	(*cursor) += bytes_to_copy;
	return ret;
}

bool JSONSerialization::LoadBoolBinary(char ** cursor)
{
	uint bytes_to_copy = sizeof(bool);
	bool ret;
	memcpy(&ret, (*cursor), bytes_to_copy);
	(*cursor) += bytes_to_copy;
	return ret;
}

std::string JSONSerialization::LoadStringBinary(char ** cursor)
{
	uint bytes_to_copy = sizeof(int);
	std::string ret;
	int size_string = 0;
	memcpy(&size_string, (*cursor), bytes_to_copy);
	(*cursor) += bytes_to_copy;

	char* c_str = new char[size_string + 1];
	bytes_to_copy = size_string;
	memcpy(c_str, (*cursor), bytes_to_copy);
	(*cursor) += bytes_to_copy;
	c_str[size_string] = 0x00;
	ret = c_str;
	RELEASE_ARRAY(c_str);
	return ret;
}

void JSONSerialization::LoadConstCharBinary(char ** cursor, const char** value)
{
	uint bytes_to_copy = sizeof(int);
	int size_string = 0;
	memcpy(&size_string, (*cursor), bytes_to_copy);
	(*cursor) += bytes_to_copy;

	(*value) = new char[size_string + 1];
	bytes_to_copy = size_string;
	memcpy(value, (*cursor), bytes_to_copy);
	(*cursor) += bytes_to_copy;
	value[size_string] = 0x00;
}

math::float2 JSONSerialization::LoadFloat2Binary(char ** cursor)
{
	uint bytes_to_copy = sizeof(int);
	math::float2 ret;
	int temp = 0;
	memcpy(&temp, (*cursor), bytes_to_copy);
	ret.x = temp;
	(*cursor) += bytes_to_copy;

	memcpy(&temp, (*cursor), bytes_to_copy);
	ret.y = temp;
	(*cursor) += bytes_to_copy;
	return ret;
}

math::float3 JSONSerialization::LoadFloat3Binary(char ** cursor)
{
	uint bytes_to_copy = sizeof(int);
	math::float3 ret;
	int temp = 0;
	memcpy(&temp, (*cursor), bytes_to_copy);
	ret.x = temp;
	(*cursor) += bytes_to_copy;

	memcpy(&temp, (*cursor), bytes_to_copy);
	ret.y = temp;
	(*cursor) += bytes_to_copy;

	memcpy(&temp, (*cursor), bytes_to_copy);
	ret.z = temp;
	(*cursor) += bytes_to_copy;
	return ret;
}

math::float4 JSONSerialization::LoadFloat4Binary(char ** cursor)
{
	uint bytes_to_copy = sizeof(int);
	math::float4 ret;
	int temp = 0;
	memcpy(&temp, (*cursor), bytes_to_copy);
	ret.x = temp;
	(*cursor) += bytes_to_copy;

	memcpy(&temp, (*cursor), bytes_to_copy);
	ret.y = temp;
	(*cursor) += bytes_to_copy;

	memcpy(&temp, (*cursor), bytes_to_copy);
	ret.z = temp;
	(*cursor) += bytes_to_copy;

	memcpy(&temp, (*cursor), bytes_to_copy);
	ret.w = temp;
	(*cursor) += bytes_to_copy;
	return ret;
}
