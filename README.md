Remeber in order to use the tool:
1. You must emove the suffix -Unity.
2. It must be in Editor undr a folder named "UVS"
3. The tool is located Under tools*Note it has both VehicleEdiorWindow and ModularVehicleEditorWindow as sprat ven hough they are one tool.*
4. Move the following files/folders:
i.) VehicleConfigs,folder to Assets
5. Go through the warnings from unity as thy might tell you that som fies ae missing, yt they aren't.
6. Your car MUST be a prefab in th Assets\ foler, this does not work with Scene Objects.
7. Now before you go through a head ache, the editor window only supports drag and drop.
8. You must create a tag "vehicle" this tag must be assigned to  the vehicl you want to edit.
9. Navigate to tools, then either of the two(as mentiond in 3), navigate to Info and drop your vehicle in.
10. The vehicle name is automatically copied from model name. You can set author but its not saved.
11. Important, set vehicle ype to any of he suppored ones.
12. You must set a 3 character seed for your car, sinc each ca recves a 16 random Identifier. Your 3 Digit char MUST be ONLY letters and numbers.
13. Click on Generate, then on welcome to unlock the other tabs.
14. Find Parts and hit the rescan parts to automatically arrange your parts. Incase you do not want a part to be assigned to the existing vehicle part types("Body, Wheel, Suspension, Engine, Drivetrain,
        Brake, Light, Interior, Glass, Exhaust,
        Mirror, FuelSystem, Electrical,
        Miscellaneous, SteeringWheel,
        Transmission, Door, Turbo") fo whatever reason, YOU MUST include th suffix Collider/LOD.
15. Go to Wheels Tab, click on scan wheels. It wll list your whels if any exist. If you hav none skp this step. *Note:You must frst scan yur pats to scan wheels, cz I made it gt from parts to save on memory.
16. To add Mass, navgate to body, st you mass.
17. Go back to wheels and click apply collider.

It is highly recommendd yu go through both VehicleEditorWindow.cs and ModularVhicleEditorWindow.cs, thy both do he sam thing with the first one having 673 lines and the second having 441 lines. Idk what I was thinking when witing the two bcz I forgot to merge it into one script. In a future, I might comment out the second script or leav it bcz it dosn't disrupt functionality. 
Do note that the first choic has more fatures than the second.

The tool has th following capabilities:
1. Keeps track of vehicles you have ditd before.
2. Measure your vehicle for you, with limiations.
3. Auto scan parts.
4. Auo create a rigid body for your car if one dos not exist.
5. If rg body exists it can st your mass from there.
6. Make wheel colliders, assign them radius from measurements(unreliable, you might need o mke sure they are correct size and placed correctly.
7. For now it only suppors the following Vehicl types: Land, Air, Water, Space, Fictional.
8. 


The tool is limeted when it comes to:
1. Measuring
2. Correctly placing Wheel Colliders
3. The added moules do not work yet so don't worry.

