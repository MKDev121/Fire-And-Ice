using Fusion;
using UnityEngine;
using Fusion.Addons.Physics;
using UnityEngine.Timeline;
using System;

enum player_state
{
    Jumping = 0,
    Falling = 1,

}
public class Player : NetworkBehaviour
{
    public float speed = 5f;
    public float jumpForce = 5f;
    [Networked] public int health { get; set; }
    private Rigidbody rb;
    [Networked] int charIndex { get; set; }
    public int index;
    [Networked] private float Moving { get; set; }
    [Networked] public NetworkButtons buttonsPrevious { get; set; }

    private NetworkMecanimAnimator _networkAnimator;

    float jumpVelocity;
    [Networked] public bool grounded { get; set; }


    [Networked] bool onFloor { get; set; }
    [Networked] public bool attack { get; set; }

    bool spawned { get; set; }

    public LayerMask platformLayer;
    public GameObject spell;
    public Transform spellPos;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        _networkAnimator = GetComponentInChildren<NetworkMecanimAnimator>();
        platformLayer |= 1 << 6;


    }
    void Update()
    {
        if (!spawned)
        {
            if (Runner.IsServer)
            {
                charIndex = index;
            }
            GameObject obj = transform.GetChild(charIndex).gameObject;
            obj.SetActive(true);
            _networkAnimator.Animator.runtimeAnimatorController = obj.GetComponent<characterClass>().animator;
            _networkAnimator.Animator.avatar = obj.GetComponent<characterClass>().avatar;
            spawned = true;

        }
        if (health <= 0)
        {
            GameObject.Find("Canvas").transform.GetChild(0).gameObject.SetActive(true);

            _networkAnimator.Animator.SetBool("Dead", true);
        }
    }
    public override void FixedUpdateNetwork()
    {
        if (rb.linearVelocity.y > 0)
        {

            rb.excludeLayers = platformLayer;
        }
        else
        {
            LayerMask temp = new LayerMask();
            temp |= 1 << 6;
            rb.excludeLayers = temp;
        }

        if (GetInput(out NetworkInputData data) && health > 0)
        {
            Moving = Mathf.Abs(data.direction);
            int sign = (int)data.direction;

            if (sign > 0)
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            else if (sign < 0)
                transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            var pressed = data.buttons.GetPressed(buttonsPrevious);
            buttonsPrevious = data.buttons;
            if (_networkAnimator.Animator.GetBool("Jump"))
                _networkAnimator.Animator.SetBool("Jump", false);
            if (_networkAnimator.Animator.GetBool("Attack"))
                _networkAnimator.Animator.SetBool("Attack", false);
            if (pressed.IsSet(MyButtons.Jump) && grounded && !_networkAnimator.Animator.GetBool("Hurt"))
            {
                jumpVelocity = jumpForce;
                rb.linearVelocity += rb.transform.up * jumpVelocity;
                _networkAnimator.Animator.SetBool("Jump", true);

            }
            if (pressed.IsSet(MyButtons.Attack) && !_networkAnimator.Animator.GetBool("Hurt"))
            {
                _networkAnimator.Animator.SetBool("Attack", true);
                attack = true;
            }

            // Preserve gravity (Y axis) while applying horizontal velocity
            var currentVel = rb.linearVelocity;
            int moveVal = _networkAnimator.Animator.GetBool("Hurt") || attack ? 0 : 1;
            rb.linearVelocity = new Vector3(data.direction * speed * moveVal, currentVel.y, 0f);


            _networkAnimator.Animator.SetFloat("Moving", Moving);
            _networkAnimator.Animator.SetBool("Grounded", grounded);




        }




    }
    void OnCollisionEnter(Collision collision)
    {

        grounded = collision.gameObject.tag == "Ground";



    }
    void OnCollisionExit(Collision collision)
    {


        if (collision.gameObject.tag == "Ground")
        {
            grounded = false;

        }


    }
    void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Enemy") && charIndex == 0)
        {
            EnemyObject obj = other.GetComponent<EnemyObject>();
            switch (obj.objectType)
            {
                case ObjectType.Weapon:
                    Enemy enemy = other.GetComponentInParent<Enemy>();
                    if (enemy.attack)
                    {
                        _networkAnimator.Animator.SetBool("Hurt", true);
                    }
                    break;
                case ObjectType.Projectile:
                    _networkAnimator.Animator.SetBool("Hurt", true);
                    break;
            }
            health -= obj.damage;
        }
    }
    public void attackFinish()
    {

        attack = false;
    }
    public void spellCast()
    {

        NetworkObject obj = Runner.Spawn(spell, spellPos.position, Quaternion.identity);
        obj.GetComponent<Rigidbody>().linearVelocity = transform.right * 30f;

    }
    public void changeBool(string boolName)
    {
        var animator = _networkAnimator.Animator;
        animator.SetBool(boolName, !animator.GetBool(boolName));
    }
    public async void ReloadScene()
    {

    }
}
