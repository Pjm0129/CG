using UnityEngine;

namespace StarterAssets
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private int maxHP = 5;

        private int currentHP;
        private PlayerDash playerDash;

        public int CurrentHP => currentHP;
        public int MaxHP => maxHP;

        private void Awake()
        {
            currentHP = maxHP;
            playerDash = GetComponent<PlayerDash>();
        }

        public void TakeDamage(int damage)
        {
            // 대시 중 무적
            if (playerDash != null && playerDash.IsDashing)
            {
                Debug.Log("Dash Invincible!");
                return;
            }

            currentHP -= damage;
            currentHP = Mathf.Max(currentHP, 0);

            Debug.Log($"Player HP: {currentHP} / {maxHP}");

            if (currentHP <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log("Player Dead");
        }
    }
}