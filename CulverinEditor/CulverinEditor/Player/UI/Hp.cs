﻿using CulverinEditor;
using CulverinEditor.Debug;

public class Hp : CulverinBehaviour
{
    public GameObject this_obj_hp;
    public GameObject other_obj_hp;
    CompImage hp_bar;
    public float max_hp = 100.0f;
    float current_hp = 100.0f;
    float calc_hp = 1.0f;

    void Start()
    {
        current_hp = 100.0f;
        this_obj_hp = GetLinkedObject("this_obj_hp");
        other_obj_hp = GetLinkedObject("other_obj_hp");
    }

    void Update()
    {
    }

    public void GetDamage(float dmg)
    {
        if (other_obj_hp.GetComponent<LeftHp>().current_lasthp < current_hp)
        {
            other_obj_hp.GetComponent<LeftHp>().lasthp_value = current_hp;
            other_obj_hp.GetComponent<LeftHp>().hp_bar_changed = false;
        }
        current_hp -= dmg;
        if(current_hp < 0)
        {
            current_hp = 0;
        }

        calc_hp = current_hp / max_hp;
        hp_bar = this_obj_hp.GetComponent<CompImage>();
        hp_bar.FillAmount(calc_hp);
     
    }

    public float GetCurrentHealth()
    {
        return current_hp;
    }

    public void SetHP(float curr_health, float max_health)
    {
        current_hp = curr_health;
        max_hp = max_health;

        calc_hp = current_hp / max_hp;
        hp_bar = this_obj_hp.GetComponent<CompImage>();
        hp_bar.FillAmount(calc_hp);
    }
}