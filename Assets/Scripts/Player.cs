using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Controller2D))]
[RequireComponent(typeof(Animator))]
public class Player : MonoBehaviour
{
    [SerializeField] private float minJumpHeight = 0.5f;
    [SerializeField] private float jumpHeight = 4.0f;
    [SerializeField] private float timeToJumpApex = 0.4f;
    [SerializeField] private float fallGravityMultiplier = 2.0f;
    [SerializeField] private float moveSpeed = 6.0f;

    float gravity;
    float jumpReleaseGravity;
    float fallingGravity;
    float jumpVelocity;

    float accelerationTimeAirbourne = 0.2f;
    float accelerationTimeGrounded = 0.1f;
    float velocityxSmoothing;
    private float targetVelocityX;

    Vector2 directionalInput;
    public int wallDirX;

    //Fast falling variables
    private bool risingJump = false;
    private bool reachedApex = true;
    private float maxHeightReached = Mathf.NegativeInfinity;

    //Wall sliding variables
    [SerializeField] private bool isWallSliding;
    public float wallSpeedSlideMax = 3;
    private float wallStickTime = .15f;
    float timeToWallUnstick;
    [SerializeField] private Vector2 wallJumpClimb;
    [SerializeField] private Vector2 wallJumpNeutral;
    [SerializeField] private Vector2 wallJumpLeap;

    bool isJump = false;
    Vector2 jumpForce;

    bool initAttack = false;
    bool isAttacking = false;

    [SerializeField] Vector3 velocity;

    Controller2D controller;
    Animator animator;

    void Start()
    {
        controller = GetComponent<Controller2D>();
        animator = GetComponent<Animator>();

        gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        jumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        Debug.Log("Gravity: " + gravity + ", Jump Velocity: " + jumpVelocity);

        jumpReleaseGravity = (-1 * jumpVelocity * jumpVelocity) / (2.0f * minJumpHeight);
        fallingGravity = gravity * fallGravityMultiplier;
        Debug.Log("Jump Release Gravity = " + jumpReleaseGravity + ", Falling Gravity = " + fallingGravity);
    }

    public void SetDirectionalInput(Vector2 input)
    {
        directionalInput = input;
    }
    public Vector2 GetDirectionalInput()
    {
        return directionalInput;
    }

    void Update()
    {
        isWallSliding = false;
        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below)
        {
            isWallSliding = true;
        }

            HandleAnimations();
           
        if (!reachedApex && maxHeightReached > transform.position.y)
        {
            risingJump = false;
            reachedApex = true;
            gravity = fallingGravity;
        }
        maxHeightReached = Mathf.Max(transform.position.y, maxHeightReached);
    }

    private void FixedUpdate()
    {
        if (isAttacking && controller.collisions.below)
        {
            directionalInput = new Vector2(0, 0);
        }
        targetVelocityX = directionalInput.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x,
                                targetVelocityX,
                                ref velocityxSmoothing,
                                (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirbourne);

        if (isWallSliding == true)
        {
            if (timeToWallUnstick > 0)
            {
                int wallDirX = (controller.collisions.left) ? -1 : 1; // y
                float inputX = Input.GetAxisRaw("Horizontal");
                velocity.x = 0;
                velocityxSmoothing = 0;
                if (inputX != wallDirX && inputX != 0)
                    timeToWallUnstick -= Time.fixedDeltaTime;
                else
                    timeToWallUnstick = wallStickTime;
            }
            else
            {
                timeToWallUnstick = wallStickTime;
            }
        }

        if (isJump)
        {
            Jump(jumpForce);
            isJump = false;
        }


        //Using Verlet integration
        Vector3 acc = new Vector3(0.0f, gravity, 0.0f);
        Vector3 deltaPosition = (velocity * Time.fixedDeltaTime) + (0.5f * acc * Time.fixedDeltaTime * Time.fixedDeltaTime);
        controller.Move(deltaPosition);

        velocity.y += gravity * Time.fixedDeltaTime;
        //Hard coded terminal velocity
        if (velocity.y < fallingGravity * 0.5f)
        {
            velocity.y = fallingGravity * 0.5f;
        }
        if (isWallSliding && velocity.y < -wallSpeedSlideMax)
        {
            velocity.y = -wallSpeedSlideMax;
        }
        if (controller.collisions.above || controller.collisions.below)
        {
            velocity.y = 0;
        }
        if (controller.collisions.left || controller.collisions.right)
        {
            velocity.x = 0;
        }
    }

    void HandleAnimations()
    {
        animator.SetFloat("HorizontalSpeed", Mathf.Abs(velocity.x));

        //If running, change the animation speed in relation to the horizontal speed of the character
        if (controller.collisions.below && Mathf.Abs(velocity.x) > 0.01
             &&  animator.GetCurrentAnimatorStateInfo(0).IsName("Player_Run"))
            animator.speed = ((Mathf.Abs(velocity.x) / moveSpeed) + 1.0f) / 2.0f;
        else
            animator.speed = 1;


        if (isWallSliding)
            animator.SetBool("IsWallGrab", true);
        else
            animator.SetBool("IsWallGrab", false);


        if (initAttack)
        {
            initAttack = false;
            isAttacking = true;
            animator.SetBool("IsAttacking", true);

            StartCoroutine(Attacking());
        }
        if (!isAttacking)
            animator.SetBool("IsAttacking", false);



        if ((!controller.collisions.below && !isWallSliding))
        {
            animator.SetBool("IsJumping", true);
            if (!isAttacking)
            {
                int clampVal = 20;
                //Value between 0 and 1 which selects the correct point of the jump animation
                float jumpAnimValue = ((Mathf.Clamp(-velocity.y, -clampVal, clampVal)) + (float)clampVal) / (2.0f * (float)clampVal);
                animator.Play("Player_Jump", 0, jumpAnimValue);
            }
        }
        else
            animator.SetBool("IsJumping", false);
    }

    IEnumerator Attacking()
    {
        while ((animator.GetCurrentAnimatorStateInfo(0).IsName("Player_Attack") ||
                 animator.GetCurrentAnimatorStateInfo(0).IsName("Player_JumpAttack")) &&
                animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Player_JumpAttack")
                 && controller.collisions.below)
            {
                isAttacking = false;
            }
            yield return null;
        }

        isAttacking = false;
        animator.SetBool("IsAttacking", false);
    }
    public bool OnJumpInputDown()
    {
        if (isWallSliding)
        {
            wallDirX = (controller.collisions.left) ? -1 : 1;
            //Wall Jump
            if (wallDirX == directionalInput.x)
            {
                isJump = true;
                jumpForce = new Vector2(-wallDirX * wallJumpClimb.x, wallJumpClimb.y);
            }
            else if (directionalInput.x == 0)
            {
                isJump = true;
                jumpForce = new Vector2(-wallDirX * wallJumpNeutral.x, wallJumpNeutral.y);
            }
            else
            {
                isJump = true;
                jumpForce = new Vector2(-wallDirX * wallJumpLeap.x, wallJumpLeap.y);
            }
            return true;
        }
        if (controller.collisions.below)
        {
            // jump
            isJump = true;
            jumpForce = new Vector2(velocity.x, jumpVelocity);
            return true;
        }
        // Returns false if not in a position to jump
        return false;
    }

    public void OnJumpInputUp()
    {
        if (risingJump)
        {
            gravity = jumpReleaseGravity;
        }
    }

    public void OnAttackInputDown()
    {
        if (controller.collisions.below)
        {
            animator.Play("Player_Attack");
            initAttack = true;
        }
        else if (!isWallSliding)
        {
            animator.Play("Player_JumpAttack");
            initAttack = true;
        }
    }

    private void Jump(Vector2 jumpForce)
    {
        velocity = jumpForce;
        gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        risingJump = true;
        reachedApex = false;
        maxHeightReached = Mathf.NegativeInfinity;
    }
}

