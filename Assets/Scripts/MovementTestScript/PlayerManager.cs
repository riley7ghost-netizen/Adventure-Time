using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;
    public static event System.Action OnInteract;

    // Reynolds Simple Vehicle Model parameters
    [SerializeField] private float _maxSpeed = 10f;
    [SerializeField] private float _maxForce = 60f;
    [SerializeField] private float _mass     = 1f;

    public Vector3 Velocity => _velocity;

    public KeyCode[] controlKeys = new KeyCode[]
    {
        KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D
    };

    private Vector3 _velocity;
    private Color   _originalColor;
    private Renderer _rend;
    private float   _colorTimer;
    private bool    _isColorChanged;

    void Awake() => Instance = this;

    void Start()
    {
        _rend = GetComponent<Renderer>();
        if (_rend != null)
            _originalColor = _rend.material.color;
    }

    void Update()
    {
        // --- Reynolds Steering (Seek toward input direction) ---
        // desired_velocity = input_direction * max_speed
        // steering         = desired_velocity - velocity          (velocity error)
        // steering_force   = truncate(steering, max_force)
        // acceleration     = steering_force / mass
        // velocity         = truncate(velocity + acceleration*dt, max_speed)
        // position         = position + velocity*dt
        Vector3 desiredVelocity = GetInputDirection() * _maxSpeed;
        Vector3 steering        = Vector3.ClampMagnitude(desiredVelocity - _velocity, _maxForce);
        Vector3 acceleration    = steering / _mass;
        _velocity               = Vector3.ClampMagnitude(_velocity + acceleration * Time.deltaTime, _maxSpeed);
        transform.position     += _velocity * Time.deltaTime;

        // --- Space: broadcast interaction event + visual flash ---
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnInteract?.Invoke();

            if (_rend != null)
            {
                _rend.material.color = Color.red;
                _colorTimer          = 0.25f;
                _isColorChanged      = true;
            }
        }

        if (_isColorChanged && _rend != null)
        {
            _colorTimer -= Time.deltaTime;
            if (_colorTimer <= 0f)
            {
                _rend.material.color = _originalColor;
                _isColorChanged      = false;
            }
        }
    }

    private Vector3 GetInputDirection()
    {
        Vector3 dir = Vector3.zero;
        if (Input.GetKey(controlKeys[0])) dir += Vector3.forward;
        if (Input.GetKey(controlKeys[1])) dir += Vector3.left;
        if (Input.GetKey(controlKeys[2])) dir += Vector3.back;
        if (Input.GetKey(controlKeys[3])) dir += Vector3.right;
        return dir.normalized;
    }
}
