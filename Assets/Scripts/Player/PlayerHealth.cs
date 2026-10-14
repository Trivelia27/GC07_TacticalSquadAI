using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    private int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0;

    // Dipicu setiap Player terkena tembakan (dipakai efek layar merah).
    public event System.Action<int> Damaged;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0)
            return;

        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);

        Damaged?.Invoke(damage);

        if (currentHealth <= 0)
            Debug.Log("Player Dead");
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
    }
}
