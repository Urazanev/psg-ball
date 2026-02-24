using UnityEngine;
using System.Collections;

public class BumperVisuals : MonoBehaviour
{
    private static readonly Vector3 BaseLocalPos = new Vector3(0f, 0.08f, 0f);
    private static readonly Vector3 BaseLocalScale = new Vector3(0.62f, 1.18f, 0.62f);
    private static readonly Vector3 TopRootLocalPos = new Vector3(0f, 0.96f, 0f);
    private static readonly Vector3 RingLocalPos = new Vector3(0f, -0.02f, 0f);
    private static readonly Vector3 RingLocalScale = new Vector3(0.9f, 0.045f, 0.9f);
    private static readonly Vector3 CapLocalPos = new Vector3(0f, 0.08f, 0f);
    private static readonly Vector3 CapLocalScale = new Vector3(0.56f, 0.34f, 0.56f);

    [Header("Flash Target")]
    public Renderer flashRenderer;

    public float flashDuration = 0.1f;
    public float maxEmissionIntensity = 4.8f;
    
    [Header("Scale Punch Settings")]
    public float punchScale = 1.12f;
    public float punchDuration = 0.12f;

    private Material flashMaterial;
    private Color baseEmissionColor = Color.black;
    private Color flashColor = new Color(1f, 0.84313726f, 0f); // #FFD700
    private Coroutine flashCoroutine;
    private Coroutine punchCoroutine;
    
    private Vector3 originalScale;

    void Start()
    {
        ApplyVisualLayout();
        originalScale = transform.localScale;
        
        Renderer targetRenderer = flashRenderer;
        if (targetRenderer == null)
        {
            Transform capTransform = transform.Find("BumperTop/BumperTopCap");
            if (capTransform != null)
            {
                targetRenderer = capTransform.GetComponent<Renderer>();
            }
        }

        if (targetRenderer == null)
        {
            Transform ringTransform = transform.Find("BumperTop/BumperTopRing");
            if (ringTransform != null)
            {
                targetRenderer = ringTransform.GetComponent<Renderer>();
            }
        }

        if (targetRenderer == null)
        {
            Transform topTransform = transform.Find("BumperTop");
            if (topTransform != null)
            {
                targetRenderer = topTransform.GetComponent<Renderer>();
            }
        }

        if (targetRenderer == null)
        {
            return;
        }

        // Clone materials for this instance and use material 0 as flash layer.
        Material[] materials = targetRenderer.materials;
        if (materials.Length > 0)
        {
            flashMaterial = materials[0];
            if (flashMaterial != null && flashMaterial.HasProperty("_EmissionColor"))
            {
                baseEmissionColor = flashMaterial.GetColor("_EmissionColor");
            }
        }
    }

    private void ApplyVisualLayout()
    {
        Transform baseTransform = transform.Find("Base");
        if (baseTransform != null)
        {
            baseTransform.localPosition = BaseLocalPos;
            baseTransform.localScale = BaseLocalScale;
        }

        Transform topRoot = transform.Find("BumperTop");
        if (topRoot == null)
        {
            return;
        }

        topRoot.localPosition = TopRootLocalPos;

        Transform ringTransform = topRoot.Find("BumperTopRing");
        if (ringTransform != null)
        {
            ringTransform.localPosition = RingLocalPos;
            ringTransform.localScale = RingLocalScale;
        }

        Transform capTransform = topRoot.Find("BumperTopCap");
        if (capTransform != null)
        {
            capTransform.localPosition = CapLocalPos;
            capTransform.localScale = CapLocalScale;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Typically ball has Rigidbody
        if (collision.gameObject.GetComponent<Rigidbody>() != null)
        {
            // Flash Emission
            if (flashMaterial != null)
            {
                if (flashCoroutine != null)
                {
                    StopCoroutine(flashCoroutine);
                }
                flashCoroutine = StartCoroutine(FlashEmission());
            }
            
            // Scale Punch
            if (punchCoroutine != null)
            {
                StopCoroutine(punchCoroutine);
            }
            punchCoroutine = StartCoroutine(PunchScale());
        }
    }

    private IEnumerator FlashEmission()
    {
        float elapsed = 0f;
        flashMaterial.EnableKeyword("_EMISSION");
        
        Color targetFlashColor = flashColor * maxEmissionIntensity;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;
            
            // Lerp from max flash to base color
            Color currentEmission = Color.Lerp(targetFlashColor, baseEmissionColor, t);
            flashMaterial.SetColor("_EmissionColor", currentEmission);
            
            yield return null;
        }

        flashMaterial.SetColor("_EmissionColor", baseEmissionColor);
    }
    
    private IEnumerator PunchScale()
    {
        float elapsed = 0f;
        Vector3 targetScale = originalScale * punchScale;
        
        // We will do a quick out and back
        float halfDuration = punchDuration / 2f;

        // Scale up
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }
        
        elapsed = 0f;
        
        // Scale down
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
}
