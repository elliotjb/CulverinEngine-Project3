﻿using CulverinEditor;
using CulverinEditor.Debug;

public class TheonCD_Left : CoolDown
{
    public override void OnClick()
    {
        if (GetLinkedObject("theon_obj").GetComponent<CharacterController>().GetState() == 0)
        {
            if (in_cd == false)
            {
                if (GetLinkedObject("theon_obj").GetComponent<TheonController>().OnLeftClick() == true)
                {
                    ActivateAbility();

                    // Set Attacking State
                }
            }
        }
    }

    public override void ActivateAbility()
    {
        //this_obj.GetComponent
        button_cd = GetComponent<CompButton>();
        button_cd.Deactivate();

        Debug.Log("Theon Left CD Clicked");
        act_time = 0.0f;
        in_cd = true;
    }
}