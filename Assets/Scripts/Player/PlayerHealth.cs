using System;
using UnityEngine;
using StarterAssets;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHP = 5;

    [Header("Game Over")]
    [SerializeField] private string lobbySceneName = "Lobby";

    private int currentHP;
    private PlayerDash playerDash;
    private bool isDead;

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public bool IsDead => isDead;
    public string LobbySceneName => lobbySceneName;

    public event Action<int, int> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        maxHP = Mathf.Max(1, maxHP);
        currentHP = maxHP;
        playerDash = GetComponent<PlayerDash>();
    }

    private void Start()
    {
        GameUI gameUI = GetComponent<GameUI>();
        if (gameUI == null)
            gameUI = gameObject.AddComponent<GameUI>();

        gameUI.Initialize(this);
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
            return;

        if (playerDash != null && playerDash.IsDashing)
            return;

        currentHP = Mathf.Max(currentHP - damage, 0);
        HealthChanged?.Invoke(currentHP, maxHP);

        if (currentHP == 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHP += Mathf.Min(amount, maxHP - currentHP);
        HealthChanged?.Invoke(currentHP, maxHP);
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        Died?.Invoke();
    }
}
