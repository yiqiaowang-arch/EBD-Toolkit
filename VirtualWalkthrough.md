# Virtual Walkthrough

The _EBD-Toolkit_ enables you to perform virtual walkthroughs in your desired environment while tracking a variety of human-centered performance measures.
# 1 Quickstart
Open the scene `Assets/Scenes/VirtualWalkthrough.unity`. It is a complete example that demonstrates how to set up a virtual walkthrough project.

![Figure 1](/figures/vr_editor_overview.jpg)
***Figure 1**: This is what you will see if you double-click on the scene object and the scene is loaded. Annotations in yellow.*

In the _Scene Hierarchy_ you can see that the scene contains 5 objects. We will provide a brief overview and will describe the possibilities for customization later.
- **Model**: This is the model of your environment. This model can be arbitrarily complex. In this example, the model is kept simple on purpose.
- **Directional Light**: This light source provides illumination in the scene. However, other light sources are also allowed.
- **WalkthroughCharacter**: This is the character that you will control in first-person view when performing a virtual walkthrough. It has a `PlayerMovement` component attached, which allows you to control characteristics of the movement such as speed. In addition, it has a `CaptureWalkthrough`component attached, which records the movement and view direction of the character during the simulation.
- **WalkthroughTarget**: This is the object representing the target that the character has to reach. Once the character is close enough, the virtual walkthrough will end and the `CaptureWalkthrough` will save all data recorded during the experiment.
- **WalkthroughProcessor**: This object holds the `ProcessWalkthrough` component, which analyzes the walkthrough data and provides multiple modes of visualization, including a visualization of the generated trajectories and the visual attention patterns.

![Figure 2](/figures/vr_scripts.jpg)
***Figure 2**: An overview of the interaction between the different scripts and their outputs*

You can experience the virtual walkthrough by clicking the play button:
![Figure 3](/figures/vr_quickstart.gif)
***Figure 3**: A very quick first virtual walkthrough*

# 2 Setting up a Walkthrough
This section described how to set up a virtual walkthrough. Please create **a new scene** from the template shown previously `Assets/Scenes/VirtualWalkthrough.unity` and use `Ctrl + D` to duplicate it. Rename the scene to your liking and open it by double-clicking.
## 2.1 Importing your environment
At the heart of your experiment lies your environment. Usually, you will have created this environment with some external CAD software. To import it into Unity, use the `fbx` file format. Navigate to `Assets/Models`, and right-click, then `Import New Asset...`.
![Figure 3](/figures/vr_import_geometry.gif)
***Figure 3**: Importing the model*

Once imported, check `Generate Colliders`, and press `Apply`. This will reimport the asset.
![Figure 4](/figures/vr_generate_colliders.gif)
***Figure 4**: Check `Generate Colliders`*.

>[!NOTE]
>Colliders are representations of the geometry that are used to test whether two objects intersect. This prevents the character from walking through walls and falling through the floor once the experiment starts. In case this still happens, check whether the individual objects of your model have a `MeshCollider` component attached to them.
>![[vr_mesh_collider.jpg]]
>Likewise, if an object should not act as an obstacle (for example a curtain or a door), remove the `MeshCollider` component from said object.

After generating the colliders, you can delete the old model and drag and drop your model into the scene:
![Figure 5](/figures/vr_replace_geometry.gif)
***Figure 5**: Adding the model to the scene with drag-and-drop*

## 2.2 Building the Navigation Mesh

Some of the measures computed from the walkthrough rely on the computation of the shortest path between the start position and the position of the target. To compute this shortest path, we need to create a `NavMesh`. First, open up the Navigation window by clicking `Window > AI > Navigation (Obsolete)`. You can place this window in the inspector view by dragging it there:
![Figure 6](/figures/vr_navigation_window.gif)
***Figure 6**: Adding the Navigation window*

For your model to be detected by Unity to compute the `NavMesh`, you need to make your model `Static`. To do so, select your model in the scene hierarchy, check `Static` in the top right corner, and then select `Yes, Change Children`.
![Figure 7](/figures/vr_make_static.gif)
***Figure 7**: Making the model `Static`*

Then you can bake the `NavMesh` by clicking `Navigation (Obsolete) > Bake`. Your scene view should now display a blue surface on your geometry.
![Figure 8](/figures/vr_navmesh.png)
***Figure 8**: The generated `NavMesh`*

>[!WARNING]
>**I don't see the NavMesh**. It could be that the visualization of the `NavMesh` is disabled. Using the AI Navigation interface in your _scene view_, choose `Surfaces > Show NavMesh`. You can also use this option to disable the display of the `NavMesh`.

Sometimes paths through narrow passages like corridors or doors are not properly represented. This is usually due to two factors:

1. **The simulated agent is too large**: In this case, the size of the agent can be adapted in the `Navigation (Obsolete)` to be narrower (reducing the radius) and shorter (reducing the height).
![Figure 9](/figures/vr_navmesh_configuration.jpg)
***Figure 9**: Adapting the parameters of the character*

2. **The path is blocked by geometry**: If you have an object representing a door or glass, but you intend to make this passage walkable, you can either delete that item or make it not `Static`. To do so, choose this object, and uncheck `Static` (but only for this object).
![Figure 10](/figures/vr_uncheck_static.gif)
***Figure 10**: Make some parts of the model not `Static`*

## 2.3 Assigning Custom Layers

Unity allows users to assign objects to different _Layers_. We can use this functionality to add semantic information to the constituents of our model, which we can then later use to compute the amount of visual attention attributed to different types of objects (for example, to track the visibility of patient beds before and after a design intervention).

Some layers are already provided in the project, e.g., `Wall` and `Floor`. However, you might be interested in adding your custom layer, for example, a layer for `Windows`. To do so, select an arbitrary object in the scene view. Then, in the inspector view, click on the `Layer` drop-down, and then choose `Add Layer...`. Define a new layer by typing the desired layer name into an empty field (or overwrite an existing layer if it's not needed anymore).

![Figure 11](/figures/vr_add_layer.gif)
***Figure 11**: Adding a new layer*

Then, for all the objects of your model, choose the appropriate layer.

>[!TIP]
>Certain CAD software (such as Rhino) allows you to organize your model into layers. Exporting the model to `fbx` will preserve these layers and represent your model as a hierarchy in Unity, grouping the objects by layer. This makes it much easier to assign the layers, as you can simply assign the layer to the parent object and choose `Yes, Change Children`. Thus we highly recommend grouping your geometry accordingly to speed up the assignment.

## 2.4 Designing the Walkthrough

Now it is time to decide on the start and end position of the walkthrough. Move the `WalkthroughCharacter` and `WalkthroughTarget` to the desired positions in the layout.

![Figure 12](/figures/vr_place_character_and_target.gif)
***Figure 12**: Placing the character and the target*

## 2.5 Adapting the Walkthrough-Behavior

The characteristics of the character and the first-person view can be modified via the components `PlayerMovement` and `CaptureWalkthrough` on the `WalkthroughCharacter` and the `MouseTracker` component of the child `FieldOfView` of the `WalkthroughCharacter` (see _Figure 2_ for the reference)

>[!NOTE]
>Most of the parameters can be left on their default value. You can hover over the parameter to learn about its exact meaning if it is not clear from the name:
>![[vr_tooltips.gif]]
>In the following, we will only specifically highlight parameters if it is likely that you need to adapt them for your project

In your simulations, please choose an appropriate `Movement Speed` in `PlayerMovement` and `Mouse Sensitivity` on `MouseTracker`. Also, choose a file name for the data that will be recorded on `CaptureWalkthrough`.

## 2.6 Running the Walkthrough

After setting up the task and adapting the parameters to your liking, you can start your walkthrough. After successfully finishing the walkthrough (or finishing early by clicking the start button a second time), a file containing the data will be written to the folder you have chosen before. Remember this file for the next steps.
![Figure 13](/figures/vr_complete_walkthrough.gif)
***Figure 8**: Choosing the location of the save file, running the walkthough, and inspecting the raw data*
# 3 Analyzing the Walkthrough
Now that you have captured the walkthrough, it is time that we analyze it. As you can see in _Figure 2_, the `ProcessWalkthrough` component on the `WalkthroughProcessor` object will take care of that.

There are three main outputs from this section: A visualization of the visual attention, the trajectories, and a summary of some measures computed about the walkthrough (saved in a separate file.)

>[!TIP]
>To avoid recording further walkthroughs when analyzing the data, make sure to disable the `CaptureWalkthrough` component:
>![[vr_disable_component.gif]]

You can now either choose _a single data file_ or _all data files_ in a directory to analyze. If you choose the option `Use all files in raw directory`, visualization and analysis will be performed for all files found in this directory, which can be handy if you want to analyze data generated by a group of participants.
![Figure 14](/figures/vr_choose_data.gif)
***Figure 14**: Choosing the raw data*

## 3.1 Visualizing the Trajectories

![Figure 15](/figures/vr_visualize_trajectory.gif)
***Figure 15**: Activate trajectory visualization*

This option allows you to visualize the trajectory (or trajectories). Multiple options to visualize the trajectories are provided. After hitting play in the editor, you can expect an output similar to this.
![Figure 16](/figures/vr_trajectories.png)
***Figure 16**: Example output of trajectory visualization*
## 3.2 Visualizing Visual Attention
Similarly to the above, you can also visualize the visual attention patterns during the walkthrough by enabling the `Visual Attention Heatmap`.

>[!WARNING]
>The computation for creating the heatmap is more computationally intense than that of the trajectories. You can steer the computational load by setting the parameter `Max Rays` (this is the number of samples that will be taken). We recommend gradually increasing `Max Rays`, and starting with a low value of `1000`. Higher values will lead to more complete results.
>![Max rays comparison](/figures/vr_max_rays_comparison.jpg)

## 3.3 Analysis Output
If `Visual Attention Heatmap` is enabled, a summary file including a variety of measures about the trajectories will be generated. The location of said file is indicated on the `ProcessWalkthrough` script.
![Figure 17](/figures/vr_summary_file_save_location.jpg)
***Figure 12**: Location of the summary file*

The file contains a table, where each row corresponds to a trajectory, and each column to a metric. The contents of this file can be read using any text editor, but it is probably more convenient to use a tool like Excel. Here is a description of the columns:
- **TrialID**: This corresponds to the file name that contained the raw trajectory data
- **Duration**: Duration of the walkthrough in seconds
- **Distance**: Distance of the walkthrough in in-game units
- **AverageSpeed**: Average speed of the character during the walkthrough
- **ShortestPathDistance**: Distance of the shortest path between the start position of the character and the position of the target
- **SurplusShortestPath**: length(trajectory) - length(shortest path) (in game units)
- **RatioShortestPath**: length(trajectory) / length(shortest path)
- **Successful**: 1 if the target was reached, 0 otherwise
All of the following columns correspond to layers in the project. Continuing the previous example, a value of `0.38` in the column `Windows` means that the corresponding participant has cumulatively had objects of layer `Windows` in their field of view for 38% of the walkthrough.