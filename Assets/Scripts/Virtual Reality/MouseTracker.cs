/*
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

/* EBD FS20, Raphaël Baur
 * In case of bugs or suggestions, feel free to contact rabaur@student.ethz.ch.
 */
public class MouseTracker : MonoBehaviour
{
    
    public float mouseSensitivity = 100.0f;        // How fast is your player reacting to mouse-movement.
    public Transform playerBody;                    // The body of the character.
    public float maxDorsal = 60.0f;                 // How far back can the character tilt its head back.
    public float maxVentral = 60.0f;                // How far forward can the character tilt its head forward.
    
    private float xRotation = 0.0f;                 // The current tilt of the head.

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {

        // Get the current position of mouse and scale it appropriately.
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Change the tilt of the head according to the vertical mouse axis.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90.0f, 90.0f);

        // Transform the character according to previous changes.
        transform.localRotation = Quaternion.Euler(xRotation, 0.0f, 0.0f);
        playerBody.Rotate(Vector3.up * mouseX);        
    }
}
