﻿using CulverinEditor;
using CulverinEditor.Debug;

public class BarrelFall : CulverinBehaviour
{
    BarrelMovement barrel_mov;

    public GameObject barrel_mov_go;
    CompRigidBody rigid_body;
    Vector3 start_pos;
    bool falling = false;
    
    float fall_x_pos = 0;
    float start_x_pos = 0;
    void Start()
    {
        barrel_mov_go = GetLinkedObject("barrel_mov_go");
        barrel_mov = barrel_mov_go.GetComponent<BarrelMovement>();
        rigid_body = gameObject.GetComponent<CompRigidBody>();
        start_pos = gameObject.GetComponent<Transform>().local_position;
        Debug.Log(start_pos.ToString());
        falling = false;
    }

    void Update()
    {
        if (!falling)
        {
            barrel_mov = barrel_mov_go.GetComponent<BarrelMovement>();
            if (barrel_mov.restart)
            {
                //CAL FUNCTION
                // Vector3 pos = gameObject.GetComponent<CompRigidBody>().GetMaxJointPose();
                Quaternion quat = gameObject.GetComponent<CompRigidBody>().GetColliderQuaternion();

                BarrelManage manage = barrel_mov.instance.GetComponent<BarrelManage>();
                Vector3 parent_pos = new Vector3(manage.restart_pos_x, manage.restart_pos_y, manage.restart_pos_z);

                gameObject.GetComponent<CompRigidBody>().MoveKinematic(start_pos * 10 + parent_pos, quat);

                gameObject.GetComponent<CompRigidBody>().ResetForce();
                gameObject.GetComponent<CompRigidBody>().ApplyImpulse(new Vector3(1, 0, 0));

                Debug.Log(start_pos.ToString());
            }

            //TESTING BARRELS FALL
            if (Input.GetInput_KeyDown("RAttack", "Player"))
            {


                gameObject.GetComponent<CompRigidBody>().RemoveJoint();

                Quaternion quat = gameObject.GetComponent<CompRigidBody>().GetColliderQuaternion();
                BarrelManage manage = barrel_mov.instance.GetComponent<BarrelManage>();
                Vector3 parent_pos = new Vector3(manage.restart_pos_x, manage.restart_pos_y, manage.restart_pos_z);

                start_x_pos = gameObject.GetComponent<Transform>().local_position.x + parent_pos.x;
                fall_x_pos = Mathf.Round(start_x_pos) - start_x_pos;
               
                falling = true;
            }

        }
        else
        {
            //ONE WAY TO CONTROL WHERE THE BARRELS FALL
            BarrelManage manage = barrel_mov.instance.GetComponent<BarrelManage>();
            Vector3 parent_pos = new Vector3(manage.restart_pos_x, manage.restart_pos_y, manage.restart_pos_z);
            Vector3 actual_pos = gameObject.GetComponent<Transform>().local_position + parent_pos;

            //ADD THIS X POSITION
            Vector3 pos = new Vector3(actual_pos.x + fall_x_pos / 100, actual_pos.y, actual_pos.z);
         
            Quaternion quat = gameObject.GetComponent<CompRigidBody>().GetColliderQuaternion();
            if ((actual_pos.x - Mathf.Round(actual_pos.x)) > 0.01f)              
            {
                gameObject.GetComponent<CompRigidBody>().ApplyForce(new Vector3(-1, 0, 0));
              
            }
            else if (actual_pos.x - Mathf.Round(actual_pos.x) < -0.01f){
                gameObject.GetComponent<CompRigidBody>().ApplyForce(new Vector3(1, 0, 0));
            }
               
            else
                gameObject.GetComponent<CompRigidBody>().MoveKinematic(actual_pos, quat);

        }
      
      
        
    }

    void OnContact()
    {

        CompRigidBody rbody = GetComponent<CompRigidBody>();

        if (rbody != null)
        {
            rbody.RemoveJoint();

        }
    }
}
