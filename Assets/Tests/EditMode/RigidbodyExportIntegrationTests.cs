using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Plugins.CarX.Modding.Creator.Runtime;
using Plugins.CarX.Modding.Creator.Editor;
public class RigidbodyExportIntegrationTests {
 [NUnit.Framework.TestCase(false)]
 [NUnit.Framework.TestCase(true)] public void ExportsCompoundLodAndVatBindings(bool separateRoots){
 var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("Rigidbody export test");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
 try {
  GameObject Cube(string name,Transform parent){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);return go;}
  var emptyBody=Cube("Skipped body",root.transform);emptyBody.AddComponent<Rigidbody>();emptyBody.GetComponent<BoxCollider>().enabled=false;
  UnityEngine.TestTools.LogAssert.Expect(LogType.Warning,"Rigidbody 'Skipped body' was skipped: no enabled supported colliders. Geometry and animation will still be exported.");
  var box=Cube("Dynamic",root.transform);box.AddComponent<Rigidbody>().mass=7;
  var child=Cube("CompoundChild",box.transform);child.transform.localPosition=Vector3.right*2;
  var lodBody=new GameObject("LOD body");lodBody.transform.SetParent(root.transform,false);lodBody.AddComponent<Rigidbody>();lodBody.AddComponent<BoxCollider>();
  var high=Cube("High",lodBody.transform);var low=Cube("Low",lodBody.transform);UnityEngine.Object.DestroyImmediate(high.GetComponent<BoxCollider>());UnityEngine.Object.DestroyImmediate(low.GetComponent<BoxCollider>());
  var lod=lodBody.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.5f,new[]{high.GetComponent<Renderer>()}),new LOD(.1f,new[]{low.GetComponent<Renderer>()})});
  Cube("Static",root.transform);
  var flag=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MapResources/TestMap/AnimationTest/VAT Flag.prefab"),root.transform);
  if(!flag.GetComponent<Rigidbody>())flag.AddComponent<Rigidbody>();if(!flag.GetComponent<Collider>())flag.AddComponent<BoxCollider>();
  if(separateRoots){
   var twin=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MapResources/TestMap/AnimationTest/VAT Flag.prefab"),root.transform);
   twin.transform.position=new Vector3(20,0,8);
   foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>t.parent==root.transform).ToArray())t.SetParent(null,true);
   var inactive=new GameObject("Inactive body",typeof(Rigidbody),typeof(BoxCollider));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(inactive,scene);inactive.SetActive(false);
   var garbage=Cube("Excluded root",null);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(garbage,scene);garbage.tag="Garbage";garbage.AddComponent<Rigidbody>();
  }
  var exportRoots=separateRoots?scene.GetRootGameObjects().Select(g=>g.transform).ToArray():new[]{root.transform};
  var before=exportRoots.Select(t=>t.localToWorldMatrix).ToArray();
  var result=new SceneFormatCollector(exportRoots,"RbRegression","Garbage").CollectModResults(new EditorCollectionProvider(),ModdingVersion.GetDefaultFullVersionFormat());
  var entries=(IEnumerable)typeof(ModResults).GetField("m_results",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(result);
  StaticHierarchyMeta hierarchy=null;LodHierarchyMeta lods=null;AnimationMeta animation=null;
  foreach(var entry in entries){var value=entry.GetType().GetField("modObject").GetValue(entry);if(value is StaticHierarchyMeta h)hierarchy=h;if(value is LodHierarchyMeta l)lods=l;if(value is AnimationMeta a)animation=a;}
  if(hierarchy==null||hierarchy.rigidbodies.Count!=(separateRoots?4:3)||hierarchy.staticObjects.Count(s=>s.rigidbodyId==1)!=2||hierarchy.staticObjects.Count(s=>s.rigidbodyId==0)!=2||lods.lodInstances.Single().rigidbodyId!=2)throw new Exception("Body/static/LOD binding failed.");
  if(hierarchy.rigidbodies[0].colliders.Count!=2||hierarchy.rigidbodies[1].colliders.Count!=1)throw new Exception("Compound colliders duplicated or missing.");
  if(animation==null||animation.instances.Count!=(separateRoots?2:1)||animation.instances[0].rigidbodyId!=3|| (separateRoots&&animation.instances[1].rigidbodyId!=4))throw new Exception("VAT body binding failed.");
  for(int i=0;i<exportRoots.Length;i++)if(exportRoots[i].localToWorldMatrix!=before[i]||exportRoots[i].parent!=null)throw new Exception("Export modified a source root.");
  var a0=animation.assets.Single();var tex=new Texture2D(a0.width,a0.height,TextureFormat.RGBAHalf,false,true);
  try{tex.LoadRawTextureData(Convert.FromBase64String(a0.positions));tex.Apply();int frame=a0.clips[0].firstFrame+a0.clips[0].frameCount/2;
  if(!Enumerable.Range(0,a0.vertexCount).Any(v=>Mathf.Abs(tex.GetPixel(v%a0.width,v/a0.width).b-tex.GetPixel(v%a0.width,frame*((a0.vertexCount+a0.width-1)/a0.width)+v/a0.width).b)>.01f))throw new Exception("Flag animation stopped after adding Rigidbody.");}
  finally{UnityEngine.Object.DestroyImmediate(tex);}

 }finally{UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}}
}

public class MeshReadabilityExportTests
{
 [NUnit.Framework.Test]
 public void EnablesReadWriteBeforeCollectingAnImportedMeshCollider()
 {
  string path="Assets/__MeshReadabilityTest_"+Guid.NewGuid().ToString("N")+".obj";
  GameObject instance=null;
  try
  {
   File.WriteAllText(path,"o ColliderMesh\nv 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n");
   AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
   var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.isReadable=false;importer.SaveAndReimport();
   var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
   instance=UnityEngine.Object.Instantiate(asset);
   var filter=instance.GetComponentInChildren<MeshFilter>();
   NUnit.Framework.Assert.IsFalse(filter.sharedMesh.isReadable);
   var body=instance.AddComponent<Rigidbody>();body.isKinematic=true;
   filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
   RigidbodyExporter.EnsureMeshReadability(new[]{instance.transform});
   NUnit.Framework.Assert.IsTrue(((ModelImporter)AssetImporter.GetAtPath(path)).isReadable);
   var data=RigidbodyExporter.Collect(body,_=>false);
   NUnit.Framework.Assert.AreEqual(1,data.colliders.Count);
   NUnit.Framework.Assert.AreEqual(3,data.colliders[0].vertices.Length);
   NUnit.Framework.Assert.AreEqual(3,data.colliders[0].triangles.Length);
  }
  finally { if(instance!=null)UnityEngine.Object.DestroyImmediate(instance);AssetDatabase.DeleteAsset(path); }
 }
}
