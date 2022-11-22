using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VanguardMovement : NetworkBehaviour
{
    NetworkCharacterControllerPrototype controller;

    public Transform groundCheck;

    public LayerMask groundMask;

    public Animator titanAnimator;

    bool isRunning;
    public bool isWalking;
    public bool isSprinting;
    public bool isDashing;
    bool isGrounded;
    public bool isMoving;

    float speed;
    public float walkSpeed;
    public float runSpeed;
    public float sprintSpeed;
    public float normalGravity;

    Vector3 input;
    Vector3 move;
    Vector3 forwardDirection;
    Vector3 Yvelocity;

    EnterVanguardTitan enterScript;

    // Start is called before the first frame update
    void Awake()
    {
        controller = GetComponent<NetworkCharacterControllerPrototype>();
        enterScript = GetComponent<EnterVanguardTitan>();
    }

    void HandleInput()
    {
        if (GetInput(out NetworkInputData data))
        {
            data.direction.Normalize();
            input = data.direction;

            titanAnimator.SetFloat("moveX", input.x, 0.1f, Time.deltaTime);
            titanAnimator.SetFloat("moveZ", input.z, 0.1f, Time.deltaTime);

            if (data.isSprinting && !isWalking)
            {
                isSprinting = true;
            }

            if (!data.isSprinting)
            {
                isSprinting = false;
            }

            if (data.isAiming && !isSprinting)
            {
                isWalking = true;
            }

            if (!data.isAiming)
            {
                isWalking = false;
            }

            if (data.isDashing)
            {
                StartCoroutine(HandleDash());
            }
        }
    }

    // Update is called once per frame
    public override void FixedUpdateNetwork()
    {
        if (!HasInputAuthority) return;

        if (enterScript != null && enterScript.inTitan)
        {
            HandleInput();
            CheckSprinting();
            CheckRunning();
            if (isDashing)
            {
                Dash();
            }
            else if (isGrounded)
            {
                Movement();
            }
            else if (!isGrounded)
            {
                AirMovement();
            }

            controller.Move(move * Time.deltaTime);
            CheckGround();
            HandleAnimation();
        }
    }

    void CheckGround()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, 0.2f, groundMask);
        if (!isGrounded)
        {
            ApplyGravity();
        }
    }

    void CheckSprinting()
    {
        if (isSprinting)
        {
            if (Vector3.Dot(input, transform.forward) < 0.1)
            {
                isSprinting = false;
            }
        }
    }

    void CheckRunning()
    {
        if (!isSprinting && !isWalking && isMoving)
        {
            isRunning = true;
        }
        else
        {
            isRunning = false;
        }
    }

    void Movement()
    {
        speed = isSprinting ? sprintSpeed : isWalking ? walkSpeed : runSpeed;
        if (input.x != 0)
        {
            move.x += input.x * speed;
        }
        else
        {
            move.x = 0;
        }

        if (input.z != 0)
        {
            move.z += input.z * speed;
        }
        else
        {
            move.z = 0;
        }

        if (input == new Vector3(0f, 0f, 0f))
        {
            isMoving = false;
        }
        else
        {
            isMoving = true;
        }

        move = Vector3.ClampMagnitude(move, speed);
    }

    void AirMovement()
    {
        move.x += input.x * 1f;
        move.z += input.z * 1f;

        move = Vector3.ClampMagnitude(move, speed);
    }

    void Dash()
    {
        move += forwardDirection;
        move = Vector3.ClampMagnitude(move, speed);
    }

    IEnumerator HandleDash()
    {
        titanAnimator.SetTrigger("dodge");
        speed += 27f;
        forwardDirection = input;
        isDashing = true;

        yield return new WaitForSeconds(0.6f);

        isDashing = false;
    }

    void HandleAnimation()
    {
        titanAnimator.SetBool("run", isRunning);
        titanAnimator.SetBool("walk", isWalking);
        titanAnimator.SetBool("sprint", isSprinting);
    }

    void ApplyGravity()
    {
        Yvelocity.y += normalGravity * Time.deltaTime;
        controller.Move(Yvelocity * Time.deltaTime);
    }
}