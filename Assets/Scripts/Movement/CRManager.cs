using UnityEngine;

public class CRManager : MonoBehaviour
{
    //Idle 參數
    [SerializeField] private float _moveSpeed = 1f;//移速 default 1
    [SerializeField] private float _turnSpeed = 60f;//轉向速度
    [SerializeField] private float _directionChangeInterval = 5f;//轉向間隔
    [SerializeField] private float _areaRadius = 25f;//限制移動範圍
    [SerializeField] private Vector3 _areaCenter =  new Vector3(0f, 1f, 0f);//中心

    [SerializeField] private Transform _playerTransform;
    [SerializeField] Rigidbody _rb;
    [SerializeField] private Phase _phase;

    // 玩家互動參數
    [SerializeField] private float _keyCooldown = 0.5f; // 按鍵冷卻秒數
    [SerializeField] float _timer;
    [SerializeField] float _nextTriggerTime = 0f;
    [SerializeField] float _getAffectedTime = 0f;
    [SerializeField] float _chaseFleeDuration = 5f;
    [SerializeField] private float _directionChangeTime = 0f;
    [SerializeField] private Vector3 _direction;
    [SerializeField] private Vector3 _velocity;
    [SerializeField] private Vector3 _steeringDirection;
    [SerializeField] private float _distance = 100f;//與玩家距離
    [SerializeField] private float _triggerDistance = 20f;//觸發距離
    [SerializeField] private float _maxForce = 10f;
    [SerializeField] private float _minForce = 5f;
    Quaternion _targetRotation;

    [SerializeField] Vector3 targetPos;
    void Start()
    {
        _phase = Phase.idle;
        PickNewDirection();
    }

    void Update()
    {
        _timer += Time.deltaTime;
        _distance = Vector3.Distance(transform.position, _playerTransform.position);


        //nextTriggerTime 是避免玩家按太快的按鍵冷卻時間
        //chaseFleeDuration 是追擊狀態維持的時間
        if (_timer > _nextTriggerTime)
        {
            if (Input.GetKeyDown(KeyCode.Space) && _distance < _triggerDistance)
            {
                _moveSpeed = _distance / 10; //距離越遠移動越快
                SetCRState();
            }
        }

        if ((_phase != Phase.idle) && _timer < (_getAffectedTime + _chaseFleeDuration) && _playerTransform != null)
        {
            SeekOrFlee();
        }
        else
        {
            if (_phase != Phase.idle)
            {
                _phase = Phase.idle;
            }
            if (_timer > _directionChangeTime)
            {
                _directionChangeTime = _timer + _directionChangeInterval;
                PickNewDirection();
            }
        }

        // 面向方向
        if (_steeringDirection != Vector3.zero)
        {
            _targetRotation = Quaternion.LookRotation(_steeringDirection);
        }
        transform.rotation = Quaternion.RotateTowards(transform.rotation, _targetRotation, _turnSpeed * Time.deltaTime);

        Vector3 steering_force = Truncate(_steeringDirection);
        Vector3 acceleration = steering_force / _rb.mass;
        _velocity = acceleration;
        //_rb.AddForce(_force); 不好控制
        _rb.linearVelocity = acceleration;

        Vector3 toCenter = _areaCenter - transform.position;
        if (toCenter.magnitude > _areaRadius)
        {
            _steeringDirection = toCenter.normalized;//超出範圍回彈
        }

        Debug.Log($"targetNextPos{targetPos} _playerTransform.position {_playerTransform.position} _moveSpeed {_moveSpeed} ");
    }

    void SetCRState()
    {
        //重設觸發時間
        _nextTriggerTime = _timer + _keyCooldown;

        //隨機設定狀態Flee or Chase
        if (Random.Range(0f, 1f) < 0.2f)
        {
            _phase = Phase.flee;
            _getAffectedTime = _timer;
        }
        else
        {
            _phase = Phase.chase;
            _getAffectedTime = _timer;
        }
    }

    //方向定義
    // new_forward = velocity.normalized
    // approximate_up = normalize (approximate_up)
    // new_side = cross (new_forward, approximate_up)
    // new_up = cross (new_forward, new_side)
    void SeekOrFlee()//給normalized vector3* _moveSpeed
    {
        //Vector3 moveDir = PlayerManager.Instance.playerSpeed;
        targetPos = Vector3.Scale(_playerTransform.position, new Vector3(1, 0, 1));

        Vector3 desired_velocity;
        if (_phase == Phase.chase)
        {
            desired_velocity = Flatten(targetPos - Vector3.Scale(transform.position, new Vector3(1, 0, 1))).normalized * _moveSpeed;
        }
        else
        {
            desired_velocity = Flatten(Vector3.Scale(transform.position, new Vector3(1, 0, 1)) - targetPos).normalized * _moveSpeed;
        }
        _steeringDirection = desired_velocity + _velocity;
    }

    void PickNewDirection()//給normalized vector3* _moveSpeed
    {
        _steeringDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized * 1; //1=_moveSpeed
        _directionChangeTime = _timer + _directionChangeInterval;
    }

    public Vector3 Truncate(Vector3 steeringDir) //變成將速度轉成力並給予上下限
    {
        var output = Mathf.Min(steeringDir.magnitude, _maxForce);
        output = Mathf.Max(output, _minForce);
        Vector3 f = steeringDir.normalized * output;
        return f;
    }
    public enum Phase
    {
        flee,
        chase,
        idle
    }

    static Vector3 Flatten(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }
}
