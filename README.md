# Instructions for uploading tracks to workshop</h1>

1. [Preparation of the track loading project](https://github.com/CarXTechnologies/dro-map-uploader#1-preparation-of-the-track-loading-project-)
2. [Import the 3D model of the alignment into the project](https://github.com/CarXTechnologies/dro-map-uploader#2-import-the-3d-model-of-the-alignment-into-the-project-)
3. [Adding core components](https://github.com/CarXTechnologies/dro-map-uploader#3-adding-core-components-)
   1. [Assigning surface collisions](https://github.com/CarXTechnologies/dro-map-uploader#a-assigning-surface-collisions)
   2. [Assigning a spawn point on the map](https://github.com/CarXTechnologies/dro-map-uploader#assigning-a-spawn-point-on-the-map)
   3. [Assigning ambient sounds](https://github.com/CarXTechnologies/dro-map-uploader#assigning-ambient-sounds)
   4. [Adding a mini-map](https://github.com/CarXTechnologies/dro-map-uploader#adding-a-mini-map)
   5. [Capture prototypes](https://github.com/CarXTechnologies/dro-map-uploader#capture-prototypes--icon-preview-minimap)
4. [Uploading the track to the workshop](https://github.com/CarXTechnologies/dro-map-uploader#4-uploading-the-track-to-the-workshop-)
5. [Supported Components](https://github.com/CarXTechnologies/dro-map-uploader#5-supported-components-)
6. [Recommendations](https://github.com/CarXTechnologies/dro-map-uploader#6-requirements-)

## 1. Preparation of the track loading project 🛠
- To get started, you need to download the project archive. You can find the download link here: **[Project](https://github.com/CarXTechnologies/dro-map-uploader)**. Once you have downloaded the archive, extract it to any location on your computer (Code → Download ZIP).

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/new-components/Image/1.png?raw=true" alt="drawing" style="width:600px;"/> <br>

- To open the project, you'll need to install the Unity Editor with version 2020.3.25 (available for 64-bit systems only). You can download it from the following link: **[Download link](https://download.unity3d.com/download_unity/9b9180224418/Windows64EditorInstaller/UnitySetup64-2020.3.25f1.exe)**
- The next step is to launch Unity. Go to the File → Open Project menu, then choose the folder that contains the unpacked project (ensure this folder includes the Assets, Packages, etc. folders).
- When the project is set up, we can proceed to the next step.

## 2. Import the 3D model of the alignment into the project 🚁

- In the _Assets/MapResources/ folder,_ create a folder with the working name of the map.
- Within the created folder, generate a scene through the "Assets → Create → Scene" menu.
- Next, you need to load .fbx/[.obj](https://www.autodesk.com/products/fbx/overview)/[.dae](https://www.khronos.org/collada/) Assets/MapResources/%_your\_folder%/_ folder via Drag & Drop.
- If the materials are not set up within the models, create the materials using the "Assets → Create → Material" menu and configure them as shown below

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/new-components/Image/2.png?raw=true" alt="drawing" style="width:400px;"/> <br>

- Open the created scene and generate a GameObject by dragging the 3d model onto the scene.
- For the created GameObject, add the required components.
- To create a reusable object (Prefab), create the _Assets/MapResources/_%your\_folder%/ Prefabs folder.
- Right-click on the object in the scene and choose "Prefab → Unpack Complete" from the menu.
- The next step is to drag the GameObject from the scene to the newly created "Prefabs" folder. This enables the object to be reused as many times as needed.

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/new-components/Image/3.png?raw=true" alt="drawing" style="width:300px;"/> <br>

## 3. Adding core components ⚙

- The project supports several types of components that are ported into the game. The main ones are:
   - the point where the car appears on the map,
   - Ambient Sounds
   - Physical Materials of Surfaces

- These components are assigned using the **GameMarkerData helper.**

- To add a component to a GameObject or Prefab in the Inspector window, click the _Add Component button_ and type the name _GameMarkerData._
 There is also an option to add a mini-map.

### Assigning surface collisions

- For the track object that representing the surface, choose the GameMarkerData **Road** component type, and in the dropdown, pick the material type that will be used in the game when interacting with this surface.

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/new-components/Image/4.png?raw=true" alt="drawing" style="width:500px;"/> <br>

> *Note that any GameObject/Prefab with collision must also have a Collider component (Box/Sphere/Capsule/Mesh Collider). This condition is necessary for collision accuracy.

### Assigning a spawn point on the map

- To assign a spawn point on the map, create an empty object: choose the GameObect → Create Empty menu item (or Ctrl+Shift+N). In the Transform component, set the coordinates of the point that is most suitable for the car to appear in the game. Add the _GameMarkerData component_, and choose the **SpawnPoint type**

> *Note that only one **vehicle spawn point** should be placed on the map!

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/new-components/Image/5.png?raw=true" alt="drawing" style="width:400px;"/> <br>

### Assigning ambient sounds

- To assign a spawn point on the map, select the GameObect → Create Empty menu item (or Ctrl+Shift+N). Add the _GameMarkerData component, and select the_ Ambient type. **Next, in the dropdown, select the sound type that is the best suitable on the map.**

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/new-components/Image/6.png?raw=true" alt="drawing" style="width:400px;"/> <br>

- When adding an Ambient-type marker, you can use the DrawZoneBehaviour component. It's like a helper that shows where the sounds will be heard. For instance, for an Ambient marker, it draws a zone to indicate where the assigned sounds will be heard.

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/new-components/Image/7.png?raw=true" alt="drawing" style="width:300px;"/> <br>

- If you need another helper script, you can write it yourself and add your own at Assets/Resources/MapSkipComponent

### Template system (*road only)
#### 1. To start using you need to create a template config<br>
   
<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/new-components/Image/22.png?raw=true" alt="drawing" style="width:500px;"/> <br><br>
#### 2. Create and redefine template parameters<br>

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/new-components/Image/23.png?raw=true" alt="drawing" style="width:350px;"/> <br><br>
#### 3. Select the desired template config in the GameMarkerData component<br>

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/new-components/Image/24.png?raw=true" alt="drawing" style="width:500px;"/> <br><br>
#### 4. Select a template for reassigning parameters<br>

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/new-components/Image/25.png?raw=true" alt="drawing" style="width:400px;"/> <br><br>

### Adding a mini-map

- You can also include an optional minimap feature. To do this, create an empty object in the scene (as mentioned earlier) and add a Minimap component. Then, select the Minimap Layer. In the Textures field → Element 0, assign at least one texture - MainTexture. Additionally, while configuring the map, you can use auxiliary functions to create a template. You can upload this template to the graphical editor and design your own minimap based on it. Keep in mind that the map should be centered relative to zero coordinates.
- Bound center represents the minimap's offset relative to the center.
- Bound size indicates the map's size measured in world scale.

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/new-components/Image/8.png?raw=true" alt="drawing" style="width:800px;"/> <br>

### Capture prototypes : icon, preview, minimap

- Added component CaptureCamera for Camera GameObject.
- Set up your camera for your prototype.
- Open component contex menu, and press Capture.
- Save the prototype to disk.<br>
   
<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/new-components/Image/14.png?raw=true" alt="drawing" style="width:300px;"/> <br>

## 4. Uploading the track to the workshop ✈

#### 1. Open window "_Tools/MapBuilder_". <br>

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/new-components/Image/17.png?raw=true)<br>

#### 2. Create or select community item. <br>

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/new-components/Image/18.png?raw=true" alt="drawing" style="width:600px;"/> <br>

> Note! Be sure to add your scenes to your build settings, otherwise they won't be visible in MapBuilder. <br>
> <img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/new-components/Image/15.png?raw=true" alt="drawing" style="width:500px;"/> <br>

#### 3. To upload the track, you initially need to create a map configuration in the map folder. 
<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/new-components/Image/26.png?raw=true" alt="drawing" style="width:600px;"/> <br>

#### 4. Next, you should configure the settings in the configuration file. To do this, you need to:
 - Workshop Name - enter the map name that will be displayed in the workshop<br>
 - Workshop Description(Optional) - description that will be displayed in the workshop<br>
 - Set an **Icon** -the icon of the map that will be displayed in the list of workshop maps in the game itself (read/write enabled item required, png only)<br>
 - Set a **Preview** - map preview, which will be displayed in the workshop and when entering the map in the game itself (read/write enabled item required, png only)

![image](https://github.com/CarXTechnologies/dro-map-uploader/blob/new-components/Image/16.png?raw=true)

#### 5. To set up a scene as a build, select MapMetaConfig in the MapBuilder window<br>

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/new-components/Image/27.png?raw=true" alt="drawing" style="width:600px;"/> <br>

- #### Build Setting
  - Platform - only steam support.
  - Target Scene - selected scene for build map bundle. (*the scene must be in build settings).
  - Build Targets(flags) - select build targets what you want to build/rebuild.
  - *Build* - builds all selected "Build Targets" <br>
<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/new-components/Image/20.png?raw=true" alt="drawing" style="width:400px;"/><br>

- #### Upload Setting
  - Upload Steam Description - if enabled, the description on the workshop page will be updated
  - Upload Steam Name - if enabled, the map name on the workshop page will be updated.
  - Upload Steam Preview - If enabled, the map icon on the Workshop page will be updated.
  - Local Build(*Only test) - If enabled, replaces the current build in steam, but only locally (*and may not always work correctly).
  - *Upload To ...* - Upload to current workshop, of successful all "Build Targets" for selected config<br>
<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/new-components/Image/21.png?raw=true" alt="drawing" style="width:400px;"/> <br>

## 5. Supported Components 📜

- Physics - MeshCollider, BoxCollider, SphereCollider, CapsuleCollider, Rigidbody.
- Graphics - ReflectionProbe, Volume.
- Renderer - MeshRenderer, MeshFilter, Light, LODGroup, ParticleSystemRenderer, VFX particle.
- UI - Canvas, RawImage, TextMeshProUGUI.
- Other - VideoPlayer (1280x720, 30fps, 15sec)

## 6. Requirements 🎈

- Avoid using multiple Directional light sources.
- Ensure the Steam preview does not exceed the limit of 1MB.
- Keep the Steam description within the character limit of 8000.
- Limit the map name to 128 characters for Steam.
- Ensure the maximum map size does not exceed 4GB.
- Ensure the maximum size of the meta does not exceed 24MB (including preview, icon, description, title).
- Be mindful of component limitations.
- Non-convex MeshColider with non-kinematic Rigidbody is no longer supported.
    
If the map is configured incorrectly, an error will be displayed during uploading that will need to address the causes on your own.

After completing these steps, the map will be published in the **steam workshop**. Initially, the visibility of the downloaded map will be "Private". For example, you can test the **created** map in the game, and it will be visible only to the author. To do this, you need to go _to the Workshop → Track Workshop menu in the game._ You can change the visibility to "Public" on the map page in the **workshop**.

> Important note! The "Friends Only" visibility option is currently experiencing issues due to a problem with the external library used for Steam API integration. We plan to resolve this matter in upcoming releases.
