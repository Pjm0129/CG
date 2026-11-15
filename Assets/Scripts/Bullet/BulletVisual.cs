using UnityEngine;

public class BulletVisual : MonoBehaviour
{
    [Header("Renderers")]
    [Tooltip("비워두면 Bullet 아래의 Renderer를 자동으로 찾습니다.")]
    [SerializeField] private Renderer[] renderers;

    [Header("Particle Systems")]
    [Tooltip("비워두면 Bullet 아래의 ParticleSystem을 자동으로 찾습니다.")]
    [SerializeField] private ParticleSystem[] particleSystems;


    private void Awake()
    {
        CacheComponents();
    }


    private void CacheComponents()
    {
        if (renderers == null || renderers.Length == 0)
        {
            renderers =
                GetComponentsInChildren<Renderer>(true);
        }


        if (particleSystems == null ||
            particleSystems.Length == 0)
        {
            particleSystems =
                GetComponentsInChildren<ParticleSystem>(true);
        }
    }


    public void ApplyVisual(
        Material material,
        Color color)
    {
        CacheComponents();


        // ============================================
        // Material 적용
        // ============================================

        if (material != null)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                renderer.sharedMaterial = material;
            }
        }


        // ============================================
        // Particle 색상 적용
        // ============================================

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            if (particleSystem == null)
                continue;


            ParticleSystem.MainModule main =
                particleSystem.main;


            main.startColor = color;


            // 이미 살아있는 Particle도 색 변경
            ParticleSystem.Particle[] particles =
                new ParticleSystem.Particle[
                    particleSystem.main.maxParticles
                ];


            int count =
                particleSystem.GetParticles(particles);


            for (int i = 0; i < count; i++)
            {
                particles[i].startColor = color;
            }


            particleSystem.SetParticles(
                particles,
                count
            );
        }
    }
}