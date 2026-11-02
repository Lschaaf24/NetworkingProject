using Mirror;
using UnityEngine;

// =====================================================================================
//  OwnershipDebug.cs  -  PROVES WHICH PLAYER OBJECT EACH WINDOW CONTROLS
// =====================================================================================
//  Add this to the root of your Player prefab. It works alongside ANY player script.
//  It draws a label above every player in every window:
//     GREEN "LOCAL"  = this window owns/controls this object  (isLocalPlayer == true)
//     RED   "REMOTE" = some other window controls it
//
//  CORRECT RESULT: in EACH window exactly ONE cube is green, and it is a DIFFERENT cube
//  in each window.
// =====================================================================================
public class OwnershipDebug : NetworkBehaviour
{
    public override void OnStartClient() =>
        Debug.Log($"[Ownership] spawned netId={netId} isLocalPlayer={isLocalPlayer} isServer={isServer} name={name}");

    public override void OnStartLocalPlayer() =>
        Debug.Log($"[Ownership] >>> I CONTROL netId={netId} ({name}) <<<");

    void OnGUI()
    {
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
        if (sp.z < 0) return;

        // Only the server knows which connection owns an object.
        string owner = isServer && connectionToClient != null ? $"  ownerConn={connectionToClient.connectionId}" : "";

        GUI.color = isLocalPlayer ? Color.green : Color.red;
        GUI.Label(new Rect(sp.x - 130, Screen.height - sp.y - 20, 300, 44),
            $"netId {netId} | {(isLocalPlayer ? "LOCAL" : "REMOTE")}\nserver:{isServer} client:{isClient}{owner}");
        GUI.color = Color.white;
    }
}
