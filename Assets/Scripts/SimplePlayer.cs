using Mirror;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(NetworkMatch))]
public class SimplePlayer : NetworkBehaviour
{
    // SyncVar is a variable the server owns. Whenever the server changes it, automatically sends the new value to every client that can see this object. 
    [SyncVar] public string playerName;

    //hook is function called on client when value changes
    [SyncVar(hook = nameof(OnColorChanged))] public Color playerColor = Color.white;

    public float moveSpeed = 6f;

    Renderer rend;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        
    }

    void OnColorChanged(Color oldColor, Color newColor)
    {
        ApplyColor(newColor);
    }
    void ApplyColor(Color c) 
    { 
        if (rend != null) rend.material.color = c; 
    }

    // Called on every client when this object is spawned for them
    public override void OnStartClient()
    {
        ApplyColor(playerColor);
        gameObject.name = $"Player ({playerName})";
    }

    // Called on the client that owns this player
    public override void OnStartLocalPlayer()
    {
        Debug.Log($"[Client] I am {playerName}. Move with WASD");

    }

    void Update()
    {

        // every copy of this script runs Update() only the owner should read input
        if (!isLocalPlayer) return;


        if (isLocalPlayer)
        {
            float moveHorizontal = Input.GetAxis("Horizontal");
            float moveVertical = Input.GetAxis("Vertical");
            Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
            transform.Translate(movement * Time.deltaTime * 5.0f);
        }


    }


    // Simple follow camera for the local player only
    void LateUpdate()
    {
        if (!isLocalPlayer || Camera.main == null) return;
        Transform cam = Camera.main.transform;
        cam.position = transform.position + new Vector3(0, 9, -7);
        cam.rotation = Quaternion.Euler(52, 0, 0);
    }


}
