using UnityEngine;

public class ReloadingStateBehaviour : StateMachineBehaviour {

    private PlayerScript playerScript;

    private static int BODY_LAYER = 1;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        if (playerScript == null) {
             playerScript = animator.transform.parent.GetComponent<PlayerScript>();
        }

        if (playerScript != null) { 
            playerScript.isReloading = true;
            
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        if (playerScript != null) {
            if (stateInfo.normalizedTime > 0.5f && stateInfo.normalizedTime < 1) {
                if (playerScript.networkAnimator != null)
                    playerScript.networkAnimator.animator.SetBool("Reload", false);
            }
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        if (playerScript != null) {
            playerScript.isReloading = false;
        }
    }
}
