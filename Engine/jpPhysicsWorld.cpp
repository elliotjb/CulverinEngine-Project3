#include "jpPhysicsWorld.h"
//#include "Globals.h"
#include "Math.h"
#include "ModulePhysics.h"

//PhysX libraries for debug builds
#ifdef _DEBUG

#pragma comment (lib, "PhysX/lib/vc15win32/PhysX3DEBUG_x86.lib")
#pragma comment (lib, "PhysX/lib/vc15win32/PxFoundationDEBUG_x86.lib")
#pragma comment (lib, "PhysX/lib/vc15win32/PhysX3ExtensionsDEBUG.lib")
#pragma comment (lib, "PhysX/lib/vc15win32/PhysX3CommonDEBUG_x86.lib")
#pragma comment (lib, "PhysX/lib/vc15win32/PxPvdSDKDEBUG_x86.lib")

#endif // _DEBUG

//PhysX libraries for release builds
#ifndef _DEBUG

#pragma comment (lib, "PhysX/lib/vc15win32/PhysX3_x86.lib")
#pragma comment (lib, "PhysX/lib/vc15win32/PxFoundation_x86.lib")
#pragma comment (lib, "PhysX/lib/vc15win32/PhysX3Extensions.lib")
#pragma comment (lib, "PhysX/lib/vc15win32/PhysX3Common_x86.lib")
#pragma comment (lib, "PhysX/lib/vc15win32/PxPvdSDK_x86.lib")

#endif // _DEBUG

jpPhysicsWorld::jpPhysicsWorld()
{	
}

jpPhysicsWorld::jpPhysicsWorld(ModulePhysics * module_callback)
{
	collision_callback = new jpCollisionCallback(module_callback);
}

jpPhysicsWorld::~jpPhysicsWorld()
{
	if (jpWorld) {
		physx::PxScene* scene = nullptr;
		for (unsigned int i = 0; i < jpWorld->getNbScenes(); i++) {
			jpWorld->getScenes(&scene, 1, i);
			scene->release();
			scene = nullptr;
		}
		jpWorld->release();	
		//pvd->release();
		jpFoundation->release();
	}
}

bool jpPhysicsWorld::CreateNewPhysicsWorld()
{
	if (!jpWorld) {
		jpFoundation = PxCreateFoundation(PX_FOUNDATION_VERSION, gDefaultAllocatorCallback,
			gDefaultErrorCallback);

		if (jpFoundation)
		{
			jpWorld = PxCreatePhysics(PX_PHYSICS_VERSION, *jpFoundation, physx::PxTolerancesScale(), true, nullptr);
		}

		if (jpWorld) {
			//jpCooking = PxCreateCooking(PX_PHYSICS_VERSION, *jpFoundation, jpWorld->getTolerancesScale());
			return true;
		}
		else return false;
	}
	else return false;
}

bool jpPhysicsWorld::Simulate(float dt)
{
	if (jpWorld && jpWorld->getNbScenes() > 0) 
	{
		physx::PxScene* scene;
		jpWorld->getScenes(&scene, 1, 0);
		scene->simulate(dt);
		return true;
	}
	return false;
}

bool jpPhysicsWorld::StopSimulation(bool priority)
{
	if (jpWorld && jpWorld->getNbScenes() > 0) 
	{
		physx::PxScene* scene;
		jpWorld->getScenes(&scene, 1, 0);
		return scene->fetchResults(priority);
	}
	return false;
}

// jp Filter Shader -------------------------------
physx::PxFilterFlags jpFilterShader(
	physx::PxFilterObjectAttributes attributes0, physx::PxFilterData filterData0,
	physx::PxFilterObjectAttributes attributes1, physx::PxFilterData filterData1,
	physx::PxPairFlags& pairFlags, const void* constantBlock, physx::PxU32 constantBlockSize)
{
	// report all contacts with triggers
	if (physx::PxFilterObjectIsTrigger(attributes0) || physx::PxFilterObjectIsTrigger(attributes1))
	{
		pairFlags = physx::PxPairFlag::eTRIGGER_DEFAULT;
		return physx::PxFilterFlag::eDEFAULT;
	}
	// generate contacts for all that were not filtered above
	pairFlags = physx::PxPairFlag::eCONTACT_DEFAULT;

	// trigger the contact callback for pairs (A,B) where 
	// the filtermask of A contains the ID of B and vice versa.
	if ((filterData0.word0 & filterData1.word1) && (filterData1.word0 & filterData0.word1))
		pairFlags |= physx::PxPairFlag::eNOTIFY_TOUCH_FOUND;

	return physx::PxFilterFlag::eDEFAULT;
}

// Create Default Scene
physx::PxScene * jpPhysicsWorld::CreateNewScene()
{
	physx::PxSceneDesc sceneDesc(jpWorld->getTolerancesScale());
	sceneDesc.gravity = physx::PxVec3(0.0f, -9.81f, 0.0f);
	sceneDesc.cpuDispatcher = physx::PxDefaultCpuDispatcherCreate(1);
	sceneDesc.filterShader = jpFilterShader;
	sceneDesc.simulationEventCallback = collision_callback;
	sceneDesc.flags |= physx::PxSceneFlag::eENABLE_PCM;

	return jpWorld->createScene(sceneDesc);
}

physx::PxScene * jpPhysicsWorld::GetScene(int scene_index) const
{
	physx::PxScene* ret = nullptr;
	jpWorld->getScenes(&ret, 1, scene_index);
	return ret;
}

physx::PxPhysics * jpPhysicsWorld::GetPhysicsWorld()
{
	return jpWorld;
}

physx::PxCooking * jpPhysicsWorld::GetCooking()
{
	return nullptr;
}

jpPhysicsRigidBody * jpPhysicsWorld::CreateRigidBody(physx::PxScene * curr_scene, bool dynamic)
{
	jpPhysicsRigidBody* new_body = nullptr;
	if (jpWorld) {
		new_body = new jpPhysicsRigidBody(jpWorld, dynamic);
		if (curr_scene != nullptr)
			curr_scene->addActor(*new_body->GetActor());
	}
	
	return new_body;
}

void jpCollisionCallback::onContact(const physx::PxContactPairHeader & pairHeader, const physx::PxContactPair * pairs, physx::PxU32 nbPairs)
{
	if (callback_module)
	{
		callback_module->OnContact(pairHeader.actors[0], pairHeader.actors[1], JP_COLLISION_TYPE::CONTACT_ENTER);
	}
}

void jpCollisionCallback::onTrigger(physx::PxTriggerPair * pairs, physx::PxU32 count)
{
	if (callback_module)
	{
		for (uint i = 0; i < count; i++)
		{
			// Notify first contact -----------
			if (pairs[i].status == physx::PxPairFlag::eNOTIFY_TOUCH_FOUND)
			{
				callback_module->OnTrigger(pairs[i].triggerActor, pairs[i].otherActor, JP_COLLISION_TYPE::TRIGGER_ENTER);
			}

			// Notify lost contact ------------
			if (pairs[i].status == physx::PxPairFlag::eNOTIFY_TOUCH_LOST)
			{
				callback_module->OnTrigger(pairs[i].triggerActor, pairs[i].otherActor, JP_COLLISION_TYPE::TRIGGER_LOST);
			}
		}
	}
}
