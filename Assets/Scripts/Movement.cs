using UnityEngine;
using Mirror;

public class Movement : NetworkBehaviour
{
    void Update()
    {
        if (isLocalPlayer)
        {
            float moveHorizontal = Input.GetAxis("Horizontal");
            float moveVertical = Input.GetAxis("Vertical");
            Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
            transform.Translate(movement * Time.deltaTime * 5.0f);
        }
    }
}
