using UnityEngine;

public class MissionSpot : MonoBehaviour
{
    [SerializeField] float _timer = 0;
    [SerializeField] float _completeTime = 20f;
    [SerializeField] bool _isComplete = false;
    [SerializeField] private Renderer rend;
    [SerializeField] private Collider coll;
    bool _playerInside = false;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }
    void Update()
    {
        if (_playerInside)
        {
           if (_timer < _completeTime)
            {
                _timer += Time.deltaTime;
                rend.material.color = Color.green;
            }
        }
        if (!_isComplete && _timer > _completeTime)
        {
            _isComplete = true;
            rend.material.color = Color.black;
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")&& coll.enabled)
        {
            _playerInside = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")&& coll.enabled)
        {
            _playerInside = false;
        }
    }
}
