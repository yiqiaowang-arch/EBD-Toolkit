﻿/*
DesignMind: A Toolkit for Evidence-Based, Cognitively- Informed and Human-Centered Architectural Design
Copyright (C) 2023  michal Gath-Morad, Christoph Hölscher, Raphaël Baur

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public CharacterController controller;          // Takes care of the actual movement.
    public float movementSpeed = 12.0f;             // How fast the character will move around.
    public float gravity = -9.81f;                  // Gravitational force.
    public Transform groundCheck;                   // Object that will perform the ground-check.
    public float checkRadius = 0.5f;                // Radius in which the ground-check will be performed.
    public LayerMask layerMask;                     // Allows us to discern what is ground and what not.
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
