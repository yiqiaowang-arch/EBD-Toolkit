using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Tooltip("The CharacterController component that will be used to move the player.")]
    public CharacterController controller;          // Takes care of the actual movement.
    [Tooltip("How fast the character will move around.")]
    public float movementSpeed = 12.0f;             // How fast the character will move around.
    [Tooltip("The gravitational force.")]
    public float gravity = -9.81f;                  // Gravitational force.
    [Tooltip("Object that will perform the ground-check.")]
    public Transform groundCheck;                   // Object that will perform the ground-check.
    [Tooltip("Radius in which the ground-check will be performed.")]
    public float checkRadius = 0.5f;                // Radius in which the ground-check will be performed.
    [Tooltip("LayerMask that will be used to discern what is ground and what not.")]
    public LayerMask layerMask;                     // Allows us to discern what is ground and what not.
    [Tooltip("The height of our jump.")]
    public float jumpHeight = 1.0f;                 // The height of our jump.

    private Vector3 velocity;                       // Current velocity of the character.
    private bool isOnGround;                        // Is the character on the ground or not.
    
    // Update is called once per frame
    void Update()
    {
        // Perform the ground-check so we can decide if we need to apply gravity or not.
        isOnGround = Physics.CheckSphere(groundCheck.position, checkRadius, layerMask);

        // If we are on the ground, reset the velocity.
        if (isOnGround && velocity.y < 0) 
        {
            velocity.y = 0.0f;
        }
        
        // Get input.
        float xMove = Input.GetAxis("Horizontal");
        float zMove = Input.GetAxis("Vertical");

        // The direction the player will move in.
        Vector3 move = transform.right * xMove + transform.forward * zMove;
        
        // Tell the CharacterController to move.
        controller.Move(move * movementSpeed * Time.deltaTime);

        // Jump.
        if (isOnGround && Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        }

        // Apply gravity.
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
