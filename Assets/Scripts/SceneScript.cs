using Mirror;
using UnityEngine;
using UnityEngine.UI;


public class SceneScript : NetworkBehaviour {
    public Text canvasStatusText;
    public Text canvasAmmoText;
    public Slider healthSlider;
    public PlayerScript playerScript;
    public SceneReference sceneReference;
    public Text reloadSceneText;

    [SyncVar(hook = nameof(OnStatusTextChanged))]
    public string statusText;

    private void OnStatusTextChanged(string oldText, string newText) {
        canvasStatusText.text = statusText;
    }

    public void ButtonSendMessage() {
        if (playerScript != null)
            playerScript.CmdSendPlayerMessage();
    }

    public void UIAmmo(int value) {
        canvasAmmoText.text = "Ammo: " + value;
    }

    public void UIHealth(int health) {
        healthSlider.value = health;
    }

    public void UIMaxHealth(int health) {
        healthSlider.maxValue = health;
        healthSlider.value = health;
    }

    public void ButtonChangeScene() {
        if (isServer) {
            NetworkManager.singleton.ServerChangeScene("MyScene");
        } else {
            reloadSceneText.text = "Only for Host";
        }
            
    }
}

