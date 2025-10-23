using UnityEngine;

public class CRManager : MonoBehaviour
{
    //Idle 參數
    [SerializeField]
    private float _moveSpeed = 1f;//移速 default 1
    [SerializeField]
    private float _turnSpeed = 60f;//轉向速度
    [SerializeField]
    private float _directionChangeInterval = 5f;//轉向間隔
    [SerializeField]
    private float _areaRadius = 5f;//限制移動範圍
    [SerializeField]
    private Vector3 _areaCenter = Vector3.zero;//中心

    [SerializeField] private Transform _playerTransform;
    [SerializeField] Rigidbody _rb;
    private bool _isChasingPlayer = false;
    private bool _isAfraidPlayer = false;

    // 玩家互動參數
    [SerializeField] private float _chaseDuration = 5f; // 追擊持續時間
    [SerializeField] private float _keyCooldown = 0.5f; // 按鍵冷卻秒數
    float _timer;
    float _nextTriggerTime = 0f;
    float _chaseFleeTime = 0f;
    float _chaseFleeDuration = 5f;
    [SerializeField] private float _directionChangeTime = 0f;
    [SerializeField] private Vector3 _direction;
    private Vector3 _velocity;
    private Vector3 _steeringDirection;
    private static float _distance = 100f;//與玩家距離
     private static float _triggerDistance = 10f;//觸發距離
    private static float _maxForce = 10f;
    private static float _minForce = 5f;
    Quaternion _targetRotation;

    [SerializeField] Vector3 targetNextPos;
    void Start()
    {
        Debug.Log($"CRManager");
        PickNewDirection();
    }

    void LateUpdate()
    {
        _timer += Time.deltaTime;

        //nextTriggerTime 是避免玩家按太快的按鍵冷卻時間
        //chaseFleeDuration 是追擊狀態維持的時間
        if (_timer > _nextTriggerTime)
        {
            _distance = Vector3.Distance(transform.position, _playerTransform.position);
            _moveSpeed = _distance / 50; //距離越遠移動越快
            TriggerByPlayer(_distance);
        }

        if ((_isChasingPlayer || _isAfraidPlayer) && _timer < (_chaseFleeTime + _chaseFleeDuration) && _playerTransform != null)
        {
            SeekOrFlee();
        }
        else
        {
            if (_timer > _directionChangeTime)
            {
                _directionChangeTime = _timer + _directionChangeInterval;
                PickNewDirection();
            }
        }

        Vector3 steering_force = Truncate(_steeringDirection);
        Vector3 acceleration = steering_force / _rb.mass;
        //_velocity = Truncate(acceleration);
        //_rb.AddForce(_force); 不好控制
        _rb.linearVelocity = acceleration;

        BounceBack();//超出範圍回彈

        // 面向方向
        if (_direction != Vector3.zero)
        {
            _targetRotation = Quaternion.LookRotation(_direction);
        }
        transform.rotation = Quaternion.RotateTowards(transform.rotation, _targetRotation, _turnSpeed * Time.deltaTime);

        Debug.Log($"targetNextPos{targetNextPos} _playerTransform.position {_playerTransform.position} _moveSpeed {_moveSpeed} ");
    }

    //方向定義
    // new_forward = velocity.normalized
    // approximate_up = normalize (approximate_up)
    // new_side = cross (new_forward, approximate_up)
    // new_up = cross (new_forward, new_side)
    void SeekOrFlee()//決定方向
    {
        //targetNextPos = Vector3.Scale(_playerTransform.position, new Vector3(1, 0, 1)) + (_playerTransform.forward * _moveSpeed * Time.deltaTime);

        Vector3 moveDir = _playerTransform.position - PlayerManager.Instance.lastPosition;
        Vector3 targetNextPos = Vector3.Scale(_playerTransform.position, new Vector3(1, 0, 1)) + moveDir;

        Vector3 desired_velocity;
        if (_isChasingPlayer)
        {
            desired_velocity = (targetNextPos - Vector3.Scale(transform.position, new Vector3(1, 0, 1))).normalized * _moveSpeed;
        }
        else
        {
            desired_velocity = (Vector3.Scale(transform.position, new Vector3(1, 0, 1)) - targetNextPos).normalized * _moveSpeed;
        }
        _steeringDirection = desired_velocity - _velocity;
    }

    void PickNewDirection()//決定方向(Idle)
    {
        _steeringDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
        _directionChangeTime = _timer + _directionChangeInterval;
    }

    void BounceBack()
    {
        Vector3 toCenter = transform.position - _areaCenter;
        float distance = toCenter.magnitude;

        if (distance > _areaRadius)
        {
            Vector3 inward = -toCenter.normalized;
            _direction = (inward + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f))).normalized;

            _directionChangeTime = _timer + _directionChangeInterval;
        }
    }

    public static Vector3 Truncate(Vector3 steeringDir) //變成將速度轉成力並給予上下限
    {
        var output = Mathf.Min(steeringDir.magnitude, _maxForce);
        output = Mathf.Max(output, _minForce);
        Vector3 f = steeringDir.normalized * output;
        return f;
    }

    void TriggerByPlayer(float distance)
    {
        if (Input.GetKeyDown(KeyCode.Space) && distance < _triggerDistance && _timer > _nextTriggerTime)
        {
            //重設觸發時間
            _nextTriggerTime = _timer + _keyCooldown;

            //隨機設定狀態Flee or Chase
            if (Random.Range(0f, 1f) < 0.2f)
            {
                _isChasingPlayer = false;
                _isAfraidPlayer = true;
                _chaseFleeTime = _chaseDuration;
            }
            else
            {
                _isChasingPlayer = true;
                _isAfraidPlayer = false;
                _chaseFleeTime = _chaseDuration;
            }
        }
    }
}
