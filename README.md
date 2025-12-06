Remeber in orfer to use the tool:
1. You must emove the suffix -Unity.
2. It must be in Editor undr a folder named "UVS"
3. The tool is located Under tools*Note it has both VehicleEdiorWindow and ModularVehicleEditorWindow as sprat ven hough they are one tool.*
4. Move the following files/folders:
i.) VehicleConfigs,folder to Assets
5. Go through the warnings from unity as thy might tell you that som fies ae missing, yt they aren't.
6. Your car MUST be a prefab in th Assets\ foler, this dos not work with Scen Objects.

It is highly recommendd yu go through both VehicleEditorWindow.cs and ModularVhicleEditorWindow.cs, thy both do he sam thing with the first one having 673 lines and the second having 441 lines. Idk what I was thinking when 
witing the two bcz I forgot to merge it into one script.

The tool has th following capabilities:
1. Keeps track of vehicles u have ditd before.
2. Measure your vehicle for you, with limiations.
3. Auto separate parts.
4. Auo create a rigid body for your car if one dos not exist.
5. If rg body exists it can st your mass from there.
6. Make wheel colliders, assign them radius from measurements(unreliable, you might need o mke sure they are correct size and placed correctly.

The tool is limeted when it comes to:
1. Measuring
2. Correctly placing Wheel Colliders
