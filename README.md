## Instructions for uploading tracks to workshop</h1>

1. [Preparation of the track loading project](https://github.com/CarXTechnologies/dro-map-uploader#1-preparation-of-the-track-loading-project)
2. [Import the 3D model of the alignment into the project](https://github.com/CarXTechnologies/dro-map-uploader#2-import-the-3d-model-of-the-alignment-into-the-project)
3. [Adding core components](https://github.com/CarXTechnologies/dro-map-uploader#3-adding-core-components)
   1. [Assigning surface collisions](https://github.com/CarXTechnologies/dro-map-uploader#a-assigning-surface-collisions)
   2. [Assigning a spawn point on the map](https://github.com/CarXTechnologies/dro-map-uploader#assigning-a-spawn-point-on-the-map)
   3. [Assigning ambient sounds](https://github.com/CarXTechnologies/dro-map-uploader#assigning-ambient-sounds)
   4. [Adding a mini-map](https://github.com/CarXTechnologies/dro-map-uploader#adding-a-mini-map)
4. [Uploading the track to the workshop](https://github.com/CarXTechnologies/dro-map-uploader#4-uploading-the-track-to-the-workshop)
5. [Recommendations](https://github.com/CarXTechnologies/dro-map-uploader#5-recommendations)

# 1. Preparation of the track loading project

1. First, you need to download the project itself. It is available at the link **[Project](https://github.com/CarXTechnologies/dro-map-uploader)**. You can download the zip archive and unzip it anywhere (Code → Download ZIP)

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/1.png?raw=true)

1. Next, to open the project, you need to install the Unity Editor, you need Unity version 2020.3.25, a download link (only for 64-bit systems), **[Download link](https://download.unity3d.com/download_unity/9b9180224418/Windows64EditorInstaller/UnitySetup64-2020.3.25f1.exe)**
2. The next step is to start Unity, select the File → Open Project menu, and select the folder containing the unpacked project (this folder should contain the Assets, Packages, etc. folders)
3. The project has been prepared, we can move on to the next item

# 2. Import the 3D model of the alignment into the project

1. In the _Assets/MapResources/ folder,_ create a folder with the working name of the map.
2. In the created folder, you need to create a scene through the Assets → Create → Scene menu.
3. Next, you need to load .fbx/[.obj](https://www.autodesk.com/products/fbx/overview)/[.dae](https://www.khronos.org/collada/) Assets/MapResources/%_your\_folder%/_ folder via Drag & Drop.
4. If the materials are not set up in the models, then create the material via the Assets → Create → Material menu and configure (see below for an example)

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/2.png?raw=true)

1. Open the created scene (step 2) and create a GameObject by dragging the 3d model onto the scene.
2. For the created GameObject, add the required components ( **section 3** ).
3. To create a reusable object (Prefab), create the _Assets/MapResources/_%your\_folder%/ Prefabs folder.
4. On the object in the scene, right-click and select the Prefab → Unpack Complete menu .
5. Next, drag the GameObject to the created Prefabs folder from the scene. Thus, it is possible to reuse the object as many times as necessary.

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/3.png?raw=true)

# 3. Adding core components

The project supports several types of components that are ported into the game. The main ones are:

- the point where the car appears on the map,
- Ambient Sounds
- Physical Materials of Surfaces

These components are assigned through the **GameMarkerData helper.**

To add a component to a GameObject or Prefab in the Inspector window, click the _Add Component button_ and type the name _GameMarkerData._
 There is also an option to add a mini-map.

## Assigning surface collisions

1. For the track object that represents the surface, select the GameMarkerData **Road** component type, and in the dropdown, select the type of material that will be used in the game when hitting this surface.

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/4.png?raw=true)

Note that any collision GameObject/Prefab must also be with some component of type Collider (Box/Sphere/Capsule/Mesh Collider). This condition is necessary for collisions to be correct.

## Assigning a spawn point on the map

1. To assign a spawn point on the map, create an empty object: select the GameObect → Create Empty menu item (or Ctrl+Shift+N). In the Transform component, set the coordinates of the point that is most suitable for the car to appear in the game. Add the _GameMarkerData component_, and select the **SpawnPoint type**

Please note that only one **vehicle spawn point** should be placed on the map!

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/5.png?raw=true)

## Assigning ambient sounds

1. To assign a spawn point on the map, select the GameObect → Create Empty menu item (or Ctrl+Shift+N). Add the _GameMarkerData component, and select the_ Ambient type. **Next, in the dropdown, select the sound type that best suits the situation on the map.**

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/6.png?raw=true)

When adding an Ambient marker, you can use the DrawZoneBehaviour component, which is a helper component that draws the component's range of operation. For example, for an Ambient type marker, this component will show the area in which the assigned sounds will be heard.

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/7.png?raw=true)

If you need another helper script, you can write it yourself and add your own at Assets/Resources/MapSkipComponent

## Adding a mini-map

1. It is also possible to add an optional minimap feature. To do this, you need to create an empty object in the scene (see above) and add a Minimap component. Next, you need to select the Minimap Layer. In the Textures field → Element 0, you need to assign at least one texture - MainTexture. Also, when configuring the map, you can use the auxiliary functions to create a template that you can upload to the graphical editor and draw your own mini-map based on it. Note that the map must be centered relative to zero coordinates.
2. Bound center is the offset of the minimap relative to the center
3. Bound size - the size of the map is measured in world scale

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/8.png?raw=true)

# 4. Uploading the track to the workshop

1. To unload, you first need to create a map config in the map folder

1. Next, you need to set up the config.

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/9.png?raw=true)

3. To do this, you need to:
 - \*specify **the name of the working scene.** - \*enter the name of the map that will be displayed in the workshop- enter a description of the map that will be displayed in the workshop- \ ***assign** Icon
**-**
 the icon of the map that will be displayed **in the list of workshop maps in the game itself** (read/write enabled item required, png only)-
 \*assign **Large icon** - map preview, which will be displayed in the **workshop** and when entering the map in the game itself(read/write enabled item required, png only)

- Item Workshop Id _- map id received when uploading to_ workshop (assigned automatically after publishing) **- Upload Steam Description - if enabled, then the description on the page in the workshop will definitely be updated-** Upload Steam Name - _if enabled, then_ the map name on the page in the workshop will definitely be updated **-** _Upload Steam Preview_ - If enabled, the map icon on the Workshop page will be updated( **\*** ) - required field

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/10.png?raw=true)

1. To set the scene as a build on Steam, select the MapMetaConfig that you created earlier in Resources/MapManagerConfig

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/11.png?raw=true)

1. When everything is ready, it's time to export to steam, for this open steam, then there are a few items to choose a build

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/12.png?raw=true)

  1. **Create** - Simply assemble the map if there are no errors. Then the map is done correctly. It also creates an intermediate result in Asset/{MapName}
  2. **Create and publication** - Assembles the map and sends it to the workshop, if the card is assembled and successfully passes the publishing stage, then there will be no errors in the console
  3. **Update exist publication** - updates an existing publication that you have access to, the id of the last publication is specified in the _Item Workshop Id field_

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.0/Image/13.png?raw=true)

# 5. Recommendations

1. Do not create multiple Directional light sources
2. Do not exceed the Steam limit of 1mb per preview
3. Do not exceed the Steam limit for a description of 8000 characters
4. Do not exceed the Steam limit of 128 characters for the map name
5. It is possible to use the name only from the Latin alphabet
6. The maximum size of the card should not exceed 4GB
7. The maximum size of the meta should not exceed 24mb (preview, icon, description, title)
8. Maximum number of vertices per 100 units = 30,000,000
9. There is also a limitation on components

If the map is configured incorrectly, an error will be displayed during uploading, the causes of which you will need to eliminate yourself.

After completing these steps, the map will be published in the **steam workshop**. Initially, the visibility of the downloaded map will be "Private". For example, you can test the **created** map in the game, and it will be visible only to the author. To do this, you need to go _to the Workshop → Track Workshop menu in the game._ You can change the visibility to "Public" on the map page in the **workshop**.

Importantly! At the moment, the "Friends Only" visibility is not working correctly. This is due to a problem on the side of the external library through which we work with the Steam API. We will address this issue in future releases.
