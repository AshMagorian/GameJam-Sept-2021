using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Player))]
public class PlayerInput : MonoBehaviour
{
    Player player;

    [SerializeField] private int jumpBuffer = 20;
    private int jumpBufferCount = 0;

    void Start()
    {
        player = GetComponent<Player>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 directionalInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        player.SetDirectionalInput(directionalInput);
        
        if (jumpBufferCount > 0)
            jumpBufferCount--;
        if (Input.GetKeyDown(KeyCode.Space) || jumpBufferCount > 0)
        {
            if (player.OnJumpInputDown() == false && jumpBufferCount == 0)
            {
                jumpBufferCount = jumpBuffer;
            }
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            player.OnJumpInputUp();
        }
        if (Input.GetMouseButtonDown(0))
        {
            player.OnAttackInputDown();
        }
    }
}
