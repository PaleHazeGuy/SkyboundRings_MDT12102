using UnityEngine;

namespace Gameplay
{
  public class Ring : MonoBehaviour
  {
    [Header("Visuals")]
    [SerializeField] private Renderer ringRenderer;

    [Header("Colors")]
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color preActiveColor = new Color(1f, 0.5f, 0f, 1f);
    [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);

    [Header("Detection")]
    [Tooltip("Radius to detect the player flying through the ring")]
    [SerializeField] private float detectionRadius = 20f;

    private Manager manager;
    private bool isCurrent = false;
    private bool triggered = false;

    public void Initialize(Manager m)
    {
      manager = m;
      triggered = false;
      if (ringRenderer == null)
        ringRenderer = GetComponentInChildren<Renderer>();
    }

    public void SetState(bool current, bool isPreActive = false)
    {
      isCurrent = current;
      if (ringRenderer != null)
      {
        if (isCurrent)
        {
          ringRenderer.material.color = activeColor;
        }
        else if (isPreActive)
        {
          ringRenderer.material.color = preActiveColor;
        }
        else
        {
          ringRenderer.material.color = inactiveColor;
        }
      }
    }

    private void Update()
    {
      if (!isCurrent || triggered || manager == null) return;

      Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
      foreach (var hit in hits)
      {
        if (hit.CompareTag("Player") || hit.transform.root.CompareTag("Player"))
        {
          triggered = true;
          manager.OnRingPassed(this);
          break;
        }
      }
    }

    private void OnDrawGizmosSelected()
    {
      Gizmos.color = isCurrent ? Color.green : Color.gray;
      Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
  }
}
