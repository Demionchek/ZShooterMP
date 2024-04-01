using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyScript : EntityScript {

    public int maxHealth = 3;
    public float speed = 1f;
    public float stopppingDistance;
    public HitBoxTrigger hitBox;
    public GameObject hitPrefab;
    [HideInInspector]
    public bool isAttacking;

    private PlayerScript[] playerScripts;
    private NavMeshAgent navMeshAgent;
    private CapsuleCollider capsuleCollider;
    private Transform currentTarget;

    private void OnEnable() {
        health = maxHealth;

        playerScripts = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);
        if (playerScripts == null)
           Debug.LogError("playerScripts is null!");

        networkAnimator = GetComponent<NetworkAnimator>();
        if (networkAnimator == null)
            Debug.LogError("NetworkAnimator is null!");

        navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent != null) {
            navMeshAgent.stoppingDistance = stopppingDistance;
            navMeshAgent.speed = speed;
        } else {
            Debug.LogError("navMeshAgent is null!");
        }

        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider == null)
            Debug.LogError("capsuleCollider is null!");


        TryFindClosestPayer();
        StartCoroutine(TryChangeTargetCorutine());
    }

    private void Update() {

        if (!isServer)
            return;

        if (!isDead && currentTarget != null) {
            if (!isAttacking && Vector3.Distance(transform.position, currentTarget.position) > stopppingDistance) {

                if (networkAnimator != null)
                    networkAnimator.animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
                
                if (currentTarget != null) {
                    navMeshAgent.SetDestination(currentTarget.position);
                } else {
                    Debug.LogError("currentTarget is null!");
                }

            } else {

                if (!isAttacking)
                    networkAnimator.SetTrigger("Attack");

                if (networkAnimator != null)
                    networkAnimator.animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
            }
        }
    }

    public override void TakeDamage(Vector3 impactPoint, Quaternion impactRotation) {

        if (!isServer)
            return;

        health--;

        RcpTakeDamage(impactPoint, impactRotation);

        if (health <= 0 && !isDead) {
            RpcDiying();
        }
    }

    [ClientRpc]
    private void RpcDiying() {
        isDead = true;
        navMeshAgent.isStopped = true;
        capsuleCollider.isTrigger = true;
        SetRandomDeath();
        StartCoroutine(DespawnWithDealyCorutine());
    }

    [ClientRpc]
    private void RcpTakeDamage(Vector3 impactPoint, Quaternion impactRotation) {
        GameObject gameObject =  Instantiate(hitPrefab, impactPoint, impactRotation);
        Destroy(gameObject, 2f);
    }

    private void SetRandomDeath() {
        int deathIndex = Random.Range(0,1);

        networkAnimator.animator.SetTrigger("isDead");

        switch (deathIndex) {
            case 0:
                networkAnimator.animator.SetTrigger("AltDeath");
                break;
            default: 
                break;
        }
    }

    private IEnumerator TryChangeTargetCorutine() {
        while (!isDead) {
            yield return new WaitForSecondsRealtime(2f);
            TryFindClosestPayer();
        }
    }

    private void TryFindClosestPayer() {

        playerScripts = FindObjectsByType<PlayerScript>(FindObjectsSortMode.None);

        for (int i = 0; i < playerScripts.Length; i++) {

            if (playerScripts[i] != null) {

                if (playerScripts[i].isDead)
                    continue;

                if (currentTarget == null) {
                    currentTarget = playerScripts[i].transform;
                    continue;
                }

                if (Vector3.Distance(transform.position, currentTarget.position)
                        > Vector3.Distance(transform.position, playerScripts[i].transform.position)) {
                    currentTarget = playerScripts[i].transform;
                }
            }
        }
    }

    private IEnumerator DespawnWithDealyCorutine() {
        yield return new WaitForSecondsRealtime(10f);

        NetworkServer.Destroy(gameObject);
    }

    public void HitBoxOff() {
        if (hitBox != null) { 
            hitBox.gameObject.SetActive(false);
        }
    }

    public void HitBoxOn() {
        if (hitBox != null) {
            hitBox.gameObject.SetActive(true);
        }
    }
}
