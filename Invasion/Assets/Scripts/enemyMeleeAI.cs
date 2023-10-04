using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Device;

public class enemyMeleeAI : MonoBehaviour, IDamage, IPhysics
{
    #region Fields, Members and Variables
    [Header("-----Components-----")]
    [SerializeField] Renderer model;
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Transform headPos;
    [SerializeField] Animator anime;
    [SerializeField] Collider hitBox;

    [Header("-----Enemy Stats-----")]

    [Tooltip("Enemy health value between 1 and 100.")]
    [Range(1, 100)][SerializeField] int hp;

    [Tooltip("Turning speed 1-10.")]
    [Range(1, 10)][SerializeField] int targetFaceSpeed;

    [Tooltip("Enemy viewing angle, (-)360-360.")]
    [Range(-360, 360)][SerializeField] int viewAngle;

    [Tooltip("This controls how far movement stops from spawn point.")]
    [Range(0, 100)][SerializeField] int roamDistance;

    [Tooltip("How long enemy will stop before going to another location. Will eventually become a float and not integer. 1-10.")]
    [Range(1, 10)][SerializeField] int roamPauseTime;

    [Tooltip("10 is the default value for all current speeds. Changing this without adjusting Enemy Speed and nav mesh speed will break it!!!!")]
    [Range(-30, 30)][SerializeField] float animeSpeedChange;


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
    float angleToPlayer;
    float speedOrig;
    bool isAttacking;
    bool isDead;
    bool playerInRange;
    bool destinationPicked;

    #endregion


    // Start is called before the first frame update
    void Start()
    {
        speedOrig = agent.speed; // gives the agent speed to the float original speed for later on. 
        startingPos = transform.position;
        stoppingDistOriginal = agent.stoppingDistance;
        isDead = false;

    }


    // Update is called once per frame
    void Update()
    {
        if (!isDead)
        {
            //allows the enemy to ease into the transition animation with a tuneable
            //variable for custimization by Lerping it over time prevents choppy transitions
            float agentVel = agent.velocity.normalized.magnitude;

            anime.SetFloat("Speed", Mathf.Lerp(anime.GetFloat("Speed"), agentVel, Time.deltaTime * animeSpeedChange));

            //if the player is in range but cant be "seen" the enemy is allowed to roam
            //also if the player is not in range at all the enemy is allowed to roam
            if (playerInRange && !canSeePlayer())
            {
                StartCoroutine(roam());
            }
            else if (!playerInRange)
            {
                StartCoroutine(roam());
            }

        
        }
    }



    IEnumerator roam()
    {
        //Added this if statement to prevent spawning throwing an error when spawning enemies
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        //will allow the enemy to consulte with the nav mesh to pick a random destination that is walkable
        //that is within the roaming distance set that destinatino and walk to it
        {
            if (agent.remainingDistance < 0.05f && !destinationPicked)
            {
                destinationPicked = true;
                agent.stoppingDistance = 0;
                yield return new WaitForSeconds(roamPauseTime);

                Vector3 randomPos = Random.insideUnitSphere * roamDistance;
                randomPos += startingPos;
                NavMeshHit destination;

                if (NavMesh.SamplePosition(randomPos, out destination, roamDistance, 1))
                {
                    agent.SetDestination(destination.position);
                }

                destinationPicked = false;
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
            
            // Turn out the lights! (When the enemy dies)
            Light enemyLight = GetComponent<Light>();
            if (enemyLight != null)
            {
                enemyLight.enabled = false;
            }
           
            
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



        //tests to see if the player is within range of the enemy and if the player is within range calculate
        /* the angle of the player to the enemy 
         * there is a debug to show the angle and the player position to the enemy position in the scene screen.
         * sends out a ray cast from the head of the enemy to the player to figure out the _direction and if there is 
         * any obstacles in the way
         */
        bool canSeePlayer()
        {

           
            playerDirection = gameManager.instance.player.transform.position - headPos.position;
            angleToPlayer = Vector3.Angle(new Vector3(playerDirection.x, 0, playerDirection.z), transform.forward);

            RaycastHit hit;
            if (Physics.Raycast(headPos.position, playerDirection, out hit))
            {
                /*
                 * if the ray cast hits an object and its the player and the angle to the player is less than
                 * or equal to the preset viewing angle then tell the enemy to set the target destination to the player
                 */
                if (hit.collider.CompareTag("Player") && angleToPlayer <= viewAngle)
                {
                    agent.stoppingDistance = stoppingDistOriginal;
                    
                    if(!isAttacking)
                        agent.SetDestination(gameManager.instance.player.transform.position);
                    /*
                     * if the remaining distance is less than or equal to the stopping  distance of the enemy
                     * face the target and prepare to shoot if the angle is within parameter and the enemy is not already shooting
                     * if these are true then start to shoot
                     */
                    if (agent.remainingDistance <= agent.stoppingDistance)
                    {
                        faceTarget();
                        StartCoroutine(Melee());

                    }
                    return true;
                }
            }
            //otherwise set the stopping distance to zero and return false
           agent.stoppingDistance = 0;
            return false;
        }

        // Set the shooting bool to true then
        // places and intializes the bullet object from the shooting postion and gives it a _direction while triggering the bool that checks whether the enemy is shooting or not.
        //suspends the coroutine for the amount of seconds the shootrate is set to then sets the shooting back to false


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

        //sets the rotation of the enemy to face the player based on the player _direction to the enemy 
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
