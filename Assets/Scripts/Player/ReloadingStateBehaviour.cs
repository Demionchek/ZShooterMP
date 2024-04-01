using UnityEngine;

public class ReloadingStateBehaviour : StateMachineBehaviour {

    private PlayerScript playerScript;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        if (playerScript == null) {
             playerScript = animator.transform.parent.GetComponent<PlayerScript>();
        }

        if (playerScript != null) { 
            playerScript.isReloading = true;
            
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        if (playerScript != null) {
            playerScript.isReloading = false;
            if (playerScript.networkAnimator != null)
            playerScript.networkAnimator.animator.SetBool("Reload", false);
        }
    }
}
