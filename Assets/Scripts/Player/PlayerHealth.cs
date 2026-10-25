using UnityEngine;
using UnityEngine.SceneManagement;
using StarterAssets;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHP = 5;

    [Header("Game Over")]
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private float returnDelay = 2f;

    private int currentHP;
    private PlayerDash playerDash;
    private bool isDead;

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHP = maxHP;
        playerDash = GetComponent<PlayerDash>();
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        // 대시 중 무적
        if (playerDash != null && playerDash.IsDashing)
        {
            Debug.Log("Dash Invincible!");
            return;
        }

        if (damage <= 0)
            return;

        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0);

        Debug.Log($"Player HP: {currentHP} / {maxHP}");

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHP += amount;
        currentHP = Mathf.Min(currentHP, maxHP);

        Debug.Log($"Player HP: {currentHP} / {maxHP}");
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log("GAME OVER");

        Invoke(nameof(ReturnToLobby), returnDelay);
    }

    private void ReturnToLobby()
    {
        SceneManager.LoadScene(lobbySceneName);
    }
}