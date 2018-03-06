﻿using CulverinEditor;
using CulverinEditor.Debug;

public class ChainMovement : CulverinBehaviour
{

    public GameObject c_instance;
    ChainManage manage;
    public GameObject barrel = null;

    float length;
 
    bool test = false;


    void Start()
    {
        c_instance = GetLinkedObject("c_instance");
        manage = c_instance.GetComponent<ChainManage>();
        //trans = gameObject.GetComponent<Transform>();
        barrel = null;

    }
    void Update()
    {
        c_instance = GetLinkedObject("c_instance");
        manage = c_instance.GetComponent<ChainManage>();
        

        Vector3 diff = gameObject.GetComponent<Transform>().GetPosition() - new Vector3(0, 0, 0);
        if (diff.Length >= manage.length)
        {
            gameObject.GetComponent<Transform>().SetPosition(new Vector3(0, 0, 0));


        }
        if (!manage.stop)
        {
            gameObject.GetComponent<Transform>().SetPosition(new Vector3(gameObject.GetComponent<Transform>().local_position.x + manage.movSpeed * manage.p_dt, gameObject.GetComponent<Transform>().local_position.y, gameObject.GetComponent<Transform>().local_position.z));

        }

        else
        {
            gameObject.GetComponent<Transform>().SetPosition(new Vector3(Mathf.Round(gameObject.GetComponent<Transform>().local_position.x), gameObject.GetComponent<Transform>().local_position.y, gameObject.GetComponent<Transform>().local_position.z));

        }

    }

    uint lfsr = 0xACE1u;
    uint bit;

    uint rand()
    {
        bit = ((lfsr >> 0) ^ (lfsr >> 2) ^ (lfsr >> 3) ^ (lfsr >> 5)) & 1;
        return lfsr = (lfsr >> 1) | (bit << 15);
    }
}