using CulverinEditor;
using CulverinEditor.Debug;

/* WEAPON TYPES */
// 1 -> Metal Hand
// 2 -> Sword

/* SOUND FX */
//MetalHit -> 1
//SwordSlash -> 2

public class WeaponController : CulverinBehaviour
{
    public GameObject player_obj;
    public GameObject this_obj;
    public CharacterController player;
    public AttackTarget attack_collider;
    public GameObject button_obj;
    public CompButton button;
    public CoolDown cd;
    public GameObject enemy_obj;
    public EnemyController enemy;
    //public CompAudio sound_fx;

    public int weapon_type = 0;

    // WEAPON STATS -------------
    public float attack_dmg = 10.0f;
    public float cooldown = 5.0f;
    public float stamina_cost = 10.0f;
    // ---------------------------
    int step_counter = -1;

    void Start()
    {
        // Link GameObject variables with Scene GameObjects
        player_obj = GetLinkedObject("player_obj");
        button_obj = GetLinkedObject("button_obj");
        enemy_obj = GetLinkedObject("enemy_obj");
        step_counter = -1;
    }

    void Update()
    {
        if(step_counter == 0)
        {
            Debug.Log("Reseting collider...");
            this_obj = GetLinkedObject("this_obj");
            Vector3 position = this_obj.GetComponent<CompRigidBody>().GetColliderPosition();
            position -= transform.forward;
            Quaternion rotation = this_obj.GetComponent<CompRigidBody>().GetColliderQuaternion();
            this_obj.GetComponent<CompRigidBody>().MoveKinematic(position, rotation);

            Debug.Log("Collider reseted");
            step_counter--;
        }
        else if (step_counter > 0)
        {
            step_counter--;
        }
    }

    public void Attack()
    {
        Debug.Log("Attack");

        this_obj = GetLinkedObject("this_obj");
        Vector3 position = this_obj.GetComponent<CompRigidBody>().GetColliderPosition();
        position += transform.forward;
        Quaternion rotation = this_obj.GetComponent<CompRigidBody>().GetColliderQuaternion();
        this_obj.GetComponent<CompRigidBody>().MoveKinematic(position, rotation);
        Debug.Log("Kinematic collider moved");
        step_counter = 1;


        // Decrease stamina
        player_obj = GetLinkedObject("player_obj");
        player = player_obj.GetComponent<CharacterController>();
        player.DecreaseStamina(stamina_cost);
        Debug.Log("SetAnim");
        // Play specific animation
        SetCurrentAnim();

        Debug.Log("PlayFx");
        // Play specific sound
        PlayFx();

        Debug.Log("Going to hit");
        // Temp Method
        enemy_obj = GetLinkedObject("enemy_obj");
        enemy = enemy_obj.GetComponent<EnemyController>();
        enemy.Hit(attack_dmg);
    }

    void OnTriggerEnter()
    {
        Debug.Log("Trigger enter");

     //   GameObject enemy_obj = GetComponent<CompCollider>().GetCollidedObject();
        enemy_obj = GetLinkedObject("enemy_obj");
        enemy = enemy_obj.GetComponent<EnemyController>();
        enemy.Hit(attack_dmg);
        Debug.Log("Damage to enemy done");
    }

    // This method will be called when the associated button to this weapon is pressed
    void OnClick()
    {
        // Check if player has enough stamina to perform its attack
        player_obj = GetLinkedObject("player_obj");
        player = player_obj.GetComponent<CharacterController>();
        if (player.GetCurrentStamina() > stamina_cost)
        {
            button_obj = GetLinkedObject("button_obj");
            cd = button_obj.GetComponent<CoolDown>();
            if (!cd.in_cd)
            {
                Debug.Log("Going to Attack");

                // First, OnClick of WeaponController, then, onClick of Cooldown
                Attack();

                // Set Attacking State
                player_obj = GetLinkedObject("player_obj");
                player = player_obj.GetComponent<CharacterController>();
                player.SetState(CharacterController.State.ATTACKING);
            }
            else
            {
                Debug.Log("Ability in CD");
            }
        }
        else
        {
            Debug.Log("Not Enough Stamina");
        }
    }

    public void PrepareAttack()
    {
        Debug.Log("Prepare Attack");
        button_obj = GetLinkedObject("button_obj");
        button = button_obj.GetComponent<CompButton>();
        button.Clicked(); // This will execute Cooldown & Weapon OnClick Methods
        //OnClick(); // Temp call
        //Attack();
    }

    public void SetCurrentAnim()
    {
        //player = player_obj.GetComponent<CharacterController>();
        switch (weapon_type)
        {
            case 1:
                {
                    //player.SetAnim("MetalHit"); 
                    //player.SetAnimName("Attack1");
                    Debug.Log("Attack Anim 1");
                    break;
                }
            case 2:
                {
                    //player.SetAnim("SwordSlash");
                    //player.SetAnimName("Attack2");
                    Debug.Log("Attack Anim 2");
                    break;
                }
            default:
                {
                    break;
                }
        }
    }

    public void PlayFx()
    {
        // Reproduce specific audio
        //sound_fx = GetComponent<CompAudio>();

        switch (weapon_type)
        {
            case 1:
                {
                    //player.SetAnim("MetalHit"); 
                    //player.SetAnimName("Attack1");
                    //sound_fx.PlayEvent("MetalHit");
                    Debug.Log("Attack Sound 1");
                    break;
                }
            case 2:
                {
                    //player.SetAnim("SwordSlash");
                    //player.SetAnimName("Attack2");
                    //sound_fx.PlayEvent("SwordSlash");
                    Debug.Log("Attack Sound 2");
                    break;
                }
            default:
                {
                    break;
                }
        }
    }
}