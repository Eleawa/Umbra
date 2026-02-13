using UnityEngine;

public class SwordController : MonoBehaviour
{
    [SerializeField] private Transform swordPivot;
    [SerializeField] private CharacterController2D controller;

    private void Update()
    {
        Vector2 dir = controller.GetFacingDirection();

        if (dir.sqrMagnitude < 0.1f)
            return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        swordPivot.rotation = Quaternion.Euler(0, 0, angle);
    }
}
