using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private int damage = 1;
    private bool movingRight;
    
    public void Initialize(bool shootRight)
    {
        movingRight = shootRight;
        // Flip sprite if shooting left
        if (!shootRight)
        {
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        float direction = movingRight ? 1f : -1f;
        transform.Translate(Vector2.right * direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponent<EnemyPatrol>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
        else if (other.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}