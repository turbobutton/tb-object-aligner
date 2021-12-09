# TB Object Aligner
A Unity tool to help align game objects in the scene.

# How To
To open the TB Object Aligner window, go to Tools > Object Aligner...

![Opening the tool](/Images/ObjectAligner_OpenTool_01.png?raw=true)

The window that opens should look like this:

![Overview image of the tool](/Images/ObjectAligner_Main_01.png?raw=true)

There are two main ways of aligning objects using the TB Object Aligner tool: Center and Distribute.

## Center
Centering objects aligns them along the chosen axis in either World or Local space.

### Average
Centers the objects along the chosen world axis that passes through the average position of all selected objects.

![Average centering](/Images/ObjectAligner_Center_Average_01.png?raw=true)

### To Object - World Space
Centers all other objects along the chosen world axis vector that passes through the selected object's position.

![To object centering](/Images/ObjectAligner_Center_Single_World_01.png?raw=true)

### To Object - Local Space
Centers all other objects along the local axis of the selected object.

![To object centering](/Images/ObjectAligner_Center_Single_Local_01.png?raw=true)

## Distribute