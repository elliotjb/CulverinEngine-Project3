﻿using CulverinEditor;
using CulverinEditor.Debug;

public class ChainManage : CulverinBehaviour
{


    GameObject parent_obj;

    public float movSpeed;
    public float length;
    public float stop_time;
    public float stop_length;

    public float time = 0;
    public float p_dt;
    float initialpos = 0;
    float endPos;

    public bool stop = false;
    void Start()
    {
        endPos = initialpos;
    }
    void Update()
    {

        p_dt = Time.DeltaTime();
        time += p_dt;
        if (endPos >= stop_length)
        {
            stop = true;
            endPos = 0.0f;
            time = 0.0f;
        }


        if (time >= stop_time)
        {
            stop = false;
            endPos += movSpeed * p_dt;
        }


    }

}