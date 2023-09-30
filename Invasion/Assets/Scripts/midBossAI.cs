using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Device;

public class midBossAI : MonoBehaviour, IDamage, IPhysics
{
    [Header("-----Components-----")]
    [SerializeField] Renderer model;
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Transform headPos;
    [SerializeField] Animator anime;
    [SerializeField] Collider hitBox;

    [Header("-----Enemy Stats-----")]

    [Tooltip("Turning speed 1-10.")]
    [Range(1, 10)][SerializeField] int targetFaceSpeed;


    [Tooltip("Enemy health value between 1 and 100.")]
    [Range(1, 100)][SerializeField] int hp;
    [Range(1, 100)][SerializeField] int maxHp;


    [Tooltip("10 is the default value for all current speeds. Changing this without adjusting Enemy Speed and nav mesh speed will break it!!!!")]
    [Range(-30, 30)][SerializeField] float animeSpeedChange;
    //[SerializeField] int viewAngle;
    [SerializeField] int enemyRunSpeed;


    [Header("-----Melee Components-----")]
    [SerializeField] GameObject leftMeleeCollider;
    [SerializeField] GameObject rightMeleeCollider;
    [Tooltip("Delay Value between melee attacks in seconds")]
    [SerializeField] int meleeDelay;
    [Tooltip("meleeDamage")]
    [SerializeField] int meleeDamage;

    [Header("SFX")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip walkSound;
    [SerializeField] AudioClip attackSound;
    [SerializeField] AudioClip deathSound;


    Vector3 pushBack;
    Vector3 playerDirection;
    Vector3 startingPos;
    float stoppingDistOriginal;
    //float angleToPlayer;
    float speedOrig;
    bool isAttacking;
    bool isDead;
    bool playerInRange;
    bool isRunning;
    float agentVel;
    //bool destinationPicked;

    // Start is called before the first frame update
    void Start()
    {
        speedOrig = agent.speed; // gives the agent speed to the float original speed for later on. 
        startingPos = transform.position;
        stoppingDistOriginal = agent.stoppingDistance;
        hp = maxHp;
        isDead = false;

    }


    // Update is called once per frame
    void Update()
    {
        float hpRatio = (hp / maxHp) * 100;

        //Selects stage for enemy AI based on Health Remaining

        if (hpRatio >= 70)
            Stage1();

        else if (hpRatio >= 30 && hpRatio <= 70)
            Stage2();

        else if (hpRatio >= 0 && hpRatio <= 30)
            Stage3();
    }


    void Stage1()
    {
        //If enemy is not dead, we'll continue
        if (!isDead)
        {     
            //Updates the animator and speed of the enemy to make the enemy appear running
            if (!isRunning)
            {
                agentVel = agent.velocity.normalized.magnitude;
                agent.speed = speedOrig;
            }

            else if (isRunning)
            {
                agentVel = agent.velocity.normalized.magnitude + 1;
                agent.speed = enemyRunSpeed;

            }

            //casts a ray on the player
            RaycastHit hit;

            //get's player's direction
            playerDirection = gameManager.instance.player.transform.position - headPos.position;

            //angleToPlayer = Vector3.Angle(new Vector3(playerDirection.x, 0, playerDirection.z), transform.forward);


            //Casts a ray on the player
            if (Physics.Raycast(headPos.position, playerDirection, out hit))
            {

                anime.SetFloat("Speed", Mathf.Lerp(anime.GetFloat("Speed"), agentVel, Time.deltaTime * animeSpeedChange));

                //Gets distance to player
                float distToPlayer = Vector3.Distance(headPos.position, gameManager.instance.player.transform.position);

                //Uncomment to check distance between enemy player
                //Debug.Log(distToPlayer);


                //If player within stopping distance, face target and attack if not already attacking
                if (playerInRange && hit.collider.CompareTag("Player") && distToPlayer <= agent.stoppingDistance)
                {
                    isRunning = false;
                    faceTarget();

                    if (!isAttacking)
                    {
                        StartCoroutine(Melee());
                    }

                }

                //if player is not wthin stopping distance, then set distination to the player
                else if (playerInRange && distToPlayer >= agent.stoppingDistance)
                {
                    isRunning = true;
                    faceTarget();
                    agent.SetDestination(gameManager.instance.player.transform.position);

                }
            }

        }
    }

    void Stage2()
    {
        //If enemy is not dead, we'll continue
        if (!isDead)
        {
            //Updates the animator and speed of the enemy to make the enemy appear running
            if (!isRunning)
            {
                agentVel = agent.velocity.normalized.magnitude;
                agent.speed = speedOrig;
            }

            else if (isRunning)
            {
                agentVel = agent.velocity.normalized.magnitude + 1;
                agent.speed = enemyRunSpeed;

            }

            //casts a ray on the player
            RaycastHit hit;

            //get's player's direction
            playerDirection = gameManager.instance.player.transform.position - headPos.position;

            //angleToPlayer = Vector3.Angle(new Vector3(playerDirection.x, 0, playerDirection.z), transform.forward);


            //Casts a ray on the player
            if (Physics.Raycast(headPos.position, playerDirection, out hit))
            {

                anime.SetFloat("Speed", Mathf.Lerp(anime.GetFloat("Speed"), agentVel, Time.deltaTime * animeSpeedChange));

                //Gets distance to player
                float distToPlayer = Vector3.Distance(headPos.position, gameManager.instance.player.transform.position);

                //Uncomment to check distance between enemy player
                //Debug.Log(distToPlayer);


                //If player within stopping distance, face target and attack if not already attacking
                if (playerInRange && hit.collider.CompareTag("Player") && distToPlayer <= agent.stoppingDistance)
                {
                    isRunning = false;
                    faceTarget();

                    if (!isAttacking)
                    {
                        StartCoroutine(Melee());
                    }

                }

                //if player is not wthin stopping distance, then set distination to the player
                else if (playerInRange && distToPlayer >= agent.stoppingDistance)
                {
                    isRunning = true;
                    faceTarget();
                    agent.SetDestination(gameManager.instance.player.transform.position);

                }
            }

        }
    }

    void Stage3()
    {
        //If enemy is not dead, we'll continue
        if (!isDead)
        {
            //Updates the animator and speed of the enemy to make the enemy appear running
            if (!isRunning)
            {
                agentVel = agent.velocity.normalized.magnitude;
                agent.speed = speedOrig;
            }

            else if (isRunning)
            {
                agentVel = agent.velocity.normalized.magnitude + 1;
                agent.speed = enemyRunSpeed;

            }

            //casts a ray on the player
            RaycastHit hit;

            //get's player's direction
            playerDirection = gameManager.instance.player.transform.position - headPos.position;

            //angleToPlayer = Vector3.Angle(new Vector3(playerDirection.x, 0, playerDirection.z), transform.forward);


            //Casts a ray on the player
            if (Physics.Raycast(headPos.position, playerDirection, out hit))
            {

                anime.SetFloat("Speed", Mathf.Lerp(anime.GetFloat("Speed"), agentVel, Time.deltaTime * animeSpeedChange));

                //Gets distance to player
                float distToPlayer = Vector3.Distance(headPos.position, gameManager.instance.player.transform.position);

                //Uncomment to check distance between enemy player
                //Debug.Log(distToPlayer);


                //If player within stopping distance, face target and attack if not already attacking
                if (playerInRange && hit.collider.CompareTag("Player") && distToPlayer <= agent.stoppingDistance)
                {
                    isRunning = false;
                    faceTarget();

                    if (!isAttacking)
                    {
                        StartCoroutine(Melee());
                    }

                }

                //if player is not wthin stopping distance, then set distination to the player
                else if (playerInRange && distToPlayer >= agent.stoppingDistance)
                {
                    isRunning = true;
                    faceTarget();
                    agent.SetDestination(gameManager.instance.player.transform.position);

                }
            }

        }
    }

    void playWalkSound()
        {
          if (audioSource != null && walkSound != null)
            {
                audioSource.clip = walkSound;
                audioSource.Play();
            }
        }


        /*
         * if the enemy takes damage requires an amount for the damage in a whole number
         * then subtracts the amount of damage from that whole number
         * the call the sub routine to run at the same time to make the enemy feedback show (flashing damage indicator)
         * also if the health is less than or equal to 0 destroy this enemy
         * 
         */
        public void takeDamage(int amount)
        {
            hp -= amount;
            StartCoroutine(stopMoving());


            if (hp <= 0)
            {
                isDead = true; //Will keep update method from running since Enemy is now dead
                hitBox.enabled = false; // turns off the hitbox so player isnt collided with the dead body
                agent.enabled = false;
                anime.SetBool("Death", true);
                playDeathSound();
            
            }

            else
            {
                anime.SetTrigger("Damage");
                StartCoroutine(flashDamage());
                agent.SetDestination(gameManager.instance.player.transform.position);
            }



        }

        //Allows the attached sound for Death Sound to be played
        private void playDeathSound()
        {
            if (audioSource != null && deathSound != null)
            {
                audioSource.clip = deathSound;
                audioSource.Play();
            }
        }


        IEnumerator stopMoving()
        {
            agent.speed = 0;
            yield return new WaitForSeconds(1);
            agent.speed = speedOrig;
        }


        //changes the material color from the original material to a red color for .1 seconds
        //the changes the color back to its original white state.
        IEnumerator flashDamage()
        {
            model.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            model.material.color = Color.white;
        }


        IEnumerator Melee()
        {
            isAttacking = true;
            leftMeleeCollider.SetActive(true);
            rightMeleeCollider.SetActive(true);
            anime.SetTrigger("Melee");

            yield return new WaitForSeconds(meleeDelay);
            isAttacking = false;
            leftMeleeCollider.SetActive(false);
            rightMeleeCollider.SetActive(false);
        }

   
        //Allows the attached sound for Attack Sound to be played
        private void playAttackSound()
            {
                if (audioSource != null && attackSound != null)
                {
                    audioSource.clip = attackSound;
                    audioSource.Play();
                }
            }

        //sets the rotation of the enemy to face the player based on the player direction to the enemy 
        //and it lerps the rotation over time so it is smooth and not choppy
        void faceTarget()
        {
            Quaternion rotation = Quaternion.LookRotation(playerDirection);
            //lerp over time rotation
            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime * targetFaceSpeed);
        }

        public void physics(Vector3 dir)
        {
            agent.velocity += dir / 3;

        }

        //this method is for starting a co-routine in case a delay is needed
        public void delayDamage(int damage, float seconds)
        {
            StartCoroutine(delayedDamage(damage, seconds));
        }

        //method made for triggering explosion damage through iPhysics
        public IEnumerator delayedDamage(int explosionDamage, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            takeDamage(explosionDamage);
        }


        //if an object enters the collider for the enemy check to see if it is the Player
        //if it is the player set player in range bool to true
        //returns nothing
        public void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRange = true;

            }
        }



        // does the exact opposite as On Trigger enter
        // it checks to see if the object that is in the collider is the player if it isnt then the player isnt in range
        //set the stopping distance to zero 
        public void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                playerInRange = false;
                agent.stoppingDistance = 0;

            }
        }

    
}