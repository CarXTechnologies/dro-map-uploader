## Instructions for uploading tracks to workshop</h1>

1. [Preparation of the track loading project](https://github.com/CarXTechnologies/dro-map-uploader#1-preparation-of-the-track-loading-project)
2. [Import the 3D model of the alignment into the project](https://github.com/CarXTechnologies/dro-map-uploader#2-import-the-3d-model-of-the-alignment-into-the-project)
3. [Adding core components](https://github.com/CarXTechnologies/dro-map-uploader#3-adding-core-components)
   1. [Assigning surface collisions](https://github.com/CarXTechnologies/dro-map-uploader#a-assigning-surface-collisions)
   2. [Assigning a spawn point on the map](https://github.com/CarXTechnologies/dro-map-uploader#assigning-a-spawn-point-on-the-map)
   3. [Assigning ambient sounds](https://github.com/CarXTechnologies/dro-map-uploader#assigning-ambient-sounds)
   4. [Adding a mini-map](https://github.com/CarXTechnologies/dro-map-uploader#adding-a-mini-map)
4. [Uploading the track to the workshop](https://github.com/CarXTechnologies/dro-map-uploader#4-uploading-the-track-to-the-workshop)
5. [Supported Components](https://github.com/CarXTechnologies/dro-map-uploader#5-supported-components)
6. [Recommendations](https://github.com/CarXTechnologies/dro-map-uploader#6-recommendations)

# 1. Preparation of the track loading project

1. To get started, you need to download the project archive. You can find the download link here: **[Project](https://github.com/CarXTechnologies/dro-map-uploader)**. Once you have downloaded the archive, extract it to any location on your computer (Code → Download ZIP).


![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/1.png?raw=true)

1. To open the project, you'll need to install the Unity Editor with version 2020.3.25 (available for 64-bit systems only). You can download it from the following link: **[Download link](https://download.unity3d.com/download_unity/9b9180224418/Windows64EditorInstaller/UnitySetup64-2020.3.25f1.exe)**
2. The next step is to launch Unity. Go to the File → Open Project menu, then choose the folder that contains the unpacked project (ensure this folder includes the Assets, Packages, etc. folders).
3. When the project is set up, we can proceed to the next step.

# 2. Import the 3D model of the alignment into the project

1. In the _Assets/MapResources/ folder,_ create a folder with the working name of the map.
2. Within the created folder, generate a scene through the "Assets → Create → Scene" menu.
3. Next, you need to load .fbx/[.obj](https://www.autodesk.com/products/fbx/overview)/[.dae](https://www.khronos.org/collada/) Assets/MapResources/%_your\_folder%/_ folder via Drag & Drop.
4. If the materials are not set up within the models, create the materials using the "Assets → Create → Material" menu and configure them as shown below

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/2.png?raw=true)

1. Open the created scene (step 2) and generate a GameObject by dragging the 3d model onto the scene.
2. For the created GameObject, add the required components ( **refer to section 3** ).
3. To create a reusable object (Prefab), create the _Assets/MapResources/_%your\_folder%/ Prefabs folder.
4. Right-click on the object in the scene and choose "Prefab → Unpack Complete" from the menu.
5. The next step is to drag the GameObject from the scene to the newly created "Prefabs" folder. This enables the object to be reused as many times as needed.

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/3.png?raw=true)

# 3. Adding core components

The project supports several types of components that are ported into the game. The main ones are:

- the point where the car appears on the map,
- Ambient Sounds
- Physical Materials of Surfaces

These components are assigned using the **GameMarkerData helper.**

To add a component to a GameObject or Prefab in the Inspector window, click the _Add Component button_ and type the name _GameMarkerData._
 There is also an option to add a mini-map.

## Assigning surface collisions

1. For the track object that representing the surface, choose the GameMarkerData **Road** component type, and in the dropdown, pick the material type that will be used in the game when interacting with this surface.

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/4.png?raw=true)

Please note that any GameObject/Prefab with collision must also have a Collider component (Box/Sphere/Capsule/Mesh Collider). This condition is necessary for collision accuracy.

## Assigning a spawn point on the map

1. To assign a spawn point on the map, create an empty object: choose the GameObect → Create Empty menu item (or Ctrl+Shift+N). In the Transform component, set the coordinates of the point that is most suitable for the car to appear in the game. Add the _GameMarkerData component_, and choose the **SpawnPoint type**

Please note that only one **vehicle spawn point** should be placed on the map!

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/5.png?raw=true)

## Assigning ambient sounds

1. To assign a spawn point on the map, select the GameObect → Create Empty menu item (or Ctrl+Shift+N). Add the _GameMarkerData component, and select the_ Ambient type. **Next, in the dropdown, select the sound type that is the best suitable on the map.**

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/6.png?raw=true)

When adding an Ambient-type marker, you can use the DrawZoneBehaviour component. It's like a helper that shows where the sounds will be heard. For instance, for an Ambient marker, it draws a zone to indicate where the assigned sounds will be heard.

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/7.png?raw=true)

If you need another helper script, you can write it yourself and add your own at Assets/Resources/MapSkipComponent

## Adding a mini-map

1. You can also include an optional minimap feature. To do this, create an empty object in the scene (as mentioned earlier) and add a Minimap component. Then, select the Minimap Layer. In the Textures field → Element 0, assign at least one texture - MainTexture. Additionally, while configuring the map, you can use auxiliary functions to create a template. You can upload this template to the graphical editor and design your own minimap based on it. Keep in mind that the map should be centered relative to zero coordinates.
2. Bound center represents the minimap's offset relative to the center.
3. Bound size indicates the map's size measured in world scale.

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/8.png?raw=true)

## Capture prototypes : icon, preview, minimap

1. Added component CaptureCamera for Camera GameObject.
2. Set up your camera for your prototype.
3. Open component contex menu, and press Capture.
4. Save the prototype to disk.
![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/14.png?raw=true)

# 4. Uploading the track to the workshop

1. To upload the track, you initially need to create a map configuration in the map folder. 

1. Next, you should configure the settings in the configuration file.

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/9.png?raw=true)

3. To do this, you need to:
 - \*specify **the name of the working scene.** - \*enter the map name that will be displayed in the workshop<br>
 - add a map description that will be displayed in the workshop<br>
 - \*Set an **Icon** -the icon of the map that will be displayed **in the list of workshop maps in the game itself** (read/write enabled item required, png only)-<br>
 - \*Set a **Large icon** - map preview, which will be displayed in the **workshop** and when entering the map in the game itself(read/write enabled item required, png only)

- Item Workshop Id _- map id received when uploading to_ workshop (assigned automatically after publishing)<br>
**- Upload Steam Description - if enabled, the description on the workshop page will be updated.<br>
-** Upload Steam Name - if enabled, the map name on the workshop page will be updated.<br>
-** _Upload Steam Preview_ - If enabled, the map icon on the Workshop page will be updated

>( **\*** ) - required field<br>

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/10.png?raw=true)

1. To set the scene as a build on Steam, select the MapMetaConfig that you created earlier in Resources/MapManagerConfig

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/11.png?raw=true)

1. Once everything is prepared, it's time to export the track to Steam. Open Steam and select the build options from the available items.

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/12.png?raw=true)

  1. **Create** - Simply assemble the map if there are no errors. Then the map is done correctly. It also creates an intermediate result in Asset/{MapName}
  2. **Create and publication** - Assemble the map and submit it to the workshop. If the map is successfully assembled and passes the publishing stage, there should be no errors in the console.
  3. **Update exist publication** - updates an existing publication that you have access to, the id of the last publication is specified in the _Item Workshop Id field_

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/13.png?raw=true)

# 5. Supported Components

1. Physics - MeshCollider, BoxCollider, SphereCollider, CapsuleCollider, Rigidbody.
2. Graphics - ReflectionProbe, Volume.
3. Renderer - MeshRenderer, MeshFilter, Light, LODGroup, ParticleSystemRenderer.
4. UI - Canvas, RawImage, TextMeshProUGUI.

# 6. Recommendations

1. Avoid using multiple Directional light sources.
2. Ensure the Steam preview does not exceed the limit of 1MB.
3. Keep the Steam description within the character limit of 8000.
4. Limit the map name to 128 characters for Steam.
5. Use Latin alphabet when writing names.
6. Ensure the maximum map size does not exceed 4GB.
7. Ensure the maximum size of the meta does not exceed 24MB (including preview, icon, description, title).
8. Limit the number of vertices to 30,000,000 per 100 units.
9. Be mindful of component limitations.
10. Non-convex MeshColider with non-kinematic Rigidbody is no longer supported
    
If the map is configured incorrectly, an error will be displayed during uploading that will need to address the causes on your own.

After completing these steps, the map will be published in the **steam workshop**. Initially, the visibility of the downloaded map will be "Private". For example, you can test the **created** map in the game, and it will be visible only to the author. To do this, you need to go _to the Workshop → Track Workshop menu in the game._ You can change the visibility to "Public" on the map page in the **workshop**.

Important note! The "Friends Only" visibility option is currently experiencing issues due to a problem with the external library used for Steam API integration. We plan to resolve this matter in upcoming releases.
