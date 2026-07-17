# Unity 3D side-view camera

The runtime world is a full 3D scene presented as a vertical side-view cross-section.
The logical grid keeps `X` as horizontal and `Y` as depth below the surface. The Unity
bootstrap rotates the shared visual root by 90 degrees around the X axis, mapping the
existing renderer coordinates `(X, visual height, Y)` to `(X, -Y, visual depth)`.
This preserves the real thickness of terrain cubes, residents, buildings, items,
placement ghosts, Job markers and navigation routes without duplicating coordinate
conversion logic in every renderer.

## Camera contract

- The camera is perspective, never orthographic.
- The default pose is a direct side view from the foreground depth.
- Holding either Control key and moving the mouse horizontally adjusts bounded yaw.
- Holding Control and moving the mouse vertically adjusts bounded pitch.
- The default limits preserve side-view readability and prevent rotating behind the map.
- `Home` resets yaw and pitch to the direct side view.
- The mouse wheel changes camera distance.
- WASD and arrow keys pan the focus in the vertical side-view plane.
- Framing distance is calculated from world width, world height, aspect ratio, vertical
  field of view and padding.

`SideViewCameraOrbitState` and `SideViewCameraFraming` are engine-independent
Presentation contracts with unit tests. `DigCameraController` only adapts Unity input,
Camera and Transform state. The Unity source-contract validator prevents regressions
back to orthographic projection or an unrotated top-down world root.
