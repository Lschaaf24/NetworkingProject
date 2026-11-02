using Mirror;
using UnityEngine;

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
        Debug.Log($"[Client] I am {playerName}. Move with WASD, press Space to emote.");
    }

    void Update()
    {
        // every copy of this script runs Update() only the owner should read input
        if (!isLocalPlayer) return;

        Vector2 move = ReadMove();
        transform.position += new Vector3(move.x, 0, move.y) * (moveSpeed * Time.deltaTime);
        // We don't send the position ourselves. NetworkTransform notices the change and syncs it

    }

    // Simple follow camera for the local player only
    void LateUpdate()
    {
        if (!isLocalPlayer || Camera.main == null) return;
        Transform cam = Camera.main.transform;
        cam.position = transform.position + new Vector3(0, 9, -7);
        cam.rotation = Quaternion.Euler(52, 0, 0);
    }



    static Vector2 ReadMove()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var k = Keyboard.current;
        if (k == null) return default;
        return new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0),
                           (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
#else
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
    }
}
