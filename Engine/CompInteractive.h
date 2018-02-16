#ifndef COMPONENT_INTERACTIVE_H
#define COMPONENT_INTERACTIVE_H
#include "Component.h"
#include <list>
#include "Math\float4.h"
#include "Math\float2.h"

union Event;
class CompGraphic;
class CompImage;
class ResourceMaterial;
enum NavigationMode
{
	NAVIGATION_NONE = -1,
	NAVIGATION_EXTRICTE
};
class CompInteractive:public Component
{
public:
	CompInteractive(Comp_Type t, GameObject* parent);
	CompInteractive(const CompInteractive& copy, GameObject* parent);

	~CompInteractive();


	void PreUpdate(float dt);
	void Update(float dt);
	void ShowOptions();
	void CopyValues(const CompInteractive * component);
	void Save(JSON_Object * object, std::string name, bool saveScene, uint & countResources) const;
	void Load(const JSON_Object * object, std::string name);

	virtual bool IsActive()const;
	void Desactive();
	virtual void ForceClear(Event event_input);

	virtual void OnPointDown(Event event_input);
	virtual void OnPointUP(Event event_input);
	virtual void OnPointEnter(Event event_input);
	virtual void OnPointExit(Event event_input);
	virtual void OnInteractiveSelected(Event event_input);
	virtual void OnInteractiveUnSelected(Event event_input);
	bool PointerInside(float2 position);
	void SetTargetGraphic(CompGraphic* target_graphic);
	//Setters Color tint parameters
	void SetNormalColor(const float4& set_rgba);
	void SetNormalColor(float set_r, float set_g, float set_b, float set_a);
	void SetHighlightedColor(const float4& set_rgba);
	void SetHighlightedColor(float set_r, float set_g, float set_b, float set_a);
	void SetPressedColor(const float4& set_rgba);
	void SetPressedColor(float set_r, float set_g, float set_b, float set_a);
	void SetDisabledColor(const float4& set_rgba);
	void SetDisabledColor(float set_r, float set_g, float set_b, float set_a);
	//Getters Sprite Swap  parameters
	void SetHighlightedSprite(ResourceMaterial* set_sprite);
	void SetPressedSprite(ResourceMaterial* set_sprite);
	void SetDisabledSprite(ResourceMaterial* set_sprite);
	//Getters Color tint parameters
	float4 GetNormalColor()const;
	float4 GetHighlightedColor()const;
	float4 GetPressedColor()const;
	float4 GetDisabledColor()const;
	//Getters Sprite Swap  parameters
	ResourceMaterial* GetHighligtedSprite()const;
	ResourceMaterial* GetPressedSprite()const;
	ResourceMaterial* GetDisabledSprite()const;
	//OtherGetters
	NavigationMode GetNavigationMode()const;
protected:
	virtual bool IsPressed();
	virtual bool IsHighlighted(Event event_data);
	virtual void UpdateSelectionState(Event event_data);
private:
	void HandleTransition();

	void StartTransitionColor(float4 color_to_change, bool no_fade);
	void UpdateTransitionColor(float dt);
	void StartTransitionSprite(ResourceMaterial* sprite_to_change);
public:

	enum SelectionStates
	{
		STATE_NONE =-1,
		STATE_NORMAL,
		STATE_HIGHLIGHTED,
		STATE_PRESSED,
		STATE_DISABLED,
		STATE_MAX
	};
	enum Transition
	{
		TRANSITION_NONE,
		TRANSITION_COLOR,
		TRANSITION_SPRITE,
		TRANSITION_ANIMATION,
		TRANSITION_MAX
	};

private:
	static std::list<CompInteractive*> iteractive_list;
	
	SelectionStates current_selection_state = STATE_NORMAL;
	Transition current_transition_mode = TRANSITION_NONE;
	NavigationMode current_navigation_mode = NAVIGATION_NONE;
	bool disabled = false;
	bool point_down = false;
	bool point_inside = false;
	bool interactive_selected = false;
protected:
	//Color tint parameters
	float4 normal_color;
	float4 highlighted_color;
	float4 pressed_color;
	float4 disabled_color;
	float4 desired_color;
	float color_multiply = 1.0f;
	float fade_duration = 0.1f;
	bool no_fade = false;
	bool start_transition = false;

	//Sprite Swap parameters
	ResourceMaterial* sprite[3];
	uint uuid_reimported_sprite[3];
	CompGraphic* target_graphic = nullptr;
	CompImage* image = nullptr;

private:
	

};
#endif // !COMPONENT_INTERACTIVE_H



