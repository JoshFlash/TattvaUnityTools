using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tattva.UnityTools
{

   public static partial class ExtensionMethods
   {
      private const BindingFlags FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;

      private static readonly List<string> TRANSFORM_SKIP_PROPERTIES = new List<string> {"parent", "parentInternal", "hasChanged", "hierarchyCapacity"};
      private static readonly List<string> EMPTY_LIST = new List<string> {};
      
      private static List<string> GetSkipProperties(this System.Type type)
      {
         if (type == typeof(Transform))
         {
            return TRANSFORM_SKIP_PROPERTIES;
         }
         return EMPTY_LIST;
      }

      public static T CopyComponent<T>(this T component, T source)
         where T : Component
      {
         System.Type type = component.GetType();
         if (type == source.GetType())
         {
            List<string> skipProperties = type.GetSkipProperties();
            PropertyInfo[] properties = type.GetProperties(FLAGS);
            foreach (PropertyInfo property in properties)
            {
               if (property.CanWrite && !skipProperties.Contains(property.Name))
               {
                  try
                  {
                     property.SetValue(component, property.GetValue(source, null), null);
                  }
                  catch (Exception e)
                  {
                     Debug.LogError(e.Message);
                  }
               }
            }

            FieldInfo[] fields = type.GetFields(FLAGS);
            foreach (FieldInfo field in fields)
            {
               field.SetValue(component, field.GetValue(source));
            }
         }

         return component;
      }
      
      public static string GetPathInHierarchy(this Transform current) 
      {
         if (current.parent == null)
         {
            return current.name;
         }
         return GetPathInHierarchy(current.parent) + "/" + current.name;
      }
      
   }

   [System.Serializable]
   public class RuntimeAssetUpdater
   {
      private const string TEMP_ASSET_PATH = "Assets/Temp/";
      
      private static readonly Dictionary<string, List<string>> STORED_OBJECTS_BY_SCENE = new Dictionary<string, List<string>>();
      private static readonly Dictionary<ObjectKey, Component> STORED_COMPONENT_BY_OBJECT_KEY = new Dictionary<ObjectKey, Component>();
      
      private static RuntimeAssetUpdater instance = new RuntimeAssetUpdater();
      
      [InitializeOnLoadMethod]
      public static void CreateTransformUpdaterInstance()
      {
         CreateRuntimeAssetUpdater<Transform>();
      }
      
      private static void CreateRuntimeAssetUpdater<T>()
         where T : Component
      {
         EditorApplication.playModeStateChanged += HandlePlayModeStateChanged<T>;
         if (instance == null)
         {
            instance = new RuntimeAssetUpdater();
         }
      }

      private static void HandlePlayModeStateChanged<T>(PlayModeStateChange stateChange)
         where T : Component
      {
         if (stateChange == PlayModeStateChange.EnteredEditMode)
         {
            foreach (string scenePath in STORED_OBJECTS_BY_SCENE.Keys)
            {
               Scene scene = EditorSceneManager.OpenScene(scenePath);

               List<string> objectNames = STORED_OBJECTS_BY_SCENE[scenePath];
               foreach (string name in objectNames)
               {
                  T targetObject = FindStoredObject<T>(name);
                  
                  ObjectKey storedObjectKey = GetStoredObjectKey<T>(name, scenePath);
                  Component sourceObject;
                  if (STORED_COMPONENT_BY_OBJECT_KEY.TryGetValue(storedObjectKey, out sourceObject))
                  {
                     targetObject.CopyComponent(sourceObject);
                  }
               }

               EditorSceneManager.SaveScene(scene);
            }
         }

         if (stateChange == PlayModeStateChange.ExitingEditMode)
         {
            STORED_OBJECTS_BY_SCENE.Clear();
            STORED_COMPONENT_BY_OBJECT_KEY.Clear();
         }
      }

      private static ObjectKey GetStoredObjectKey<T>(string name, string scenePath)
         where T : Component
      {
         return new ObjectKey(typeof(T), name, scenePath);
      }

      private static T FindStoredObject<T>(string pathInHierarchy, string name = "")
         where T : Component
      {
         GameObject storedObject = GameObject.Find(pathInHierarchy);
         if (storedObject != null)
         {
            return storedObject.GetComponent<T>();
         }

         T[] candidates = Resources.FindObjectsOfTypeAll<T>();
         foreach (T candidate in candidates)
         {
            if (candidate.name == name)
            {
               return candidate;
            }
         }

         return null;
      }

      [MenuItem("CONTEXT/Transform/Apply Transform Changes")]
      public static void StoreTransformUpdateValues(MenuCommand menuCommand)
      {
         StoreUpdateValues<Transform>(menuCommand);
      }
      
      public static void StoreUpdateValues<T>(MenuCommand menuCommand)
         where T : Component
      {
         if (Application.isPlaying)
         {
            T objectComponent = menuCommand.context as T;

            Scene objectScene = objectComponent.gameObject.scene;

            if (objectScene.IsValid())
            {
               string scenePath = objectScene.path;
               string objectName = objectComponent.transform.GetPathInHierarchy();
               ObjectKey storedObjectKey = GetStoredObjectKey<T>(objectName, scenePath);
               
               Debug.Log("[RuntimeAssetUpdater] Storing changes on object : " + objectName);
               
               GameObject obj = new GameObject(storedObjectKey.ToString());
               obj.hideFlags = HideFlags.HideInHierarchy;
               T componentCopy = obj.GetComponent<T>();
               if (componentCopy == null)
               {
                  componentCopy = obj.AddComponent<T>();
               }

               if (!Directory.Exists(TEMP_ASSET_PATH))
               {
                  Directory.CreateDirectory(TEMP_ASSET_PATH);
               }

               string assetPath = AssetDatabase.GenerateUniqueAssetPath(TEMP_ASSET_PATH + objectName + ".prefab");
               GameObject copyPrefab = PrefabUtility.CreatePrefab(assetPath, componentCopy.CopyComponent(objectComponent).gameObject);
               
               List<string> storedSceneObjects;
               if (STORED_OBJECTS_BY_SCENE.TryGetValue(scenePath, out storedSceneObjects))
               {
                  if (!storedSceneObjects.Contains(objectName))
                  {
                     storedSceneObjects.Add(objectName);
                  }
               }
               else
               {
                  STORED_OBJECTS_BY_SCENE.Add(scenePath, new List<string> {objectName});
               }

               Component storedComponent;
               if (STORED_COMPONENT_BY_OBJECT_KEY.TryGetValue(storedObjectKey, out storedComponent))
               {
                  STORED_COMPONENT_BY_OBJECT_KEY[storedObjectKey] = copyPrefab.GetComponent<T>();
               }
               else
               {
                  STORED_COMPONENT_BY_OBJECT_KEY.Add(storedObjectKey, copyPrefab.GetComponent<T>());
               }
            }
         }
      }

      private struct ObjectKey
      {
         public System.Type Type;
         public string Name;
         public string ScenePath;

         public ObjectKey(Type type, string name, string scenePath)
         {
            Type = type;
            Name = name;
            ScenePath = scenePath;
         }

         public override string ToString()
         {
            return Type + "." + Name;
         }
      }
      
   }
}
