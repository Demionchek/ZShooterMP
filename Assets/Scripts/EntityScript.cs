using Mirror;
using UnityEngine;


public class EntityScript : NetworkBehaviour {

    public bool isDead;
    protected int health;
    public NetworkAnimator networkAnimator;

    public virtual void RcpTakeDamage() { }
    public virtual void TakeDamage(Vector3 position, Quaternion rotation) { }
}

