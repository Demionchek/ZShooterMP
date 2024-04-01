using System;
using UnityEngine;


public class HitBoxTrigger : MonoBehaviour {

    public LayerMask targetLayer;

    private bool hasHit;

    private void OnDisable() {
        hasHit = false;
    }

    private void OnTriggerStay(Collider other) {
        if (!hasHit) { 
            if (targetLayer == (targetLayer | (1 << other.gameObject.layer))) { 
                hasHit = true;
                gameObject.SetActive(false);
            }
        }
    }
}

