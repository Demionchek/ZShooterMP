using System;
using Mirror;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerScript : EntityScript {

    [Header("References")]
    public TextMesh playerNameText;
    public GameObject playerNameObj;
    public GameObject[] weaponArray;
    public Renderer playerRenderer;
    public Transform cameraPivot;

    [Space(10)]

    [Header("Controlls")]
    public float rotationSpeedY = 10f;
    public float rotationSpeedX = 10f;
    public float groundedOffset = -0.14f;
    public LayerMask groundLayers;

    [Header("PlayerStats")]
    public int maxHealth = 5;
    public float movementSpeed = 4f;

    [HideInInspector, SyncVar(hook = nameof(OnNameChanged))]
    public string playerName;

    [HideInInspector, SyncVar(hook = nameof(OnColorChanged))]
    public Color playerColor = Color.white;

    [HideInInspector, SyncVar(hook = nameof(OnWeaponChanged))]
    public int activeWeaponSynced = 1;
    [HideInInspector]
    public bool isReloading;

    private Material playerMaterial;
    private SceneScript sceneScript;
    private Weapon activeWeapon;
    private CharacterController characterController;
    private MultiAimConstraint multiAimConstraint;
    private Vector3 verticalVelocity;

    private float weaponCooldownTime;

    private bool isGrounded;
    private int selectedWeaponLocal = 1;
    private float gravityModyfier = 2;
    private float terminalVelocity = -50f;
    private float mouseInputX = 0.0f;
    private float mouseInputY = 0.0f;
    private int enemyLayer;


    [Command]
    public void CmdSendPlayerMessage() {
        if (sceneScript)
            sceneScript.statusText = $"{playerName} says hello {UnityEngine.Random.Range(10, 99)}";
    }

    [Command]
    public void CmdSetupPlayer(string name, Color color) {
        // player info sent to server, then server updates sync vars which handles it on all clients
        playerName = name;
        playerColor = color;
        sceneScript.statusText = $"{playerName} joined.";
    }

    [Command]
    public void CmdChangeActiveWeapon(int newIndex) {
        activeWeaponSynced = newIndex;
    }

    public override void OnStartLocalPlayer() {

        sceneScript.playerScript = this;
        Camera.main.transform.SetParent(cameraPivot);
        Camera.main.transform.localPosition = new Vector3(0, 0, -0.5f);

        characterController = GetComponent<CharacterController>();
        networkAnimator = GetComponent<NetworkAnimator>();
        multiAimConstraint = GetComponentInChildren<MultiAimConstraint>();

        if (characterController == null)
            Debug.LogError("characterController is null!");

        playerNameObj.transform.localPosition = new Vector3(0, -1f, 0.6f);
        playerNameObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        string name = "Player" + UnityEngine.Random.Range(100, 999);
        Color color = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
        CmdSetupPlayer(name, color);

        CmdChangeActiveWeapon(++selectedWeaponLocal);
    }

    void Awake() {

        health = maxHealth;

        //allow all players to run this
        sceneScript = GameObject.Find("SceneReference").GetComponent<SceneReference>().sceneScript;

        if(sceneScript != null) {
            sceneScript.UIMaxHealth(health);
        }

        gameObject.name = "Player" + UnityEngine.Random.Range(0, 100);

        enemyLayer = LayerMask.NameToLayer("Enemy");

        if (selectedWeaponLocal < weaponArray.Length && weaponArray[selectedWeaponLocal] != null) {
            activeWeapon = weaponArray[selectedWeaponLocal].GetComponent<Weapon>();
            sceneScript.UIAmmo(activeWeapon.weaponAmmo);
        }
    }

    void OnWeaponChanged(int oldIndex, int newIndex) {

        if (0 < oldIndex && oldIndex < weaponArray.Length && weaponArray[oldIndex] != null)
            weaponArray[oldIndex].SetActive(false);


        if (0 < newIndex && newIndex < weaponArray.Length && weaponArray[newIndex] != null) {
            weaponArray[newIndex].SetActive(true);
            activeWeapon = weaponArray[activeWeaponSynced].GetComponent<Weapon>();

            if (isLocalPlayer && activeWeapon != null)
                sceneScript.UIAmmo(activeWeapon.weaponAmmo);
        }
    }

    public void OnNameChanged(string oldName, string newName) => playerNameText.text = playerName;


    public void OnColorChanged(Color oldColor, Color newColor) {

        playerNameText.color = newColor;

        if (playerRenderer == null)
            return;

        playerMaterial = new Material(playerRenderer.material);
        if (playerMaterial != null) {
            playerMaterial.color = newColor;
            playerRenderer.material = playerMaterial;
        }
    }

    private void Update() {

        if (!isLocalPlayer) {
            // make non-local players run this
            playerNameObj.transform.LookAt(Camera.main.transform);
            return;
        }

        sceneScript.UIHealth(health);

        if (isDead) {
            
            return;
        }

        LookInput();
        GroundedCheck();
        Movement();
        AttackInput();
        ReloadInput();
    }

    private void ReloadInput() {
        if (Input.GetKeyDown(KeyCode.R) && !isReloading) {
            if (networkAnimator != null)
                networkAnimator.animator.SetBool("Reload", true);
            if (activeWeapon != null) {
                activeWeapon.weaponAmmo = activeWeapon.maxAmmo;
            }
        }
    }

    private void FixedUpdate() {
        if (!isLocalPlayer)
            return;
    }

    private void LookInput() {
        mouseInputX += Input.GetAxis("Mouse X") * rotationSpeedX;
        mouseInputY -= Input.GetAxis("Mouse Y") * rotationSpeedY;

        transform.rotation = Quaternion.Euler(0, mouseInputX, 0);

        mouseInputY = Math.Clamp(mouseInputY, -60, 40);

        cameraPivot.transform.eulerAngles = new Vector3(mouseInputY, mouseInputX, 0);
    }

    private void AttackInput() {

        if (isReloading) return; 

        if (Input.GetButtonDown("Fire2")) {
            selectedWeaponLocal++;

            if (selectedWeaponLocal > weaponArray.Length - 1)
                selectedWeaponLocal = 1;

            CmdChangeActiveWeapon(selectedWeaponLocal);
        }

        if (Input.GetButtonDown("Fire1")) {
            if (activeWeapon && Time.time > weaponCooldownTime && activeWeapon.weaponAmmo > 0) {
                weaponCooldownTime = Time.time + activeWeapon.weaponCooldown;
                activeWeapon.weaponAmmo -= 1;
                sceneScript.UIAmmo(activeWeapon.weaponAmmo);
                FireWeapon();
            }
        }
    }

    private void Movement() {
        float inputX = Input.GetAxis("Horizontal");
        float inputZ = Input.GetAxis("Vertical");

        if (networkAnimator != null) {
            networkAnimator.animator.SetFloat("InputFwd", inputZ);
            networkAnimator.animator.SetFloat("InputRight", inputX);
        }

        inputX *= Time.deltaTime * movementSpeed;
        inputZ *= Time.deltaTime * movementSpeed;

        if (isGrounded) {
            verticalVelocity = new Vector3(0, -2f, 0);
        } else {
            if (verticalVelocity.y > terminalVelocity) {
                verticalVelocity += Physics.gravity * gravityModyfier * Time.deltaTime;
            }
        }

        Vector3 moveDir = Vector3.ClampMagnitude(transform.forward * inputZ + cameraPivot.transform.right * inputX, movementSpeed);

        moveDir += verticalVelocity;

        if (characterController != null) {
            characterController.Move(moveDir);
        }
    }

    private void GroundedCheck() {
        float groundedRadius = characterController.radius - 0.1f;
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
        isGrounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);
    }

    [Command]
    void FireWeapon() {
        GameObject bullet = Instantiate(activeWeapon.weaponBullet, activeWeapon.weaponFirePosition.position, activeWeapon.weaponFirePosition.rotation);
        NetworkServer.Spawn(bullet.gameObject);
        bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * activeWeapon.weaponSpeed;
    }

    [ClientRpc]
    public override void RcpTakeDamage() {
        if (health > 0) {
            health--;
            if (health <= 0) {
                OnDeath();
            }
        } 
    }

    private void OnDeath() {
        isDead = true;
        if (networkAnimator != null)
            networkAnimator.animator.SetBool("isDead", true);

        if (multiAimConstraint != null)
            multiAimConstraint.weight = 0f;
    }

    private void OnTriggerStay(Collider other) {

        if (!isServer)
            return;

        if (other.gameObject.layer == enemyLayer) {
                RcpTakeDamage();
        }
    }
}
