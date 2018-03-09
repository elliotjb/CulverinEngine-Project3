﻿using CulverinEditor;
using CulverinEditor.Debug;

public class JaimeController : CharacterController
{
    //MESH ELEMENTS
    public GameObject jaime_obj;
    public GameObject larm_jaime_obj; //To enable/disable mesh
    public GameObject rarm_jaime_obj; //To enable/disable mesh

    //UI ELEMENTS
    public GameObject jaime_icon_obj;
    public GameObject jaime_button_left;
    public GameObject jaime_button_right;

    /* Stats to modify Hp/Stamina bar depending on current character */
    public float max_hp = 100.0f;
    public float curr_hp = 100.0f;
    public float max_stamina = 100.0f;
    public float curr_stamina = 100.0f;

    //Secondary Ability Stats ---
    public float duration = 4.0f;
    public float sec_abuility_damage = 10.0f;
    public float sec_ability_cd = 10.0f;
    private float sec_ability_current_cd = 10.0f;
    // ---------------------

    //Left Ability Stats ---
    public float left_ability_dmg = 10.0f;
    public float left_ability_cost = 10.0f;
    JaimeCD_Left left_ability_cd;

    /* To perform different animations depending on the hit streak */
    int hit_streak = 0; 
    string[] anim_name = { "Attack1", "Attack2", "Attack3" };
    string current_anim = "Attack1";
    // ---------------------

    //Right Ability Stats ---
    public float right_ability_dmg = 0.0f;
    public float right_ability_cost = 50.0f;
    JaimeCD_Right right_ability_cd;
    // ---------------------

    protected override void Start()
    {
        sec_ability_current_cd = sec_ability_cd;

        // LINK VARIABLES TO GAMEOBJECTS OF THE SCENE
        jaime_obj = GetLinkedObject("jaime_obj");
        larm_jaime_obj = GetLinkedObject("larm_jaime_obj");
        rarm_jaime_obj = GetLinkedObject("rarm_jaime_obj");

        jaime_icon_obj = GetLinkedObject("jaime_icon_obj");
        jaime_button_left = GetLinkedObject("jaime_button_left");
        jaime_button_right = GetLinkedObject("jaime_button_right");

        // Start Idle animation
        anim_controller = jaime_obj.GetComponent<CompAnimation>();
        anim_controller.PlayAnimation("Idle");
    }

    public override void Update()
    {
        base.Update();
    }

    public override void ControlCharacter()
    {
        // Debug method to control Hp
        CheckHealth();

        //Test Secondary Ability
        if(Input.GetKeyDown(KeyCode.M))
        {
            SecondaryAbility();
        }

        // First check if you are alive
        health = GetLinkedObject("health_obj").GetComponent<Hp>();
        if (health.GetCurrentHealth() > 0)
        {
            // Check if player is moving to block attacks/abilities
            movement = GetLinkedObject("player_obj").GetComponent<MovementController>();
            if (!movement.IsMoving())
            { 
                /* Player is alive */
                switch (state)
                {
                    case State.IDLE:
                        {
                            //Check For Input + It has to check if he's moving to block attack (¿?)
                            CheckAttack();
                            break;
                        }
                    case State.ATTACKING:
                        {
                            //Check for end of the Attack animation
                            anim_controller = jaime_obj.GetComponent<CompAnimation>();
                            if (anim_controller.IsAnimationStopped(current_anim))
                            {
                                state = State.IDLE;
                            }
                            else
                            {
                                // Keep playing specific attack animation  until it ends
                                Debug.Log("Jaime Attacking " + hit_streak);
                            }
                            break;
                        }
                    case State.COVER:
                        {
                            //Check for end of the Attack animation
                            anim_controller = jaime_obj.GetComponent<CompAnimation>();                     
                            if (anim_controller.IsAnimationStopped("Cover"))
                            {
                                state = State.IDLE;
                            }
                            else
                            {
                                // Keep playing specific attack animation  until it ends
                                Debug.Log("Jaime Covering");
                            }
                            break;
                        }
                    case State.BLOCKING:
                        {
                            //Check for end of the Attack animation
                            anim_controller = jaime_obj.GetComponent<CompAnimation>();
                            if (anim_controller.IsAnimationStopped("Block"))
                            {
                                state = State.IDLE;
                            }
                            else
                            {
                                // Keep playing specific attack animation  until it ends
                                Debug.Log("Jaime Blocking");
                            }
                            break;
                        }
                    case State.HIT:
                        {
                            //Check for end of the Attack animation
                            anim_controller = jaime_obj.GetComponent<CompAnimation>();
                            if (anim_controller.IsAnimationStopped("Hit"))
                            {
                                state = State.IDLE;
                            }
                            else
                            {
                                // Keep playing specific attack animation  until it ends
                                Debug.Log("Jaime Hit");
                            }
                            break;
                        }
                    case State.DEAD:
                        {
                            Debug.Log("We are going doown");
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
        }
    }

    public override void CheckAttack()
    {
        //Left Attack
        if (Input.GetKeyDown(KeyCode.Num1))
        {
            Debug.Log("Jaime Pressed 1");
            PrepareLeftAbility();
        }

        //Right Attack
        else if (Input.GetKeyDown(KeyCode.Num2))
        {
            Debug.Log("Jaime Pressed 2");
            PrepareRightAbility();
        }

        if (Input.GetInput_KeyDown("LAttack", "Player"))
        {
            Debug.Log("Jaime Pressed 1");
            PrepareLeftAbility();
        }

        if (Input.GetInput_KeyDown("RAttack", "Player"))
        {
            Debug.Log("Jaime Pressed 2");
            PrepareRightAbility();
        }
    }

    public override void SecondaryAbility()
    {
        int curr_x = 0;
        int curr_y = 0;
        int enemy_x = 0;
        int enemy_y = 0;

        Debug.Log("Jaime Secondary Ability");
        GetLinkedObject("player_obj").GetComponent<CharactersManager>().shield_activated = true;

        //Enable Shield icon
        GetLinkedObject("shield_obj").GetComponent<CompImage>().SetEnabled(true, GetLinkedObject("shield_obj"));

        //Do Damage Around
        movement = GetLinkedObject("player_obj").GetComponent<MovementController>();
        movement.GetPlayerPos(out curr_x, out curr_y);

        //Check enemy in the tiles around the player
        for (int i = -1; i < 1; i++)
        {
            for (int j = -1; j < 1; j++)
            {
                if (i == 0 && j == 0)
                {
                    continue;
                }
                enemy_x = curr_x + j;
                enemy_y = curr_x + i;

                //Apply damage on the enemy in the specified tile
                GetLinkedObject("enemies_obj").GetComponent<EnemiesManager>().DamageEnemyInTile(enemy_x, enemy_y, sec_abuility_damage);
            }
        }

        // Activate the shield that protects from damage once
        GetLinkedObject("player_obj").GetComponent<Shield>().ActivateShield();
    }

    public override void GetDamage(float dmg)
    {
        if (state == State.COVER)
        {
            SetAnimationTransition("ToBlock", true);

            GetLinkedObject("player_obj").GetComponent<CompAudio>().PlayEvent("MetalHit");

            SetState(State.BLOCKING);
        }
        else
        {
            health = GetLinkedObject("health_obj").GetComponent<Hp>();
            health.GetDamage(dmg);

            SetAnimationTransition("ToHit", true);

            //GetLinkedObject("player_obj").GetComponent<CompAudio>().PlayEvent("Hit");

            //Reset hit count
            hit_streak = 0;

            SetState(State.HIT);
        }
    }

    public override void SetAnimationTransition(string name, bool value)
    {
        Debug.Log("Jaime Transitioning between animations");
        anim_controller = jaime_obj.GetComponent<CompAnimation>();
        anim_controller.SetTransition(name, value);
    }

    public override void UpdateHUD(bool active)
    {
        //Update Hp bar
        if (active)
        {
            Debug.Log("Update HP Jaime");
            //Update HP
            health = GetLinkedObject("health_obj").GetComponent<Hp>();
            health.SetHP(curr_hp, max_hp);

            Debug.Log("Update Stamina Jaime");
            //Update Stamina
            stamina = GetLinkedObject("stamina_obj").GetComponent<Stamina>();
            stamina.SetStamina(curr_stamina, max_stamina);
        }

        //Get values from var and store them
        else
        {
            health = GetLinkedObject("health_obj").GetComponent<Hp>();
            curr_hp = health.GetCurrentHealth();

            stamina = GetLinkedObject("stamina_obj").GetComponent<Stamina>();
            curr_stamina = stamina.GetCurrentStamina();
        }

        Debug.Log("Update Child Jaime");

        //Change current character icon
        icon = jaime_icon_obj.GetComponent<CompImage>();
        icon.SetEnabled(active, jaime_icon_obj); 
    }

    public override void ToggleMesh(bool active)
    {
        larm_jaime_obj.GetComponent<CompMesh>().SetEnabled(active, larm_jaime_obj);
        rarm_jaime_obj.GetComponent<CompMesh>().SetEnabled(active, rarm_jaime_obj);
    }

    public override bool IsAnimationStopped(string name)
    {
        anim_controller = jaime_obj.GetComponent<CompAnimation>();
        return anim_controller.IsAnimationStopped(name);
    }

    public bool IsSecondaryAbilityReady()
    {
        if (sec_ability_current_cd <= 0.0f)
            return true;
        else
            return false;
    }

    public override float GetSecondaryAbilityCoolDown()
    {
        return sec_ability_cd;
    }

    public override void ResetCoolDown()
    {
        sec_ability_current_cd = sec_ability_cd;
    }

    public override void ReduceSecondaryAbilityCoolDown()
    {
        sec_ability_current_cd -= Time.DeltaTime();
    }

    public void PrepareLeftAbility()
    {
        //button = jaime_button_left.GetComponent<CompButton>();
        //button.Clicked(); // This will execute LeftCooldown  
        OnLeftClick();
    }

    public bool OnLeftClick()
    {
        // Check if player is in Idle State
        if (GetState() == 0) /*0 = IDLE*/
        {
            //Calculate ability cost depending on the hit streak
            float calc_cost = left_ability_cost / Mathf.Pow(2, hit_streak);

            // Check if player has enough stamina to perform its attack
            if (GetCurrentStamina() > calc_cost)
            {
                //left_ability_cd = jaime_button_left.GetComponent<JaimeCD_Left>();
                //Check if the ability is not in cooldown
                //if (!left_ability_cd.in_cd)
                //{ 
                if (1 == 1) 
                { 
                    Debug.Log("Jaime LW Going to Attack");
                    DoLeftAbility(calc_cost);              
                    return true;
                }
                else
                {
                    Debug.Log("Ability in CD");
                    return false;
                }
            }
            else
            {
                Debug.Log("Not Enough Stamina");
                return false;
            }
        }
        return false;
    }

    public void DoLeftAbility(float cost)
    {
        Debug.Log("Jaime LW Attack Left");

        // Decrease stamina -----------
        DecreaseStamina(cost);

        Debug.Log("Jaime LW Going to hit");

        // Set Attacking Animation depending on the hit_streak
        current_anim = anim_name[hit_streak];
        SetAnimationTransition("To"+current_anim, true);

        // Attack the enemy in front of you
        GameObject coll_object = PhysX.RayCast(transform.position, transform.forward, 25.0f);

        if (coll_object != null)
        {
            // Check the specific enemy in front of you and apply dmg or call object OnContact
            Enemy_BT enemybt = coll_object.GetComponent<Enemy_BT>();
            if (enemybt != null)
            {
                enemybt.ApplyDamage(left_ability_dmg);

                if (hit_streak < 2)
                {
                    hit_streak++; //Increase hit count
                }
                else
                {
                    hit_streak = 0; //Reset hit count
                }
            }
            else
            {
                // Call Collider OnContact to notify raycast
                CompCollider obj_collider = coll_object.GetComponent<CompCollider>();
                if (obj_collider != null)
                {
                    obj_collider.CallOnContact();
                }
                else
                {
                    hit_streak = 0; //Reset hit count
                }
            }      
        }
        else
        {
            hit_streak = 0; //Reset hit count
        }

        // Play the Sound FX
        PlayFx("SwordSlash");

        SetState(CharacterController.State.ATTACKING);
    }

    public void PrepareRightAbility()
    {
        //button = jaime_button_right.GetComponent<CompButton>();
        //button.Clicked(); // This will execute RightCooldown    
        OnRightClick();
    }

    public bool OnRightClick()
    {
        // Check if player is in Idle State
        if (GetState() == 0)
        {
            // Check if player has enough stamina to perform its attack
            if (GetCurrentStamina() > right_ability_cost)
            {
                right_ability_cd = jaime_button_right.GetComponent<JaimeCD_Right>();
                //Check if the ability is not in cooldown
                if (!right_ability_cd.in_cd)
                {
                    Debug.Log("Jaime RW Going to Block");
                    DoRightAbility();
                    return true;
                }
                else
                {
                    Debug.Log("Jaime RW Ability in CD");
                    return false;
                }
            }
            else
            {
                Debug.Log("Jaime RW Not Enough Stamina");
                return false;
            }
        }
        return false;
    }

    public void DoRightAbility()
    {
        Debug.Log("Jaime LW Attack Right");

        // Decrease stamina -----------
        DecreaseStamina(right_ability_cost);

        // Set Covering State
        SetState(CharacterController.State.COVER);
    }
}