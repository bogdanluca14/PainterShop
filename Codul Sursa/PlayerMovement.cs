using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public static bool canInteract = true;
    public float speed = 5f;
    public float gravity = -9.81f;
    private CharacterController controller;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        float x =
            Input.GetKey(KeyCode.A) ? 1
            : Input.GetKey(KeyCode.D) ? -1
            : 0;
        float z =
            Input.GetKey(KeyCode.S) ? 1
            : Input.GetKey(KeyCode.W) ? -1
            : 0;

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
