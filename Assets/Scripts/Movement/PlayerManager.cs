using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;
    [SerializeField]
    public Vector3 playerSpeed;
    private Color originalColor;
    private Renderer rend;
    private float colorTimer = 0f;
    private bool isColorChanged = false;

    [SerializeField]
    private float moveSpeed = 20f;
    private Vector3 startPos;
    private Vector3 oldPos;
    //private float lastMoveTime = -Mathf.Infinity;

    public KeyCode[] controlKeys = new KeyCode[]
    {
        KeyCode.W,
        KeyCode.A,
        KeyCode.S,
        KeyCode.D
    };
    public Vector3 lastPosition;

    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        Debug.Log($"PlayerManager");
        lastPosition = transform.position;
        rend = GetComponent<Renderer>();

        if (rend != null && rend.material != null)
        {
            originalColor = rend.material.color;
        }
    }

    void Update()
    {
        lastPosition = transform.position;
        oldPos = transform.position;
        Move();
        if (Input.GetKeyDown(KeyCode.Space) && rend != null)
        {
            rend.material.color = Color.red;
            colorTimer = 0.25f;
            isColorChanged = true;
        }

        if (isColorChanged && rend != null)
        {
            colorTimer -= Time.deltaTime;
            if (colorTimer <= 0f)
            {
                rend.material.color = originalColor;
                isColorChanged = false;
            }
        }

    }
    void Move()
    {
        if (Input.GetKey(controlKeys[0])) // W
        {
            transform.position += Vector3.forward * moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(controlKeys[1])) // A
        {
            transform.position += Vector3.left * moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(controlKeys[2])) // S
        {
            transform.position += Vector3.back * moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(controlKeys[3])) // D
        {
            transform.position += Vector3.right * moveSpeed * Time.deltaTime;
        }
        playerSpeed = transform.position - oldPos;
    }
    // private Vector3 truncate( Vector3 steeringDir, float max_force)
    // {
    //     Vector3 f = steeringDir.normalized * max_force;
    //     return f;
    // }

    //物理模型
    // steering_force = truncate (steering_direction, max_force)
    // acceleration = steering_force / mass
    // velocity = truncate (velocity + acceleration, max_speed)
    // position = position + velocity

    //方向定義
    // new_forward = velocity.normalized
    // approximate_up = normalize (approximate_up)
    // new_side = cross (new_forward, approximate_up)
    // new_up = cross (new_forward, new_side)

    //Seek
    // target = player.transform + player.vel*time.deltatime
    // desired_velocity = (position - target).normalized * max_speed;
    // steering = desired_velocity - velocity;

    //Arrival
    // target_offset = target - position;
    // distance = length (target_offset);
    // ramped_speed = max_speed * (distance / slowing_distance);
    // clipped_speed = minimum (ramped_speed, max_speed);
    // desired_velocity = (clipped_speed / distance) * target_offset;
    // steering = desired_velocity - velocity;
}
