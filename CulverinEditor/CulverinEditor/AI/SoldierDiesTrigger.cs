﻿using CulverinEditor;
using CulverinEditor.Debug;
using CulverinEditor.SceneManagement;
using System;

public class SoldierDiesTrigger : CulverinBehaviour
{
    bool triggered = false;
    
    void OnTriggerEnter()
    {
        if(triggered == false)
        {
            triggered = true;
            GetComponent<CompAudio>().PlayEvent("BossIntro");
        }
    }
}