using UnityEngine;

public class CRManager : MonoBehaviour
{
    // --- Reynolds Simple Vehicle Model ---
    [SerializeField] private float _maxSpeed = 3f;
    [SerializeField] private float _maxForce = 10f;
    [SerializeField] private float _mass     = 1f;
    [SerializeField] private float _turnSpeed = 120f;

    // --- Area Boundary ---
    [SerializeField] private float   _areaRadius = 25f;
    [SerializeField] private Vector3 _areaCenter = new Vector3(0f, 1f, 0f);

    // --- Interaction ---
    [SerializeField] private float _triggerDistance  = 20f;
    [SerializeField] private float _keyCooldown      = 0.5f;
    [SerializeField] private float _chaseFleeDuration = 5f;

    // --- Wander (Reynolds sphere-projection method) ---
    [SerializeField] private float _wanderDistance = 2f;    // sphere center distance ahead
    [SerializeField] private float _wanderRadius   = 1.5f;  // sphere radius
    [SerializeField] private float _wanderJitter   = 40f;   // degrees/sec angular jitter

    // Inspector-visible state (read-only during play)
    [SerializeField] private Phase _phase;

    [SerializeField] private Rigidbody _rb;

    private Transform _playerTransform;
    private Vector3   _velocity;
    private float     _timer;
    private float     _nextTriggerTime;
    private float     _getAffectedTime;
    private float     _wanderAngle;
    private float     _currentMaxSpeed;

    public enum Phase { idle, chase, flee }

    // -------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------

    void OnEnable()  => PlayerManager.OnInteract += HandleInteract;
    void OnDisable() => PlayerManager.OnInteract -= HandleInteract;

    void Start()
    {
        _playerTransform = PlayerManager.Instance.transform;
        _phase           = Phase.idle;
        _currentMaxSpeed = _maxSpeed;
        _wanderAngle     = Random.Range(0f, 360f);
    }

    void Update()
    {
        _timer += Time.deltaTime;

        // Return to idle when chase/flee duration expires
        if (_phase != Phase.idle && _timer > _getAffectedTime + _chaseFleeDuration)
        {
            _phase           = Phase.idle;
            _currentMaxSpeed = _maxSpeed;
        }

        // --- Compute steering direction from current behavior ---
        Vector3 steeringDir = _phase == Phase.idle ? Wander() : SeekOrFlee();

        // --- Boundary containment: override steering if outside area ---
        Vector3 toCenter = _areaCenter - transform.position;
        if (toCenter.magnitude > _areaRadius)
            steeringDir = toCenter.normalized * _maxForce;

        // --- Reynolds physics integration ---
        // steering_force = truncate(steering_direction, max_force)
        // acceleration   = steering_force / mass
        // velocity       = truncate(velocity + acceleration * dt, max_speed)
        Vector3 steeringForce = Vector3.ClampMagnitude(steeringDir, _maxForce);
        Vector3 acceleration  = steeringForce / _mass;
        _velocity             = Vector3.ClampMagnitude(_velocity + acceleration * Time.deltaTime, _currentMaxSpeed);
        _rb.linearVelocity    = _velocity;

        // --- Velocity-aligned orientation ---
        if (_velocity.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(_velocity);
            transform.rotation  = Quaternion.RotateTowards(transform.rotation, targetRot, _turnSpeed * Time.deltaTime);
        }
    }

    // -------------------------------------------------------
    // Interaction (triggered by PlayerManager.OnInteract event)
    // -------------------------------------------------------

    void HandleInteract()
    {
        if (_timer < _nextTriggerTime) return;

        float distance = Vector3.Distance(transform.position, _playerTransform.position);
        if (distance >= _triggerDistance) return;

        _nextTriggerTime = _timer + _keyCooldown;
        _getAffectedTime = _timer;
        // Speed scales with distance: farther away = faster response
        _currentMaxSpeed = distance / 10f;
        _phase           = Random.value < 0.2f ? Phase.flee : Phase.chase;
    }

    // -------------------------------------------------------
    // Steering behaviors
    // -------------------------------------------------------

    // Seek / Flee  (Reynolds formula: steering = desired_velocity - current_velocity)
    Vector3 SeekOrFlee()
    {
        Vector3 flatSelf   = Flatten(transform.position);
        Vector3 flatTarget = Flatten(_playerTransform.position);

        Vector3 toTarget       = flatTarget - flatSelf;
        Vector3 desiredVelocity = _phase == Phase.chase
            ? toTarget.normalized  * _currentMaxSpeed
            : -toTarget.normalized * _currentMaxSpeed;

        return desiredVelocity - Flatten(_velocity);
    }

    // Wander  (Reynolds sphere-projection method)
    // A point is constrained to a sphere slightly ahead of the agent.
    // Each frame a small random angular displacement is added, producing a
    // smooth "random walk" in steering direction.
    Vector3 Wander()
    {
        _wanderAngle += Random.Range(-_wanderJitter, _wanderJitter) * Time.deltaTime;

        Vector3 forward      = _velocity.magnitude > 0.01f
            ? Flatten(_velocity).normalized
            : transform.forward;

        Vector3 circleCenter = forward * _wanderDistance;
        float   angleRad     = _wanderAngle * Mathf.Deg2Rad;
        Vector3 displacement = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad)) * _wanderRadius;

        return circleCenter + displacement;
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------

    static Vector3 Flatten(Vector3 v) => new Vector3(v.x, 0f, v.z);
}
