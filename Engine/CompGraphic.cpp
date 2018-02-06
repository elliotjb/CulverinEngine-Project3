#include "CompGraphic.h"
#include "Application.h"




CompGraphic::CompGraphic(Comp_Type t, GameObject * parent) :Component(t, parent)
{
	uid = App->random->Int();
	name_component = "Graphic";
}

CompGraphic::CompGraphic(const CompGraphic & copy, GameObject * parent) : Component(copy.GetType(), parent)
{
}

CompGraphic::~CompGraphic()
{
}
