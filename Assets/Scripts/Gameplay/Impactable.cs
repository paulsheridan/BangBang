using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Impactable : MonoBehaviour
{
    [Tooltip("VFX prefab to spawn upon impact")]
    public GameObject ImpactVfx;

    public GameObject GetImpactVfx()
    {
        return ImpactVfx;
    }
}
