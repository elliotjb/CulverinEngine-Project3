﻿using CulverinEditor;
using CulverinEditor.Debug;
using CulverinEditor.Pathfinding;
using System.Collections.Generic;

public class Movement_Action : Action
{
    public GameObject map;
    public GameObject player;
    public float tile_size = 0.0f;
    List<PathNode> path = null;

    Align_Steering align;
    Arrive_Steering arrive;
    Seek_Steering seek;

    public PathNode tile = null;

    public enum Direction
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

    public float max_rot_vel = Mathf.PI;
    public float hurt_max_rot_vel = 1.0f;
    float current_max_rot_vel;

    public float max_rot_accel = 0.05f;
    public float hurt_max_rot_accel = 0.01f;
    float current_max_rot_accel;

    Vector3 current_velocity = new Vector3(Vector3.Zero);
    Vector3 current_acceleration = new Vector3(Vector3.Zero);
    float current_rot_velocity = 0.0f;
    float current_rot_acceleration = 0.0f;

    bool rotation_finished = false;
    bool translation_finished = false;

    public bool chase = false;

    float wait_timer = 0.0f;
    public float wait_time = 1.0f;

    Vector3 look_at_pos = new Vector3(Vector3.Zero);

    public Movement_Action()
    {
        action_type = ACTION_TYPE.MOVE_ACTION;
    }

    void Start()
    {
        map = GetLinkedObject("map");
        player = GetLinkedObject("player");

        align = GetComponent<Align_Steering>();
        arrive = GetComponent<Arrive_Steering>();
        seek = GetComponent<Seek_Steering>();

        BT bt = GetComponent<EnemySword_BT>();
        if (bt == null)
            bt = GetComponent<EnemyShield_BT>();
        if (bt == null)
            bt = GetComponent<EnemySpear_BT>();
        if (bt == null)
            bt = GetComponent<Boss_BT>();

        float interpolation = bt.GetCurrentInterpolation();
        current_max_vel = hurt_max_vel + (max_vel - hurt_max_vel) * interpolation;
        current_max_accel = hurt_accel + (max_accel - hurt_accel) * interpolation;
        current_max_rot_vel = hurt_max_rot_vel + (max_rot_vel - hurt_max_rot_vel) * interpolation;
        current_max_rot_accel = hurt_max_rot_accel + (max_rot_accel - hurt_max_rot_accel) * interpolation;

        Vector3 fw = GetComponent<Transform>().GetForwardVector();
        path = new List<PathNode>();

        //Set Occupied tile in Pathfinder
        tile = new PathNode(0, 0);
        tile.SetCoords((int)(GetComponent<Transform>().position.x / tile_size + Mathf.Epsilon), (int)(GetComponent<Transform>().position.z / tile_size + Mathf.Epsilon));
        map.GetComponent<Pathfinder>().UpdateOccupiedTiles(gameObject.GetName(), tile);

        //Set direction
        SetDirection();
    }

    public override bool ActionStart()
    {
        if (chase == false)
        {
            GetComponent<CompAnimation>().SetTransition("ToPatrol");
            GetComponent<CompAnimation>().SetFirstActiveBlendingClip("Idle");
        }
        else
        {
            GetComponent<CompAnimation>().SetTransition("ToChase");
            GetComponent<CompAnimation>().SetFirstActiveBlendingClip("IdleAttack");
        }

        if (path.Count != 0)
        {
            tile = new PathNode(0, 0);
            tile.SetCoords(path[0].GetTileX(), path[0].GetTileY());
        }

        return true;
    }

    public override ACTION_RESULT ActionUpdate()
    {
        //Movement
        if (translation_finished == false)
        {
            UpdateTranslation();

            if (arrive.ReachedTile() == true)
            {
                current_velocity = new Vector3(Vector3.Zero);

                if (interupt != true)
                {
                    if (wait_timer == 0.0f)
                    {
                        arrive.SetEnabled(false);
                        seek.SetEnabled(false);
                    }

                    wait_timer += Time.deltaTime;

                    if (wait_timer >= wait_time)
                    {
                        NextTile();
                        wait_timer = 0.0f;
                    }
                }
                else
                {
                    arrive.SetEnabled(false);
                    seek.SetEnabled(false);
                    translation_finished = true;
                    if (chase == true)
                        LookAtPlayer();
                }
            }
        }

        //Rotation
        if (rotation_finished == false)
        {
            UpdateRotation();

            if (GetComponent<Align_Steering>().RotationFinished() == true)
            {
                rotation_finished = true;
                current_rot_velocity = 0.0f;
                align.SetEnabled(false);
                look_at_pos.y = GetComponent<Transform>().position.y;
                Vector3 obj_vec = new Vector3(look_at_pos - GetComponent<Transform>().position);
                Debug.Log("Looking At: " + look_at_pos);
                GetComponent<Transform>().forward = new Vector3(obj_vec.Normalized * GetComponent<Transform>().forward.Length);
                SetDirection();
            }
        }

        if (rotation_finished == true && translation_finished == true)
        {
            if (interupt == true)
                return ACTION_RESULT.AR_FAIL;
            return ACTION_RESULT.AR_SUCCESS;
        }

        return ACTION_RESULT.AR_IN_PROGRESS;
    }

    public override bool ActionEnd()
    {
        if (chase == false)
            GetComponent<CompAnimation>().SetTransition("ToIdle");
        else
            GetComponent<CompAnimation>().SetTransition("ToIdleAttack");

        if (chase != false)
            chase = false;

        if (interupt == true)
            interupt = false;

        return false;
    }

    private void UpdateRotation()
    {
        if (Mathf.Abs(current_rot_acceleration) > current_max_rot_accel)
        {
            if (current_rot_acceleration > 0)
            {
                current_rot_acceleration = current_max_rot_accel;
            }
            else
            {
                current_rot_acceleration = -current_max_rot_accel;
            }
        }
        current_rot_velocity += current_rot_acceleration;

        if (Mathf.Abs(current_rot_velocity) > current_max_rot_vel)
        {
            if (current_rot_velocity > 0)
            {
                current_rot_velocity = current_max_rot_vel;
            }
            else
            {
                current_rot_velocity = -current_max_rot_vel;
            }
        }

        //Rotate
        align.UpdateRotation(current_rot_velocity * Time.deltaTime);

        //Clean
        current_rot_acceleration = 0.0f;
    }

    private void UpdateTranslation()
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

        float point_in_speed = current_velocity.Length / current_max_vel;
        GetComponent<CompAnimation>().SetFirstActiveBlendingClipWeight((1.0f - point_in_speed) * (1.0f - point_in_speed));

        //Translate
        Vector3 pos = new Vector3(GetComponent<Transform>().position);
        pos.x = pos.x + current_velocity.x * Time.deltaTime;
        pos.z = pos.z + current_velocity.z * Time.deltaTime;
        GetComponent<Transform>().position = pos;

        //Clean
        current_acceleration = new Vector3(Vector3.Zero);
    }

    private void NextTile()
    {
        Debug.Log("[yellow] Next Tile");
        Vector3 pos = new Vector3(GetComponent<Transform>().position);
        if (path.Count > 0)
        {
            pos.x = path[0].GetTileX() * tile_size;
            pos.z = path[0].GetTileY() * tile_size;
            GetComponent<Transform>().position = pos;

            //Tiles
            if (path.Count == 1)
            {
                arrive.SetEnabled(false);
                seek.SetEnabled(false);
                translation_finished = true;

                if (chase == true)
                {
                    if (LookingAtPlayer() == false)
                        LookAtPlayer();
                    else
                    {
                        rotation_finished = true;
                        MoveSideways(path[0]);
                    }
                }

                tile.SetCoords(path[0].GetTileX(), path[0].GetTileY());
            }
            else
            {
                arrive.SetEnabled(true);
                seek.SetEnabled(true);
                path.Remove(path[0]);

                if (chase == true)
                {
                    Debug.Log("[pink]chase = true");
                    if (LookingAtPlayer() == true)
                    {
                        rotation_finished = true;
                        MoveSideways(path[0]);
                        Debug.Log("[green]MoveSideways");
                    }
                    else
                        LookAtPlayer();
                }
                else
                {
                    LookAtNextTile();
                    Debug.Log("[blue]LookAtNextTile");
                }

                tile.SetCoords(path[0].GetTileX(), path[0].GetTileY());
            }
        }
    }

    public void GoTo(int obj_x, int obj_y)
    {
        Debug.Log("Go To");
        int current_x = GetCurrentTileX();
        int current_y = GetCurrentTileY();

        if ((obj_x == current_x && obj_y == current_y) || map.GetComponent<Pathfinder>().IsWalkableTile((uint)obj_x, (uint)obj_y) == false)
        {
            translation_finished = true;
            arrive.SetEnabled(false);
            seek.SetEnabled(false);
            if (chase == true)
            {
                Debug.Log("LookingAtPlayer: " + LookingAtPlayer());
                if (LookingAtPlayer() == false)
                {
                    Debug.Log("LookAtPlayer");
                    LookAtPlayer();
                }
                else
                    rotation_finished = true;
            }
        }
        else
        {
            path.Clear();

            arrive.SetEnabled(true);
            seek.SetEnabled(true);

            translation_finished = false;

            path = map.GetComponent<Pathfinder>().CalculatePath(new PathNode(current_x, current_y), new PathNode(obj_x, obj_y));

            if (path.Count > 1)
            {
                if (arrive.ReachedTile())
                    NextTile();
            }
            else
            {
                if (arrive.ReachedTile())
                {
                    translation_finished = true;
                    arrive.SetEnabled(false);
                    seek.SetEnabled(false);
                }

                if (chase == true)
                {
                    Debug.Log("LookingAtPlayer: " + LookingAtPlayer());
                    if (LookingAtPlayer() == false)
                    {
                        Debug.Log("LookAtPlayer");
                        LookAtPlayer();
                    }
                    else
                        rotation_finished = true;
                }
            }
        }
    }

    public void GoTo(PathNode obj, bool rot = false)
    {
        GoTo(obj.GetTileX(), obj.GetTileY());
    }

    private void GoToPrevious(int obj_x, int obj_y, uint range, bool chase = false)    // Sets a path to the previous tile of your objective // Useful for chasing the player
    {
        this.chase = chase;
        Pathfinder pf = map.GetComponent<Pathfinder>();
        path.Clear();
        List<PathNode> adjacent_walkable_tiles = pf.GetWalkableAdjacents(new PathNode(obj_x, obj_y), range);
        int current_x = GetCurrentTileX();
        int current_y = GetCurrentTileY();

        if (adjacent_walkable_tiles.Count >= 0)
        {
            PathNode closest = adjacent_walkable_tiles[0];
            int closest_distance = Mathf.Abs(closest.GetTileX() - current_x) + Mathf.Abs(closest.GetTileY() - current_y);

            if (adjacent_walkable_tiles.Count > 1)
            {
                for (int i = 1; i < adjacent_walkable_tiles.Count; i++)
                {
                    int x_distance = Mathf.Abs(adjacent_walkable_tiles[i].GetTileX() - current_x);
                    int y_distance = Mathf.Abs(adjacent_walkable_tiles[i].GetTileY() - current_y);

                    int distance = x_distance + y_distance;

                    if (distance < closest_distance)
                    {
                        closest = adjacent_walkable_tiles[i];
                        closest_distance = distance;
                    }
                }
            }

            GoTo(closest);
        }
    }

    public void GoToPlayer(uint range)
    {
        player.GetComponent<MovementController>().GetPlayerPos(out int x, out int y);
        GoToPrevious(x, y, range, true);
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

    public Vector3 GetTargetPosition()
    {
        Vector3 result = new Vector3(Vector3.Zero);
        if (path.Count > 0)
        {
            result.x = path[0].GetTileX() * tile_size;
            result.y = GetComponent<Transform>().position.y;
            result.z = path[0].GetTileY() * tile_size;
        }
        else
        {
            result = GetComponent<Transform>().position;
        }

        return result;
    }

    public void LookAt(Vector3 target_position)
    {
        Vector3 forward = new Vector3(GetComponent<Transform>().GetForwardVector());
        Vector3 pos = new Vector3(GetComponent<Transform>().position);
        Vector3 obj_vec = new Vector3(target_position - pos);

        float delta = Vector3.AngleBetweenXZ(forward, obj_vec);

        if (delta > Mathf.PI)
            delta = delta - 2 * Mathf.PI;
        if (delta < (-Mathf.PI))
            delta = delta + 2 * Mathf.PI;

        if (Mathf.Abs(delta) > align.GetRotMargin())
        {
            Debug.Log("Rotation finished = false");
            rotation_finished = false;
            look_at_pos = new Vector3(target_position);
            align.SetEnabled(true);
            align.SetRotation(delta);
        }
    }

    public void LookAtNextTile()
    {
        Debug.Log("Look At next tile");
        Vector3 next_tile = new Vector3(GetComponent<Transform>().position);
        if (path != null && path.Count > 0)
        {
            next_tile.x = path[0].GetTileX() * tile_size;
            next_tile.z = path[0].GetTileY() * tile_size;
        }
        LookAt(next_tile);
    }

    public void LookAtTile(PathNode obj_tile)
    {
        Vector3 next_tile = new Vector3(GetComponent<Transform>().position);
        Debug.Log("Looking at tile");

        next_tile.x = obj_tile.GetTileX() * tile_size;
        next_tile.z = obj_tile.GetTileY() * tile_size;

        Debug.Log("Next tile: x=" + next_tile.x + "y=" + next_tile.z);
        Debug.Log("Objective tile x:" + obj_tile.GetTileX() + "y:" + obj_tile.GetTileY());
        LookAt(next_tile);
    }

    public void LookAtPlayer()
    {
        Vector3 target_pos = new Vector3(GetLinkedObject("player_obj").GetComponent<Transform>().position);

        Vector3 forward = new Vector3(GetComponent<Transform>().GetForwardVector());
        Vector3 pos = new Vector3(GetComponent<Transform>().position);
        Vector3 obj_vec = new Vector3(target_pos - pos);

        float delta = Vector3.AngleBetweenXZ(forward, obj_vec);

        if (delta > Mathf.PI)
            delta = delta - 2 * Mathf.PI;
        if (delta < (-Mathf.PI))
            delta = delta + 2 * Mathf.PI;

        Debug.Log("Angle To Player: " + Mathf.Rad2deg(delta));
        Debug.Log("Rotation done: " + rotation_finished);
        Debug.Log("Looking at: " + dir);

        switch (dir)
        {
            case Direction.DIR_EAST:
                //South
                if (delta > (-(3 * Mathf.PI) / 4) && delta < -(Mathf.PI / 4))
                {
                    LookAtTile(new PathNode(GetCurrentTileX(), GetCurrentTileY() + 1));
                    Debug.Log("Looking at East rotationg to South");
                }
                //North
                if (delta > (Mathf.PI / 4) && delta < ((3 * Mathf.PI) / 4))
                {
                    LookAtTile(new PathNode(GetCurrentTileX(), GetCurrentTileY() - 1));
                    Debug.Log("Looking at East rotationg to North");
                }
                //West
                if (delta < (-(3 * Mathf.PI) / 4) || delta > (3 * Mathf.PI) / 4)
                {
                    LookAtTile(new PathNode(GetCurrentTileX() - 1, GetCurrentTileY()));
                    Debug.Log("Looking at East rotationg to West");
                }
                break;

            case Direction.DIR_NORTH:
                //East
                if (delta > (-(3 * Mathf.PI) / 4) && delta < -(Mathf.PI / 4))
                {
                    LookAtTile(new PathNode(GetCurrentTileX() + 1, GetCurrentTileY()));
                    Debug.Log("Looking at North rotationg to East");
                }
                //West
                if (delta > (Mathf.PI / 4) && delta < ((3 * Mathf.PI) / 4))
                {
                    LookAtTile(new PathNode(GetCurrentTileX() - 1, GetCurrentTileY()));
                    Debug.Log("Looking at North rotationg to West");
                }
                //South
                if (delta < (-(3 * Mathf.PI) / 4) || delta > (3 * Mathf.PI) / 4)
                {
                    LookAtTile(new PathNode(GetCurrentTileX(), GetCurrentTileY() + 1));
                    Debug.Log("Looking at North rotationg to South");
                }
                break;
            case Direction.DIR_SOUTH:
                //West
                if (delta > (-(3 * Mathf.PI) / 4) && delta < -(Mathf.PI / 4))
                {
                    LookAtTile(new PathNode(GetCurrentTileX() - 1, GetCurrentTileY()));
                    Debug.Log("Looking at South rotationg to West");
                }
                //East
                if (delta > (Mathf.PI / 4) && delta < ((3 * Mathf.PI) / 4))
                {
                    Debug.Log("Current tile x:" + GetCurrentTileX() + "y:" + GetCurrentTileY());
                    LookAtTile(new PathNode(GetCurrentTileX() + 1, GetCurrentTileY()));
                    Debug.Log("Looking at South rotationg to East");
                }
                //North
                if (delta < (-(3 * Mathf.PI) / 4) || delta > (3 * Mathf.PI) / 4)
                {
                    LookAtTile(new PathNode(GetCurrentTileX(), GetCurrentTileY() - 1));
                    Debug.Log("Looking at South rotationg to North");
                }
                break;
            case Direction.DIR_WEST:
                //North
                if (delta > (-(3 * Mathf.PI) / 4) && delta < -(Mathf.PI / 4))
                {
                    LookAtTile(new PathNode(GetCurrentTileX(), GetCurrentTileY() - 1));
                    Debug.Log("Looking at West rotationg to North");
                }
                //South
                if (delta > (Mathf.PI / 4) && delta < ((3 * Mathf.PI) / 4))
                {
                    LookAtTile(new PathNode(GetCurrentTileX(), GetCurrentTileY() + 1));
                    Debug.Log("Looking at West rotationg to South");
                }
                //East
                if (delta < (-(3 * Mathf.PI) / 4) || delta > (3 * Mathf.PI) / 4)
                {
                    LookAtTile(new PathNode(GetCurrentTileX() + 1, GetCurrentTileY()));
                    Debug.Log("Looking at West rotationg to East");
                }
                break;
        }
    }

    public bool LookingAtPlayer()
    {
        Vector3 target_pos = new Vector3(GetLinkedObject("player_obj").GetComponent<Transform>().position);

        Vector3 forward = new Vector3(GetComponent<Transform>().GetForwardVector());
        Vector3 pos = new Vector3(GetComponent<Transform>().position);
        Vector3 obj_vec = new Vector3(target_pos - pos);

        float delta = Vector3.AngleBetweenXZ(forward, obj_vec);

        if (delta > Mathf.PI)
            delta = delta - 2 * Mathf.PI;
        if (delta < (-Mathf.PI))
            delta = delta + 2 * Mathf.PI;

        if (delta >= -(Mathf.PI / 4) && delta <= (Mathf.PI / 4))
            return true;
        return false;
    }

    public Direction SetDirection()
    {
        Vector3 forward = new Vector3(GetComponent<Transform>().GetForwardVector());
        float delta = Mathf.Atan2(forward.x, forward.z);

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

        return dir;
    }

    public void SetEnemyTile(int x, int y)
    {
        tile.SetCoords(x, y);
    }

    public Direction GetDirection()
    {
        return dir;
    }

    public int GetCurrentTileX()
    {
        return tile.GetTileX();
    }

    public int GetCurrentTileY()
    {
        return tile.GetTileY();
    }

    public PathNode GetCurrentTile()
    {
        return new PathNode(tile.GetTileX(), tile.GetTileY());
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
        return ((GetComponent<Transform>().position) - (GetTargetPosition())).Length;
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

    public bool NextToPlayer()
    {
        player.GetComponent<MovementController>().GetPlayerPos(out int x, out int y);

        return (Mathf.Abs(y - GetCurrentTileY()) + Mathf.Abs(x - GetCurrentTileX())) <= 1;
    }

    public bool IsWalkable(uint tile_x, uint tile_y)
    {
        return map.GetComponent<Pathfinder>().IsWalkableTile(tile_x, tile_y);
    }

    public void SetInterupt(bool i)
    {
        interupt = i;
    }

    public bool CenteredInTile()
    {
        if (path == null || path.Count == 0) return true;

        if ((Mathf.Abs((GetCurrentTileX() - (path[0].GetTileX() * tile_size))) < arrive.min_distance) && (Mathf.Abs((GetCurrentTileY() - (path[0].GetTileY() * tile_size))) < arrive.min_distance))
            return true;

        return false;
    }


    private void MoveSideways(PathNode destination_node)
    {
        /*int distance_x = destination_node - GetCurrentTileX();
        int distance_y = destination_node - GetCurrentTileY();
        switch (dir)
        {
            case Direction.DIR_EAST:
                
                break;
            case Direction.DIR_NORTH:
                break;
            case Direction.DIR_SOUTH:
                break;
            case Direction.DIR_WEST:
                break;
        }*/
    }
}