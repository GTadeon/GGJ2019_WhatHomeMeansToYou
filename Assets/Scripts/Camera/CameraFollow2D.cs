
using UnityEngine;
using System.Collections;

/// <summary>
/// Camera follow script that allows you to implement camera follow in 2D games (x and y axis follow)
/// or in 3D games where you need follow only on x or y axis (or both). 
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CameraFollow2D : MonoBehaviour {


    public bool followXaxis;
    public bool followYaxis;
    public bool followBothAxis;

    public bool addMovementConstraints;

    public float smoothY;
    public float smoothX;

    public float offsetX;
    public float offsetY;

    public float minX;
    public float maxX;

    public float minY;
    public float maxY;

    [Tooltip("player that you want to follow")]
    public GameObject player;

    private Rigidbody2D rigidBody2D;
    private float posx; //position of this camera on x axis
    private float posy; //position of this camera on y axis


    void Awake()
    {
        Initialize();
    }


    void FixedUpdate()
    {
        Vector2 velocity = rigidBody2D.velocity;

        this.posx = Mathf.SmoothDamp(transform.position.x, player.transform.position.x, ref velocity.x, smoothX);
        this.posy = Mathf.SmoothDamp(transform.position.y, player.transform.position.y, ref velocity.y, smoothY);

        if (addMovementConstraints)
            SetMovementConstraints(posx, posy);

        if (followBothAxis)
            FollowBothAxis(posx, posy);
        else if (followXaxis)
            FollowXAxis(posx);
        else if (followYaxis)
            FollowYAxis(posy);

    }

    /// <summary>
    /// fetches needed components and deactivates gravity on camera so it doesen't fall, called on awake
    /// </summary>
    private void Initialize()
    {
        rigidBody2D = this.gameObject.GetComponent<Rigidbody2D>();
        rigidBody2D.gravityScale = 0;
    }


    /// <summary>
    /// Clamps passed smoothen camera position on x and y axis. I.E. Sets movement constraints 
    /// </summary>
    /// <param name="posx">camera smooth position x axis</param>
    /// <param name="posy">camera smooth position y axis</param
    private void SetMovementConstraints(float posx, float posy)
    {
        this.posx = Mathf.Clamp(posx, minX, maxX);
        this.posy = Mathf.Clamp(posy, minY, maxY);
    }


    /// <summary>
    /// follows player on x axis only
    /// </summary>
    private void FollowXAxis(float posx)
    {
        transform.position = new Vector3(posx + offsetX, transform.position.y, transform.position.z);
    }


    /// <summary>
    /// follows player on y axis only
    /// </summary>
    /// <param name="posy">camera smooth position y axis</param>
    private void FollowYAxis(float posy)
    {
        transform.position = new Vector3(transform.position.x, posy + offsetY, transform.position.z);
    }

    /// <summary>
    /// follows player on both axis
    /// </summary>
    /// <param name="posx">camera smooth position x axis</param>
    /// <param name="posy">camera smooth position y axis</param>
    private void FollowBothAxis(float posx, float posy)
    {
        transform.position = new Vector3(posx + offsetX, posy + offsetY, transform.position.z);
    }
}
