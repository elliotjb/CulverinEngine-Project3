#include <vector>
#include "Math\float4x4.h"
#include "Component.h"

class ResourceMesh;

class CompBone : public Component
{
public:
	CompBone(Comp_Type t, GameObject* parent);
	CompBone(const CompBone& copy, GameObject* parent);
	~CompBone();

	bool Enable();
	bool Disable();
	void Init();
	void PreUpdate(float dt);
	void Update(float dt);
	void Draw();
	void Clear();
	void ShowOptions();
	void ShowInspectorInfo();
	void CopyValues(const CompBone* comp);

	// SAVE - LOAD METHODS ----------------
	void Save(JSON_Object* object, std::string name, bool saveScene, uint& countResources) const;
	void Load(const JSON_Object* object, std::string name);
	void Link();
	void SyncComponent(GameObject* sync_parent);

	void GetOwnBufferSize(uint& buffer_size);
	void SaveBinary(char** cursor, int position) const;
	void LoadBinary(char** cursor);
	// -------------------------------------

	void GenSkinningMatrix(const float4x4& parent_transform);
	const float4x4& GetSkinningMatrix();

	uint uuid_resource_reimported = 0;

	struct Weight
	{
		float weight;
		uint vertex_id;		
	};

	float4x4 offset;
	float4x4 skinning_matrix;
	std::vector<Weight> weights;
	std::vector<CompBone*> child_bones;
	ResourceMesh* resource_mesh = nullptr;
	std::vector<uint> child_uids;
};

