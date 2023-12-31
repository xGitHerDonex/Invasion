using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class playerController : MonoBehaviour, IDamage, IPhysics
{
    [SerializeField] CharacterController controller;
    public gameManager gameManager;

    //Players stats
    [Header("-----Player Stats-----")]
    public float HP;
    //[SerializeField] float maxHP;
    [SerializeField] float _maxHP; // = 200f;
    [SerializeField] float maxHPBuff;
    [SerializeField] float maxSTABuff;
    [SerializeField] float Stamina;
    [SerializeField] float maxStamina;
    [SerializeField] float regenStamina;
    [SerializeField] float playerSpeed;
    //[SerializeField] float playerSpeedBuff = 20f;
    [Range(0, 7)][SerializeField] float jumpHeight;
    float sprintCost;

    [Header("-----Expanded Player Stats-----")]
    [SerializeField] float originalPlayerSpeed;
    [SerializeField] int addSprintMod;
    [SerializeField] int jumpsMax;
    [SerializeField] float gravityValue;
    [SerializeField] Vector3 pushBack;
    [SerializeField] float pushBackResolve;

    //Player basic shooting
    [Header("-----Gun Stats------")]
    [SerializeField] List<gunStats> gunList = new List<gunStats>();
    [SerializeField] Transform gunMuzzle;
    [SerializeField] int shootDistance;
    [SerializeField] AudioClip shotSound;

    //Grenades
    [Header("-----Grenade Stats------")]
    [SerializeField] GameObject grenade;
    [SerializeField] float throwRate;
    [SerializeField] float maxThrowRate;
    [Range(1, 20)][SerializeField] int playerThrowForce;
    [Range(1, 20)][SerializeField] int throwLift;
    [SerializeField] Transform throwPos;

    //Time Slow
    [Header("-----Chronokinesis-----")]
    [SerializeField] bool isTimeSlowed;
    [SerializeField] float time;
    [SerializeField] float maxTime;
    [SerializeField] float regenTime;
    [Range(0, 2)][SerializeField] float timeSlowInSeconds;
    [Range(0, 2)][SerializeField] float timeSlowScale;
    [Range(0, 3)][SerializeField] float playerSlowSpeed;

    //Waypoints are used to track the player and what is close to them.
    [Header("-----Waypoints-----")]
    [SerializeField] GameObject[] waypoints;
    [SerializeField] float waypointDist;
    [SerializeField] float closestWaypointDist;
    [SerializeField] GameObject closestWaypoint;

    [Header("-----UI Elements-----")]
    [SerializeField] private Text lowHPWarnText;
    [SerializeField] private GameObject lowHealthWarnBG;
    //[SerializeField] private TextMeshProUGUI ammoCurText;
    //[SerializeField] private TextMeshProUGUI ammoMaxText;

    [Header("-----SFX-----")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip playerInjured;
    [SerializeField] AudioClip playerWalksGrass;
    [SerializeField] AudioClip playerRunsGrass;
    [SerializeField] AudioClip playerJumpsGrass;

    //Bools and others for functions
    public bool isShooting = false;
    //public AmmoType ammoType;
    private bool groundedPlayer;
    private bool canSprint = true;
    private bool isTakingDamage = false;
    private bool isJumping;
    private int jumpedTimes;
    private Vector3 playerVelocity;
    private Vector3 move;
    int selectedGun;
    public bool hasEnergeticRing = false;
    public bool hasSynthesizer = false;
    public bool grenadeCD = true;

    //Player Buff Checks
    [SerializeField] float regenStaminaBuffAmount;

    private float fixedDeltaTime;


    //public enum AmmoType
    //{
    //    PistolAmmo,
    //    PulsarAmmo,
    //    ShotgunAmmo,
    //    RailgunAmmo,
    //    RBFGAmmo
    //}

    // Ammo Management
    public Dictionary<string, int> ammoInventory = new Dictionary<string, int>();

    void Awake()
    {
        this.fixedDeltaTime = Time.fixedDeltaTime;
    }


    //Start
    private void Start()
    {

        //set up for respawn
        HP = _maxHP;
        Stamina = maxStamina;
        spawnPlayer();
        time = maxTime;
        //Sets player speed
        originalPlayerSpeed = playerSpeed;

        //Create Ground Waypoint Matrix
        waypoints = GameObject.FindGameObjectsWithTag("GNDWP");


    }

    //Update
    void Update()
    {
        //Call to movement
        movement();
        sprint();

        // Time slow
        chronokinesis();

        

        //Keeps Stamina Bar updated
        gameManager.instance.updateStamBar(Stamina / maxStamina);
        gameManager.instance.updateChronoBar(time / maxTime);

        //Low Health Warning
        lowHealthWarning();

        //Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * 70, Color.green);

        //Throw grenade - works similar to shoot    
        if (Input.GetButtonDown("throw") && !isShooting && grenadeCD && !gameManager.instance.isPaused)
        {
            // Check if the "Shoot" button is also pressed, and if so, do not throw the grenade
            if (!Input.GetButton("Shoot"))
            {
                // Start the throwGrenade coroutine
                StartCoroutine(throwGrenade());
                grenadeCD = false;
            }
        }

        //Keeps Stamina Bar updated
        gameManager.instance.updateStamBar(Stamina / maxStamina);
        gameManager.instance.updateChronoBar(time / maxTime);

    }

    //Move Ability:  Currently allows player to move!  Wheee!
    void movement()
    {
        //Prevents inadvertent jumping while the game is paused
        if (gameManager.instance.isPaused)
        {
            return;
        }
        //controls pushback amount
        if (pushBack.magnitude > 0.01f)
        {
            pushBack = Vector3.Lerp(pushBack, Vector3.zero, Time.deltaTime * pushBackResolve);
            pushBack.x = Mathf.Lerp(pushBack.x, 0, Time.deltaTime * pushBackResolve);
            pushBack.y = Mathf.Lerp(pushBack.y, 0, Time.deltaTime * pushBackResolve * 3);
            pushBack.z = Mathf.Lerp(pushBack.z, 0, Time.deltaTime * pushBackResolve);

        }

        //Sets grounded bool if the player is grounded
        groundedPlayer = controller.isGrounded;

        //Resets jump and updates player vertical vecloity is player is grounded
        if (groundedPlayer && playerVelocity.y < 0)
        {
            jumpedTimes = 0;
            playerVelocity.y = 0f;
            isJumping = false;
        }

        //Calculates movement
        move = Input.GetAxis("Horizontal") * transform.right + Input.GetAxis("Vertical") * transform.forward;


        //Adds sound to player footsteps (only while walking)
        float moveMagnitude = move.magnitude;

        //Debug.Log(moveMagnitude);
        if (moveMagnitude > .3f && !audioSource.isPlaying && !isJumping)
        {
            audioSource.PlayOneShot(playerWalksGrass);
        }

        //moves controller based on movement
        controller.Move(move * Time.deltaTime * playerSpeed);


        //Jump Ability:  Currently allows player to jump
        if (Input.GetButtonDown("Jump") && jumpedTimes < jumpsMax && Stamina>=1.3)
        {
            jumpedTimes++;
            playerVelocity.y += jumpHeight;
            isJumping = true;
            //Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);

            //Jumping now makes a sound
            audioSource.PlayOneShot(playerJumpsGrass);
            //Jumping now drains some stamina
            Stamina -= 1.1f;
        }

        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move((playerVelocity + pushBack) * Time.deltaTime);
    }


    void sprint()
    {
        float moveMagnitude = move.magnitude;

        float deltaSprint = sprintCost * Time.deltaTime;

        if (hasEnergeticRing)
        {
            sprintCost = 0.5f;
        }
        else
            sprintCost = 1.0f;


        if (!gameManager.instance.isPaused && !isTimeSlowed && Input.GetButton("Sprint") && Stamina >= deltaSprint && canSprint && moveMagnitude>0.2)
        {
            playerSpeed = originalPlayerSpeed + addSprintMod;
            Stamina -= deltaSprint;
        }


        else
        {
            if (Stamina < maxStamina)
            {
                Stamina += regenStamina * Time.deltaTime;
            }

            playerSpeed = originalPlayerSpeed;

        }

        if (Stamina <= deltaSprint)
        {
            StartCoroutine(outOfBreath());
        }
    }

    IEnumerator outOfBreath()
    {
        canSprint = false;
        yield return new WaitForSeconds(2);
        canSprint = true;
    }


    //Throw Grenade: will throw a grenade based on the throwforce and lift provided. 
    //Is infinite but only 1 grenade per second can be thrown
    IEnumerator throwGrenade()
    {
        //isShooting = false;
        //creates grenades

        GameObject thrownGrenade = Instantiate(grenade, throwPos.transform.position, throwPos.transform.rotation);
        Rigidbody thrownGrenadeRb = thrownGrenade.GetComponent<Rigidbody>();

        //throws grenade
        thrownGrenadeRb.AddForce((throwPos.transform.forward * playerThrowForce * 20) + (transform.up * throwLift), ForceMode.Impulse);
        
        grenadeCooldown();
        //wait for throwrate, then flip bool back
        yield return new WaitForSeconds(throwRate);

        grenadeCD = true;
        //isShooting = true;
        

    }

    public void grenadeCooldown()
    {
        StartCoroutine(grenadeCountdown(maxThrowRate));
    }

    private IEnumerator grenadeCountdown(float cooldownTime)
    {
        float remainingTime = cooldownTime;

        while (remainingTime >= 0)
        {
            remainingTime -= Time.deltaTime;
            string formattedTime = remainingTime.ToString("F1");
            // start the timer
            gameManager.instance.updateGrenadeCDText(formattedTime);
            //if (Mathf.Floor(remainingTime) != Mathf.Floor(remainingTime - Time.deltaTime))
            //{

            //    gameManager.instance.updateGrenadeCDText(Mathf.Floor(remainingTime).ToString());
            //}
            yield return null;
        }
        // Reset the fill amount when the cooldown is complete
        gameManager.instance.updateGrenadeCDText("F");
        grenadeCD = true;
    }


    //Time Slow: slows the world but not the player
    void chronokinesis()
    {


        if (!gameManager.instance.isPaused && Input.GetButton("time") && time > 0)
        {
            #region old Code 
            //playerSpeed = originalPlayerSpeed * playerSlowSpeed;
            //isTimeSlowed = true;

            ////Time.timeScale = timeSlowScale;


            ////time = time - Time.deltaTime; 
            #endregion

            //if(Time.timeScale == 1)
            //{
            TimeSlowed();

            //}


        }

        else if (!gameManager.instance.isPaused)
        {
            TimeUnslowed();

        }

    }

    private void TimeUnslowed()
    {
        isTimeSlowed = false;
        Time.timeScale = 1f;
        //playerSpeed = originalPlayerSpeed;


        if (time <= maxTime && !isTimeSlowed)
        {
            time += regenTime * Time.deltaTime;

        }
    }

    private void TimeSlowed()
    {
        isTimeSlowed = true;
        Time.timeScale = .5f;
        Time.fixedDeltaTime = this.fixedDeltaTime * Time.timeScale;
        playerSpeed = originalPlayerSpeed;
        time = time - (Time.deltaTime / 2);
    }

    //
    IEnumerator delaySpeed()
    {
        playerSpeed = playerSpeed * 10000;
        yield return new WaitForSeconds(0.2f);
        //slow time and increase player speed

    }

    //Health Increase via Crimson Stone Equipment
    public void giveHP(int amount)
    {
        if (HP < MaxHP)
        {
            HP += amount;
            gameManager.instance.updateHpBar(HP / MaxHP);
        }

    }

    // Temporary health buff via Health Up Orb pickup
    public void givemaxHPBuff(float duration, float maxHPBuff)
    {
        _maxHP += maxHPBuff;
        StartCoroutine(destroyHPBuff(duration, maxHPBuff));
    }

    // Destroy's temporary health up buff
    IEnumerator destroyHPBuff(float duration, float maxHPBuff)
    {
        yield return new WaitForSeconds(duration);

        _maxHP -= maxHPBuff;
    }

    public void givemaxSTABuff(float duration, float maxSTABuff)
    {
        maxStamina += maxSTABuff;
        StartCoroutine(destroySTABuff(duration, maxSTABuff));
    }

    IEnumerator destroySTABuff(float duration, float maxSTABuff)
    {
        yield return new WaitForSeconds(duration);
        maxStamina -= maxSTABuff;
        maxStamina = 3f;
        Stamina = maxStamina;
    }



    //Damageable Ability:  Currently allows player takes damage
    public void hurtBaddies(int amount)
    {
        if (!isTakingDamage)
        {
            isTakingDamage = true;
            HP -= amount;
            gameManager.instance.updateHpBar(HP / _maxHP); // updates HP UI
            StartCoroutine(gameManager.instance.playerFlashDamage());

            if (HP <= 0)
            {
                //Plays the you lose method after your health hits 0
                gameManager.instance.youLose();
            }
            else
            {
                //Damage sound!
                audioSource.PlayOneShot(playerInjured);
            }
            StartCoroutine(ResetTakingDamage());
        }
    }

    //Prevents repeat Damage
    IEnumerator ResetTakingDamage()
    {
        isTakingDamage = true;
        //Small delay to prevent repeat damage
        yield return new WaitForSeconds(0.35f);
        isTakingDamage = false;
    }

    //low hp warning
    private void lowHealthWarning()
    {
        //Once the player hits approximately 33% HP of their Max HP (equipment, etc) it'll trigger the warning
        float lowHPThresh = _maxHP * 0.33f;

        if (HP <= lowHPThresh)
        {
            //Turn on Low Health warning
            lowHPWarnText.gameObject.SetActive(true);
            lowHealthWarnBG.SetActive(true);
        }
        else
        {
            //Turn off Low Health warning
            lowHealthWarnBG.SetActive(false);
            lowHPWarnText.gameObject.SetActive(false);

        }
    }

    //Spawns player
    public void spawnPlayer()
    {

        //Resets Players HP
        HP = _maxHP;
        updatePlayerUI();

        //Prevents playerController from taking over the script
        controller.enabled = false;
        transform.position = gameManager.instance.playerSpawnPos.transform.position;
        controller.enabled = true;
    }

    //Physics method, player will not use bool here
    public void physics(Vector3 dir)
    {
        pushBack += dir;
    }

    //Updates players HP bar after a respawn.  Implemented in spawnPlayer()
    public void updatePlayerUI()
    {
        gameManager.instance.updateHpBar((float)HP / _maxHP);

    }

    //method made for delaying damage for physics
    public void delayDamage(int damage, float seconds)
    {
        StartCoroutine(delayedDamage(damage, seconds));
    }

    //method made for delaying damage for physics
    IEnumerator delayedDamage(int damage, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        hurtBaddies(damage);
    }

    //Ammo Collection for weapons
    //public void AddAmmo(AmmoType ammoType, int amount)
    //{
    //    string ammoTypeName = ammoType.ToString();

    //    if (ammoInventory.ContainsKey(ammoTypeName))
    //    {
    //        ammoInventory[ammoTypeName] += amount;
    //    }
    //    else
    //    {
    //        ammoInventory[ammoTypeName] = amount;
    //    }
    //}

    //public method that will return the ground waypoint closest to the player
    public GameObject getClosestGroundWaypoint()
    {
        GameObject closestWaypoint = null;
        closestWaypointDist = 3000f;

        //iterates through the list to find the distances
        foreach (GameObject waypoint in waypoints)
        {
            //calculates distance
            waypointDist = Vector3.Distance(transform.position, waypoint.transform.position);


            //updates closest waypoint
            if (waypointDist < closestWaypointDist)
            {
                closestWaypointDist = waypointDist;
                closestWaypoint = waypoint;
            }

        }

        //returns the waypoint
        return closestWaypoint;
    }

    public float MaxHP
    {
        get
        {
            // Calculate maxHP including any buffs
            return _maxHP + maxHPBuff;
        }
        set
        {
            // Ensure maxHP is never negative
            _maxHP = Mathf.Max(value, 0f);
        }
    }
    public float MaxStamina
    {
        get
        {
            // Calculate maxSTA including any buffs
            return maxStamina + maxSTABuff;
        }
        set
        {
            // Ensure maxSTA is never negative
            maxStamina = Mathf.Max(0f, value);
        }
    }

    public void ApplyPermanentHPBoost(float hpBoost)
    {
        _maxHP += hpBoost;
    }

    //public void ApplyPermanentSpeedBuff(float speedBuff)
    //{
    //    playerSpee
    //}

    public void ApplyMeleeDamage(int Damage)
    {
        HP -= Damage;
    }

    public void IncreasePlayerSpeed()
    {

        playerController pc = gameManager.instance.player.GetComponent<playerController>();
        pc.playerSpeed = pc.playerSpeed + 10;
        pc = null;
        Destroy(pc);

    }
}
