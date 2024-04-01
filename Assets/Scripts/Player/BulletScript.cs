using Mirror;
using UnityEngine;

public class BulletScript : NetworkBehaviour {


    private void OnCollisionEnter(Collision collision) {

        if (!isServer)
            return;

        EnemyScript enemy = collision.collider.GetComponent<EnemyScript>();

        if (enemy != null) {
            enemy.TakeDamage(collision.GetContact(0).point, transform.rotation);
        }

        NetworkServer.Destroy(gameObject);
    }
}

