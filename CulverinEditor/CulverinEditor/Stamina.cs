﻿using CulverinEditor;
using CulverinEditor.Debug;

public class Stamina2 : CulverinBehaviour
{
    CompImage stamina_bar;
    float regen = 0.0005f;
    float max_stamina = 1.0f;
    float actual_stamina = 1.0f;
    float stamina_cost = 0.3f;

    void Start()
    {
        stamina_bar = GetComponent<CompImage>();
    }

    void Update()
    {
        Debug.Log("ESTIC AL UPDATE");
        if (actual_stamina < max_stamina) 
        {
            actual_stamina += regen;
        }
        if (Input.GetKeyDown(KeyCode.Num1))
        {
            DecreaseStamina(stamina_cost);
            Debug.Log("stamina");
        }
        stamina_bar.FillAmount(actual_stamina);
    }

    public void DecreaseStamina(float cost)
    {
        if (actual_stamina > cost)
        {
            actual_stamina -= stamina_cost;
        }

        stamina_bar.FillAmount(actual_stamina);
    }

    public float GetCurrentStamina()
    {
        return actual_stamina;
    }
}