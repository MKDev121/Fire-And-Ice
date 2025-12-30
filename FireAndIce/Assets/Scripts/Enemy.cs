using Fusion;
using UnityEngine;
using Fusion.Addons.Physics;
using UnityEngine.Timeline;



enum EnemyState
{
    Idle,
    Move,
    Attack,

    Stunned

}
abstract class GameEnemy
{
    public float health;
    public int stunCount = 0;

    public EnemyState currentState;
    public int attacks;
    protected NetworkMecanimAnimator animator;
    protected Vector3 targetPos;
    protected Vector3 previousPos;


    protected float speed;

    public GameEnemy

        (Vector3 position, NetworkMecanimAnimator animator)
    {

        currentState = EnemyState.Idle;
        targetPos = position;
        this.animator = animator;
        previousPos = position;
    }


    public void GetPosition()
    {
        float width = Camera.main.pixelWidth;
        Vector3 ScreenCoord = Camera.main.ScreenToWorldPoint(new Vector3(width, 0, 14));

        float sign = Random.Range(-1, 2);
        Vector3 newPos = new Vector3((sign != 0 ? sign : sign + 1) * ScreenCoord.x % Random.Range(2, 16), -2.58f, 0f);
        if (newPos.x == targetPos.x)
        {
            GetPosition();
        }
        else
        {
            targetPos.x = newPos.x;
            return;
        }
    }
    public virtual Vector3 Move(Vector3 position, float delta)
    {
        if (targetPos.x != position.x)
        {
            Debug.Log(targetPos);
            position = Vector3.MoveTowards(position, targetPos, speed * delta);
            animator.Animator.SetInteger("state", 1);


        }
        else
        {
            GetPosition();

        }
        return position;
    }

    public virtual void Attack(ref bool attack) { animator.Animator.SetInteger("state", 2); }

    public void hurt(int damage)
    {
        animator.Animator.SetBool("damage", true);
        health -= damage;
    }

    public virtual void idle() { animator.Animator.SetInteger("state", 0); }
    public virtual void stunned()
    {
        if (stunCount >= 3)
        {
            currentState = EnemyState.Idle;
            stunCount = 0;

        }
        else
        {
            animator.Animator.SetInteger("state", 3);
        }
    }

}

class Troll : GameEnemy
{


    public Troll(float speed, Vector3 position, NetworkMecanimAnimator animator) : base(position, animator)
    {

        health = 100f;

        this.speed = speed;

        this.attacks = 3;

    }


    public override void Attack(ref bool attack)
    {
        Transform transform = animator.Animator.transform;
        base.Attack(ref attack);

        float distance = Mathf.Abs(Vector3.Distance(transform.position, GameObject.FindGameObjectWithTag("Player").transform.position));
        if (distance < 5f)
        {
            animator.Animator.SetInteger("attackIdx", 1);

        }

    }



}

class Watcher : GameEnemy
{
    public Watcher(float speed, Vector3 position, NetworkMecanimAnimator animator) : base(position, animator)
    {

        health = 50f;
        this.speed = speed;
        this.attacks = 3;

    }
    public override void Attack(ref bool attack)
    {
        base.Attack(ref attack);
        if (!attack)
        {
            int i = Random.Range(0, 2);

            animator.Animator.SetInteger("attackIdx", i);

            attack = true;
        }

    }


}


public class Enemy : NetworkBehaviour
{
    GameEnemy enemy;
    public int enemyType;
    public float speed;
    public bool changeState;
    public NetworkObject projectile;
    public Transform projectilePos;
    int attacks;
    int attackIndex;
    float distanceFromRight;

    public bool attack;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {

        switch (enemyType)
        {
            case 0:
                enemy = new Troll(speed, transform.position, GetComponent<NetworkMecanimAnimator>());
                break;
            case 1:
                Debug.Log(transform.position);
                float ypos = Random.Range(1, 5) * 2f + 1f;
                Vector3 spawnPos = new Vector3(Random.Range(0, 12) % 12, ypos, -.2f);
                transform.position = spawnPos;
                enemy = new Watcher(speed, transform.position, GetComponent<NetworkMecanimAnimator>());
                break;
        }
        attacks = enemy.attacks;
        distanceFromRight = transform.position.x + 20f;

    }

    // Update is called once per frame
    void Update()
    {

    }
    public override void FixedUpdateNetwork()
    {

        if (changeState)
        {
            if (enemy.currentState != EnemyState.Stunned)
            {
                enemy.currentState = (EnemyState)Random.Range(0, 3);
                changeState = false;
            }
        }
        switch (enemy.currentState)
        {
            case EnemyState.Idle:
                enemy.idle();
                break;
            case EnemyState.Move:
                float prevDist = distanceFromRight;

                transform.position = enemy.Move(transform.position, Runner.DeltaTime);
                distanceFromRight = transform.position.x + 20f;
                float dir = distanceFromRight - prevDist;

                if (dir > 0)
                    transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                else if (dir <= 0)
                    transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                break;
            case EnemyState.Attack:
                enemy.Attack(ref attack);

                break;
            case EnemyState.Stunned:
                enemy.stunned();
                break;
        }

        if (enemy.health <= 0f)
        {
            NetworkObject obj = gameObject.GetComponent<NetworkObject>();
            GameObject.Find("EnemyManager").GetComponent<EnemyManager>().removeEnemy(obj);
            Runner.Despawn(obj);
        }

    }
    public void changeAttack()
    {
        GetComponent<NetworkMecanimAnimator>().Animator.SetInteger("attackIdx", 0);
        attack = false;
        // Random.Range(0, attacks);

    }
    public void changeBool(string boolName)
    {
        GetComponent<NetworkMecanimAnimator>().Animator.SetBool("damage", false);
    }

    public void attacking()
    {
        attack = true;
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponentInParent<Player>();
            if (player.attack)
            {
                enemy.hurt(10);
                player.attack = false;
                Debug.Log(enemy.health);
            }


        }
        if (other.CompareTag("Spell"))
        {
            enemy.hurt(0);
            switch (enemy.currentState)
            {
                case EnemyState.Move:
                case EnemyState.Idle:
                    enemy.currentState = EnemyState.Idle;
                    break;
                case EnemyState.Attack:

                    enemy.currentState = EnemyState.Stunned;

                    break;

            }

        }
    }
    public void stunCountInc()
    {
        this.enemy.stunCount++;
    }
    public void spawnProjectile()
    {
        NetworkObject obj = Runner.Spawn(projectile, projectilePos.position, Quaternion.identity);
        obj.GetComponent<Rigidbody>().linearVelocity = transform.forward * 5f + transform.up * 10f;

    }
}
