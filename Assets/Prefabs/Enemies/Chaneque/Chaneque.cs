using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Chaneque : MonoBehaviour
{
    //for the movement of the Chaneque Monster
    // can move around the entire map, nearly as fast as the player but player can lose it chase sequence
    // can be stunned while the player shouts at it, but from a very close range, leading to possible change of target from one player to another
    // can be killed by the player giving it a very specific item after it has been stunned. 
    //if it get close enough to a player it will cast a spell that will shrink the player and allow them to be picked up by other players
    //along with adjustments for size, player movements, and voice pitch will be affected. 
    //if possible allow the player to be thrown by another player. (possibly stun the monster when player is thrown and hits monster) 

    [Range(0, 25)][SerializeField] public float speed = 10f;
    [Range(0, 25)][SerializeField] public float sightRange = 10f;
    [Range(0, 25)][SerializeField] float attackRange;
    [Range(0, 25)][SerializeField] float timeBetweenAttacks;
    [Range(0, 20)][SerializeField] private int damage; //the amount of damage that the enemy deals
    public Rigidbody monsterRig;
    public GameObject itemToRecieve;
    private NavMeshAgent chanequeNavMesh;
    private Transform playerPos;
    public GameObject player;
    private Vector3 playerStartSize = new(1f, 1f, 1f);
    public bool playerShrunk = false;
    private bool isAttacking = false; //is the enemy currently attacking the player?
    public bool isStunned = false;
    private Animator animator;

    // what other variable will I need to affect the abilities of this monster? 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //get the navMeshAgent for the enemy and the location of the player for the monster to be able to see and potientaly follow/attack player. 
        chanequeNavMesh = GetComponent<NavMeshAgent>();
        playerPos = GameObject.FindWithTag("Player").transform;
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        //to get the distane from the player and the enemy
        float distanceFromPlayer = Vector3.Distance(playerPos.position, this.transform.position);
        //is the player within sight range but outside the attack range
        if (distanceFromPlayer <= sightRange && distanceFromPlayer > attackRange && !PlayerHealth.isDead)
        {
            isAttacking = false;
            chanequeNavMesh.isStopped = false;
            StopAllCoroutines();
            ChasePlayer();
        }
        else if (distanceFromPlayer <= attackRange)//&& !isAttacking && !PlayerHealth.isDead)
        {
            chanequeNavMesh.isStopped = true; // stop the enemy from moving
            Debug.Log($"Monster is suppose to be attacking the player now!");
            animator.SetBool("Walk", true);
            StartCoroutine(AttackPlayer());
        }

        if (PlayerHealth.isDead)
        {
            //to stop the enemy from attacking the player after the player is dead. 
            chanequeNavMesh.isStopped = true;
        }
    }

    private void ChasePlayer()
    {
        chanequeNavMesh.SetDestination(playerPos.position); //set the enemy destination to the player
        //handle animations and state machine logic here as well 
        animator.SetBool("Run", true);
    }

    private IEnumerator AttackPlayer()
    {
        //need to update this so that the monsters 1st attack shrinks the player, then add checks so that if the player is shrunk then it will take damage. 
        if (player.transform.localScale == playerStartSize)
        {
            playerShrunk = true;
            player.transform.localScale /= 1.75f;
            animator.SetBool("ShrinkPlayer", true);
            Debug.Log("Player has shrunk");
        }
        else if (player.transform.localScale != playerStartSize)
        {
            Debug.Log("Player has been shrunk and is taking damage from monster.");
            isAttacking = true;
            animator.SetBool("Run", false);
            yield return new WaitForSeconds(timeBetweenAttacks); //will wait between attacks, to deal damage to the player 
            FindAnyObjectByType<PlayerHealth>().TakeDamage(damage);
            Debug.Log($"monster dealt {damage} to the player.");
            animator.SetBool("Kick", true);
            isAttacking = false;
        }
    }
    public void Stunned()
    {
        //run a if statement to check if the player shouted loud enough to stunn the chaneque. if they did set isStunned to true, else leave false
        isStunned = true;
        animator.SetBool("Stun", true);
        if (isStunned == true)
        {
            //start the stunned animation and give a timer for the player to give the item. 
        }
    }

    public void IsAppeased()
    {
        if (itemToRecieve == true)
        {
            //run the appease animation and remove from the level. 
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(this.transform.position, sightRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.transform.position, attackRange);
    }
}
