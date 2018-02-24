using CulverinEditor;
using CulverinEditor.Debug;

/* WEAPON TYPES */
// 1 -> Metal Hand
// 2 -> Sword

/* SOUND FX */
//MetalHit -> 1
//SwordSlash -> 2

public class LeftWeapon : CulverinBehaviour
{
    public GameObject rweapon_obj;
    public GameObject lweapon_obj;
    public GameObject player_obj;
    public CharacterController player;
    public AttackTarget attack_collider;
    public GameObject l_button_obj;
    public CompButton button;
    public CoolDownLeft cd;
    public GameObject enemy_obj;
    public EnemyController enemy;
    public CompAnimation animation_weapon;
    //public CompAudio sound_fx;

    // WEAPON STATS -------------
    public float attack_dmg = 10.0f;
    public float cooldown = 4.0f;
    public float stamina_cost = 30.0f;
    // ---------------------------

    int step_counter = -1;

    void Start()
    {
        // Link GameObject variables with Scene GameObjects
        player_obj = GetLinkedObject("player_obj");
        l_button_obj = GetLinkedObject("l_button_obj");
        enemy_obj = GetLinkedObject("enemy_obj");

        step_counter = -1;
    }

    void Update()
    {
        if (step_counter == 0)
        {
            Debug.Log("Reseting collider...");
            lweapon_obj = GetLinkedObject("lweapon_obj");
            Vector3 position = lweapon_obj.GetComponent<CompRigidBody>().GetColliderPosition();
            position -= transform.forward;
            Quaternion rotation = lweapon_obj.GetComponent<CompRigidBody>().GetColliderQuaternion();
            lweapon_obj.GetComponent<CompRigidBody>().MoveKinematic(position, rotation);

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
        Debug.Log("Attack Left");

        lweapon_obj = GetLinkedObject("lweapon_obj");
        Vector3 position = lweapon_obj.GetComponent<CompRigidBody>().GetColliderPosition();
        position += transform.forward;
        Quaternion rotation = lweapon_obj.GetComponent<CompRigidBody>().GetColliderQuaternion();
        lweapon_obj.GetComponent<CompRigidBody>().MoveKinematic(position, rotation);
        Debug.Log("Kinematic collider moved");
        step_counter = 1;

        // Decrease stamina
        player_obj = GetLinkedObject("player_obj");
        player = player_obj.GetComponent<CharacterController>();
        player.DecreaseStamina(stamina_cost);

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
        if (player.GetState() == 0)
        {
            if (player.GetCurrentStamina() > stamina_cost)
            {
                l_button_obj = GetLinkedObject("l_button_obj");
                cd = l_button_obj.GetComponent<CoolDownLeft>();
                if (!cd.in_cd)
                {
                    Debug.Log("Going to Attack");

                    // First, OnClick of WeaponController, then, onClick of Cooldown
                    Attack();

                    //// Set Attacking State
                    lweapon_obj = GetLinkedObject("lweapon_obj");
                    animation_weapon = lweapon_obj.GetComponent<CompAnimation>();
                    animation_weapon.SetTransition("ToAttack1");

                    GameObject this_obj_rightweapon = GetLinkedObject("rweapon_obj");
                    animation_weapon = this_obj_rightweapon.GetComponent<CompAnimation>();
                    animation_weapon.SetTransition("ToAttack1");
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
    }

    public void PrepareAttack()
    {
        Debug.Log("Prepare Attack");
        l_button_obj = GetLinkedObject("l_button_obj");
        button = l_button_obj.GetComponent<CompButton>();
        button.Clicked(); // This will execute Cooldown & Weapon OnClick Methods
    }

    public void AttackHit()
    {
        // Get the GameObject from the collider hit
        if (enemy_obj != null)
        {
            enemy_obj = GetLinkedObject("enemy_obj");
            enemy = enemy_obj.GetComponent<EnemyController>();
            enemy.Hit(attack_dmg);
        }
    }

    public void SetCurrentAnim()
    {
        //player = player_obj.GetComponent<CharacterController>();
        Debug.Log("Sword Attack"); 
    }

    public void PlayFx()
    {
        // Reproduce specific audio
        //sound_fx = GetComponent<CompAudio>();
        Debug.Log("Sword Slash");  
    }
}