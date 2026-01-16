# PhysicsHap: A Modular Haptic Interaction Toolkit

PhysicsHap is a modular haptic framework designed for the rapid construction of reconfigurable proxies that provide both high-fidelity shape replication and dynamic force feedback for immersive virtual physics experiments. The framework bridges the gap between digital simulations and physical sensation by integrating passive and active haptic components.

---

## Key Components

### 1. Tangible Bodies (Shape Modules)

Derived from a systematic analysis of 45 secondary school physics experiments, these passive modules provide realistic physical forms:

* **Object-Form**: Holistic physical counterparts for virtual objects with unique geometries, such as blocks or carts.
* **Rod-Form**: Cylindrical or rectangular prisms used to simulate elongated tools like levers.
* **Grip-Form**: A standardized interface that physicalizes only the graspable portion of a complex tool, such as a pulley handle.
* **Rotary-Form**: Physical components for continuous rotational inputs, such as knobs.

### 2. Force Engines (Active Actuators)

The Force Engines utilize integrated actuators to deliver specific kinesthetic profiles:

* **Linear Force Feedback**: Powered by a 2804 brushless DC (BLDC) motor regulated by a SimpleFOC controller and an AS5600 magnetic encoder.
  * **Vertical Rendering**: Utilizes a rocker arm and scissor linkage to simulate forces like buoyancy.
  * **Horizontal Rendering**: Uses a rocker arm, connecting rod, and slider to simulate friction or spring tension.
* **Rotational Force Feedback**: Simulates operational levels in knobs with variable resistance and "snap-to-level" effects.
* **Vibrotactile Feedback**: Employs a vibration motor for discrete event confirmation, such as the mechanical "click" of a button.

---

## Getting Started

1.  **Hardware Assembly**: Assemble the Force Engine using the specified BLDC motors and AS5600 encoders. 
2.  **Firmware**: Flash the Arduino code to the Uno to manage sensor reading and motor control.
3.  **Unity Setup**: Import the toolkit into Unity and configure the Meta XR SDK for hand tracking.
4.  **Calibration**: Perform the spatial registration by placing your hand on the physical proxy to align the virtual and physical coordinate systems.

---




