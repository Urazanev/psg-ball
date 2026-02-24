using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slingshot : MonoBehaviour
{
    [SerializeField]
    Material LitMaterial;

    MeshRenderer meshRenderer;
    Material original;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            original = meshRenderer.material;
    }

    public void ApplyMaterials(Material baseMaterial, Material litMaterial)
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) return;

        if (baseMaterial != null)
        {
            original = baseMaterial;
            meshRenderer.sharedMaterial = baseMaterial;
        }

        if (litMaterial != null)
            LitMaterial = litMaterial;
    }

    void OnCollisionEnter(Collision c)
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) return;

        meshRenderer.sharedMaterial = LitMaterial != null ? LitMaterial : original;

        AudioSource speaker = GetComponent<AudioSource>();
        if (speaker == null) return;
        if (!SoundCatalog.PlayRandom(speaker, "slingshot_snap_"))
            speaker.Play();
    }

    void OnCollisionExit(Collision c)
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) return;

        meshRenderer.sharedMaterial = original;
    }
}
