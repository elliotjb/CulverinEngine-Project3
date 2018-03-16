﻿using CulverinEditor;
using CulverinEditor.Debug;
using CulverinEditor.Pathfinding;
using System.Collections.Generic;

public class Movement_Action : Action
{
    public bool         look_at_player = false;
    public GameObject   map;
    public GameObject   myself;
    public GameObject   mesh;
    public GameObject   player;
    CompAnimation       anim;
    public float        tile_size = 0.0f;
    List<PathNode>      path = null;

    enum Direction
    {
        DIR_NO_DIR,
        DIR_NORTH,
        DIR_EAST,
        DIR_SOUTH,
        DIR_WEST
    }

    Direction dir = Direction.DIR_NO_DIR;

    public float max_vel = 5.0f;
    public float hurt_max_vel = 2.5f;
    float current_max_vel;

    public float max_accel = 2.0f;
    public float hurt_accel = 1.0f;
    float current_max_accel;

    public float max_rot_vel = 2.0f;
    public float hurt_max_rot_vel = 1.0f;
    float current_max_rot_vel;

    public float max_rot_accel = 0.7f;
    public float hurt_max_rot_accel = 0.3f;
    float current_max_rot_accel;

    Vector3 current_velocity = new Vector3(Vector3.Zero);
    Vector3 current_acceleration = new Vector3(Vector3.Zero);
    float current_rot_velocity = 0.0f;
    float current_rot_acceleration = 0.0f;
    float arrive_distance = 0.05f;
    float rot_margin = 0.05f;

    bool rotation_finished = false;
    bool translation_finished = false;

    public bool chase = false;

    public Movement_Action()
    {
        action_type = ACTION_TYPE.MOVE_ACTION;
    }

    public Movement_Action(float speed): base(speed)
    {
        action_type = ACTION_TYPE.MOVE_ACTION;
    }

    void Start()
    {
        look_at_player = false;
        map = GetLinkedObject("map");
        myself = GetLinkedObject("myself");
        player = GetLinkedObject("player");
        anim = GetComponent<CompAnimation>();
        anim.SetTransition("ToPatrol");

        Enemy_BT bt = GetComponent<EnemySword_BT>();
        if(bt == null)
            bt = GetComponent<EnemyShield_BT>();
        if (bt == null)
            bt = GetComponent<EnemySpear_BT>();

        float interpolation = bt.GetCurrentInterpolation();
        current_max_vel = hurt_max_vel + (max_vel - hurt_max_vel) * interpolation;
        current_max_accel = hurt_accel + (max_accel - hurt_accel) * interpolation;
        current_max_rot_vel = hurt_max_rot_vel + (max_rot_vel - hurt_max_rot_vel) * interpolation;
        current_max_rot_accel = hurt_max_rot_accel + (max_rot_accel - hurt_max_rot_accel) * interpolation;

        //GetComponent<CompAnimation>().SetClipsSpeed(bt.anim_speed);

        Vector3 fw = myself.GetComponent<Transform>().GetForwardVector();
        path = new List<PathNode>();
    }

    public override bool ActionStart()
    {
        anim.SetClipsSpeed(anim_speed);
        anim.SetTransition("ToPatrol");

        if (path == null)
        {
            Debug.Log("Move: Path == null");
            return false;
        }
        return true;
    }

    public override ACTION_RESULT ActionUpdate()
    {
        //Movement
        if (translation_finished == false)
        {
            if (current_acceleration.Length > current_max_accel)
            {
                current_acceleration = current_acceleration.Normalized * current_max_accel;
            }
            current_velocity.x = current_velocity.x + current_acceleration.x;
            current_velocity.z = current_velocity.z + current_acceleration.z;

            if (current_velocity.Length > current_max_vel)
            {
                current_velocity = current_velocity.Normalized * current_max_vel;
            }

            //Translate
            Vector3 local_pos = GetComponent<Transform>().local_position;
            float dt = Time.deltaTime;
            local_pos.x = local_pos.x + (current_velocity.x * dt);
            local_pos.z = local_pos.z + (current_velocity.z * dt);
            GetComponent<Transform>().local_position = local_pos;

            //Clean
            current_acceleration = new Vector3(Vector3.Zero);

            if (ReachedTile() == true)
            {
                local_pos.x = path[0].GetTileX() * tile_size;
                local_pos.z = path[0].GetTileY() * tile_size;
                GetComponent<Transform>().local_position = local_pos;

                //Update Collider -> 
                GetComponent<CompCollider>().MoveKinematic(new Vector3(local_pos.x, local_pos.y + 10, local_pos.z));

                if (interupt == true)
                        translation_finished = true;
                else
                    NextTile();
            }
        }

        //Rotation
        if (rotation_finished == false)
        {
            if (Mathf.Abs(current_rot_acceleration) > current_max_rot_accel)
            {
                if (current_rot_acceleration > 0)
                    current_rot_acceleration = current_max_rot_accel;
                else
                    current_rot_acceleration = -current_max_rot_accel;
            }
            current_rot_velocity += current_rot_acceleration;

            if (Mathf.Abs(current_rot_velocity) > current_max_rot_vel)
            {
                if (current_rot_velocity > 0)
                {
                    current_rot_velocity = current_max_rot_vel;
                }
                else current_rot_velocity = -current_max_rot_vel;
            }

            //Rotate
            GetComponent<Transform>().RotateAroundAxis(Vector3.Up, current_rot_velocity);

            //Clean
            current_rot_acceleration = 0.0f;

            if (FinishedRotation() == true)
            {
                rotation_finished = true;
                GetComponent<Align_Steering>().SetEnabled(false);
                Vector3 obj_vec = new Vector3(GetTargetPosition() - GetComponent<Transform>().local_position);
                GetComponent<Transform>().forward = new Vector3(obj_vec.Normalized * GetComponent<Transform>().forward.Length);

                SetDirection();
            }
        }

        if (rotation_finished == true && translation_finished == true)
        {
            GetComponent<CompAnimation>().SetTransition("ToIdle");
            return ACTION_RESULT.AR_SUCCESS;
        }

        return ACTION_RESULT.AR_IN_PROGRESS;
    }

    private void NextTile()
    {
        //Tiles
        if (path.Count == 1)
        {
            GetComponent<Arrive_Steering>().SetEnabled(false);
            GetComponent<Seek_Steering>().SetEnabled(false);
            translation_finished = true;
        }
        else
        {
            path.Remove(path[0]);
            GetComponent<Arrive_Steering>().SetEnabled(true);
            GetComponent<Seek_Steering>().SetEnabled(true);
            translation_finished = false;
        }

        //Rotation
        if (FinishedRotation() == false)
        {
            GetDeltaAngle(true);
            rotation_finished = false;
            Align_Steering steering = GetComponent<Align_Steering>();
            steering.SetEnabled(true);
            steering.SetRotation(GetDeltaAngle());
        }
    }

    public void GoTo(int obj_x, int obj_y, bool rot = false)
    {
        path.Clear();

        int current_x = GetCurrentTileX();
        int current_y = GetCurrentTileY();

        path = map.GetComponent<Pathfinder>().CalculatePath(new PathNode(current_x, current_y), new PathNode(obj_x, obj_y));
        look_at_player = rot;

        if (ReachedTile())
            NextTile();
        else
        {
            translation_finished = false;
            GetComponent<Arrive_Steering>().SetEnabled(true);
            GetComponent<Seek_Steering>().SetEnabled(true);
        }

        if (FinishedRotation() == false)
        {
            rotation_finished = false;
            Align_Steering steering = GetComponent<Align_Steering>();
            steering.SetEnabled(true);
            steering.SetRotation(GetDeltaAngle());
            GetDeltaAngle(true);
        }
        else
        {
            rotation_finished = true;
            GetComponent<Align_Steering>().SetEnabled(false);
        }
    }

    public void GoTo(PathNode obj, bool rot = false)
    {
        GoTo(obj.GetTileX(), obj.GetTileY());
    }

    public void GoToPrevious(int obj_x, int obj_y, bool chase = false)    // Sets a path to the previous tile of your objective // Useful for chasing the player
    {
        this.chase = chase;
        Pathfinder pf = map.GetComponent<Pathfinder>();
        path.Clear();
        List<PathNode> adjacent_walkable_tiles = pf.GetWalkableAdjacents(new PathNode(obj_x, obj_y));
        int current_x = GetCurrentTileX();
        int current_y = GetCurrentTileY();

        if (adjacent_walkable_tiles.Count >= 0)
        {
            PathNode closest = adjacent_walkable_tiles[0];
            int closest_distance = Mathf.Abs(closest.GetTileX() - current_x) + Mathf.Abs(closest.GetTileY() - current_y);

            Debug.Log("Current enemy pos:" + current_x + "," + current_y);
            Debug.Log("Closest:" + closest_distance);
            Debug.Log("Adjacent Tiles");
            foreach (PathNode n in adjacent_walkable_tiles)
                Debug.Log("Tile: " + n.GetTileX() + "," + n.GetTileY());

            if (adjacent_walkable_tiles.Count >= 1)
            {
                for (int i = 1; i < adjacent_walkable_tiles.Count; i++)
                {
                    int x_distance = Mathf.Abs(adjacent_walkable_tiles[i].GetTileX() - current_x);
                    int y_distance = Mathf.Abs(adjacent_walkable_tiles[i].GetTileY() - current_y);

                    int distance = x_distance + y_distance;
                    Debug.Log("Distance:" + distance);

                    if (distance < closest_distance)
                    {
                        closest = adjacent_walkable_tiles[i];
                        closest_distance = distance;
                    }
                }
            }

            GoTo(closest);

            foreach (PathNode n in path)
                Debug.Log("Tile: " + n.GetTileX() + "," + n.GetTileY());
        }
    }

    public void Accelerate(Vector3 acceleration)    
    {
        current_acceleration.x = current_acceleration.x + acceleration.x;
        current_acceleration.z = current_acceleration.z + acceleration.z;
    }

    public void Rotate(float rotation)
    {
        current_rot_acceleration = current_rot_acceleration + rotation;
    }

    public bool ReachedTile()
    {
        Vector3 my_pos = GetComponent<Transform>().local_position;
        Vector3 tile_pos = GetTargetPosition();

        Vector3 result = new Vector3 (Vector3.Zero);
        result.x = tile_pos.x - my_pos.x;
        result.z = tile_pos.z - my_pos.z;

        float distance_to_target = result.Length;

        bool test = (distance_to_target < arrive_distance);

        return (distance_to_target < arrive_distance);
    }

    public Vector3 GetTargetPosition()
    {
        Vector3 result = new Vector3(Vector3.Zero);
        if (path.Count > 0)
        {
            result.x = path[0].GetTileX() * tile_size;
            result.y = GetComponent<Transform>().local_position.y;
            result.z = path[0].GetTileY() * tile_size;
        }
        else
        {
            result = GetComponent<Transform>().local_position;
            Debug.Log("GetTargetPosition: Path has no values");
        }

        return result;
    }

    /*void SetState()
    {
        if (!NextToPlayer())
        {
            if (path.Count > 0)
            {
                if (!FinishedRotation())
                {
                    if (GetDeltaAngle() < 0)
                    {
                        anim = GetComponent<CompAnimation>();

                        if (chase)
                        {
                            Debug.Log("Right Turn Chase mudafukas");
                            anim.SetTransition("ToRightChase");
                        }                            
                        else
                        {
                            Debug.Log("Right Turn mudafukas");
                            anim.SetTransition("ToRight");
                        }

                        anim.SetClipsSpeed(anim_speed);
                    }
                    else
                    {
                        anim = GetComponent<CompAnimation>();

                        if (chase)
                        {
                            Debug.Log("Left Turn Chase mudafukas");
                            anim.SetTransition("ToLeftChase");
                        }
                        else
                        {
                            Debug.Log("Left Turn mudafukas");
                            anim.SetTransition("ToLeft");
                        }

                        anim.SetClipsSpeed(anim_speed);
                    }

                    state = Motion_State.MS_ROTATE;
                    GetComponent<Align_Steering>().SetEnabled(true);
                    Debug.Log("State: ROTATE");
                    return;
                }
                if (!ReachedTile())
                {
                    anim = GetComponent<CompAnimation>();

                    if (chase == true)
                        anim.SetTransition("ToChase");
                    else
                        anim.SetTransition("ToPatrol");

                    anim.SetClipsSpeed(anim_speed);

                    state = Motion_State.MS_MOVE;
                    GetComponent<Arrive_Steering>().SetEnabled(true);
                    GetComponent<Seek_Steering>().SetEnabled(true);
                    Debug.Log("State: MOVE");
                    return;
                }
            }
        }
        Debug.Log("State: NO_STATE");
        state = Motion_State.MS_NO_STATE;
    }*/

    public float GetDeltaAngle(bool db = false)
    {
        if (look_at_player == false)
        {
            Vector3 forward = new Vector3(myself.GetComponent<Transform>().GetForwardVector());
            Vector3 pos = new Vector3(myself.GetComponent<Transform>().GetPosition());
            Vector3 target_pos = new Vector3(GetTargetPosition());
            Vector3 obj_vec = new Vector3(target_pos - pos);

            if(pos == target_pos)
            {
                Debug.Log("Target and Current positions are equal");
                return 0.0f;
            }

            float delta = Vector3.AngleBetweenXZ(forward, obj_vec);

            if (delta > Mathf.PI)
                delta = delta - 2 * Mathf.PI;
            if (delta < (-Mathf.PI))
                delta = delta + 2 * Mathf.PI;

            if (db)
            {
                Debug.Log("Forward: " + forward);
                Debug.Log("Position: " + pos);
                Debug.Log("Target Position: " + GetTargetPosition());
                Debug.Log("Objective Vector: " + obj_vec);
                Debug.Log("Delta Angle: " + delta);
            }

            return delta;
        }
        else
        {
            Vector3 forward = new Vector3(myself.GetComponent<Transform>().GetForwardVector());
            Vector3 pos = new Vector3(myself.GetComponent<Transform>().GetPosition());
            Vector3 player_pos = new Vector3(player.GetComponent<Transform>().GetGlobalPosition());
            Vector3 obj_vec = new Vector3(player_pos.x - pos.x, pos.y, player_pos.z - pos.z);

            float delta = Vector3.AngleBetweenXZ(forward, obj_vec);

            if (delta > Mathf.PI)
                delta = delta - 2 * Mathf.PI;
            if (delta < (-Mathf.PI))
                delta = delta + 2 * Mathf.PI;

            Direction obj_dir = Direction.DIR_NO_DIR;

            if (delta <= (Mathf.PI / 4) && delta >= -(Mathf.PI / 4))
                obj_dir = Direction.DIR_SOUTH;
            else if (delta >= (Mathf.PI / 4) && delta <= 3 * (Mathf.PI / 4))
                obj_dir = Direction.DIR_EAST;
            else if (delta >= (3 * (Mathf.PI / 4)) || delta <= -(3 * (Mathf.PI / 4)))
                obj_dir = Direction.DIR_NORTH;
            else if (delta <= -(Mathf.PI / 4) && delta >= -(3 * (Mathf.PI / 4)))
                obj_dir = Direction.DIR_WEST;

            float obj_angle = 0.0f;

            if(obj_dir != dir)
            {
                switch (dir)
                {
                    case Direction.DIR_NORTH:
                        switch (obj_dir)
                        {
                            case Direction.DIR_EAST: obj_angle = -(Mathf.PI / 2); break;
                            case Direction.DIR_WEST: obj_angle = (Mathf.PI / 2); break;
                            case Direction.DIR_SOUTH: obj_angle = Mathf.PI; break;
                        }
                    break;
                    case Direction.DIR_EAST:
                        switch (obj_dir)
                        {
                            case Direction.DIR_NORTH: obj_angle = (Mathf.PI / 2); break;
                            case Direction.DIR_WEST: obj_angle = Mathf.PI; break;
                            case Direction.DIR_SOUTH: obj_angle = -(Mathf.PI / 2); break;
                        }
                        break;
                    case Direction.DIR_SOUTH:
                        switch (obj_dir)
                        {
                            case Direction.DIR_EAST: obj_angle = (Mathf.PI / 2); break;
                            case Direction.DIR_WEST: obj_angle = -(Mathf.PI / 2); break;
                            case Direction.DIR_NORTH: obj_angle = Mathf.PI; break;
                        }
                        break;
                    case Direction.DIR_WEST:
                        switch (obj_dir)
                        {
                            case Direction.DIR_EAST: obj_angle = Mathf.PI; break;
                            case Direction.DIR_NORTH: obj_angle = -(Mathf.PI / 2); break;
                            case Direction.DIR_SOUTH: obj_angle = (Mathf.PI / 2); break;
                        }
                        break;
                }

                Debug.Log("Direction:");
                switch (dir)
                {
                    case Direction.DIR_WEST: Debug.Log("West"); break;
                    case Direction.DIR_EAST: Debug.Log("East"); break;
                    case Direction.DIR_NORTH: Debug.Log("North"); break;
                    case Direction.DIR_SOUTH: Debug.Log("South"); break;
                }
                float test = obj_angle - delta;
                Debug.Log(test);
                return test;
            }
            Debug.Log("Retards 4 the win");
            return 0.0f;
        }
    }

    public void SetDirection()
    {
        Vector3 forward = new Vector3(myself.GetComponent<Transform>().GetForwardVector());
        float delta = Mathf.Atan2(forward.x, forward.y);

        if (delta > Mathf.PI)
            delta = delta - 2 * Mathf.PI;
        if (delta < (-Mathf.PI))
            delta = delta + 2 * Mathf.PI;

        if (delta <= (Mathf.PI / 4) && delta >= -(Mathf.PI / 4))
            dir = Direction.DIR_SOUTH;
        else if (delta >= (Mathf.PI / 4) && delta <= 3 * (Mathf.PI / 4))
            dir = Direction.DIR_EAST;
        else if (delta >= (3 * (Mathf.PI / 4)) || delta <= -(3 * (Mathf.PI / 4)))
            dir = Direction.DIR_NORTH;
        else if (delta <= -(Mathf.PI / 4) && delta >= -(3 * (Mathf.PI / 4)))
            dir = Direction.DIR_WEST;
    }

    public bool FinishedRotation()
    {
        float delta_angle = GetDeltaAngle();
        return Mathf.Abs(delta_angle) < rot_margin;
    }

    public int GetCurrentTileX()
    {
        return ((int)myself.GetComponent<Transform>().local_position.x / (int)tile_size);
    }

    public int GetCurrentTileY()
    {
        return ((int)myself.GetComponent<Transform>().local_position.z / (int)tile_size);
    }

    public Vector3 GetCurrentVelocity()
    {
        return current_velocity;
    }
    
    public float GetCurrentRotVelocity()
    {
        return current_rot_velocity;
    }

    public float GetMaxAcceleration()
    {
        return max_accel;
    }
    
    public float GetDistanceToTarget()
    {
        return ((GetComponent<Transform>().local_position) - (GetTargetPosition())).Length;
    }

    public void SetCurrentVelocity(Vector3 vel)
    {
        current_velocity = vel;
    }

    public Vector3 GetCurrentAcceleration()
    {
        return current_acceleration;
    }

    public float GetMaxVelocity()
    {
        return max_vel;
    }

    public float GetMaxRotAcceleration()
    {
        return max_rot_accel;
    }

    public float GetRotMargin()
    {
        return rot_margin;
    }

    public bool NextToPlayer()
    {
        player.GetComponent<MovementController>().GetPlayerPos(out int x, out int y);

        switch (dir)
        {
            case Direction.DIR_WEST: return (y == GetCurrentTileY() && x == GetCurrentTileX() - 1);
            case Direction.DIR_EAST: return (y == GetCurrentTileY() && x == GetCurrentTileX() + 1);
            case Direction.DIR_NORTH: return (y == GetCurrentTileY() - 1 && x == GetCurrentTileX());
            case Direction.DIR_SOUTH: return (y == GetCurrentTileY() + 1 && x == GetCurrentTileX());
        }
        return false;
    }

    public void Chase()
    {
        chase = true;
    }

    public void NotChase()
    {
        chase = false;
    }
}