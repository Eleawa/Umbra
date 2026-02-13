using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController2D))]
public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    private CharacterController2D controller;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController2D>();

        controller.OnStartMoving.AddListener(OnStartMoving);
        controller.OnStopMoving.AddListener(OnStopMoving);
        controller.OnSprintEvent.AddListener(OnSprint);
        controller.OnDodge.AddListener(PlayDash);
    }

    private void Update()
    {
        Vector2 velocity = controller.GetComponent<Rigidbody2D>().linearVelocity;

        Vector2 dir = velocity.normalized;

        animator.SetFloat("MoveX", dir.x);
        animator.SetFloat("MoveY", dir.y);
    }

    private void OnStartMoving()
    {
        animator.SetBool("isWalking", true);
    }

    private void OnStopMoving()
    {
        animator.SetBool("isWalking", false);
        animator.SetBool("isSprinting", false);
    }

    private void OnSprint(bool sprinting)
    {
        animator.SetBool("isSprinting", sprinting);
    }

    public void PlayDash()
    {
        animator.SetTrigger("dash");
    }

}
