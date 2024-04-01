using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackStateBehaviour : StateMachineBehaviour {

    EnemyScript enemyScript;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        
        if (enemyScript == null) { 
            animator.GetComponent<EnemyScript>();
        }

        if (enemyScript != null) {
            enemyScript.isAttacking = true;
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        if (enemyScript != null) {
            enemyScript.isAttacking = false;
        }
    }
}
