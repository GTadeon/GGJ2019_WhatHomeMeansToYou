using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerControllerHorizontal : MonoBehaviour
{

    public float MovementSpeed;

    private Rigidbody2D rb;


    void Start()
    {
        rb = this.gameObject.GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (GGJGameMananager.CanControlPlayer)
        {
            float mov = Input.GetAxis("Horizontal");
            rb.velocity = new Vector2(mov * MovementSpeed, rb.velocity.y);
        }
    }


}