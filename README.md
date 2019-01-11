# Tattva Unity Tools
Shortcuts Window:
Copy and paste the code found in this file into a script called "ShortcutsWindow.cs" in your Unity project (or download the file)
Navigate to Window->Shortcuts 

Runtime Asset Updater:
While in Play Mode, right-click the corresponding component and select "Apply [component] Changes." Camera and Transform are enabled by default. To alolow other components to be modified at runtime, simply add CreateRuntimeAssetUpdater<[component]>() to the static constructor and create the following corresponding method:
```
      [MenuItem("CONTEXT/[component]/Apply Transform Changes")]
      public static void Store[component]UpdateValues(MenuCommand menuCommand)
      {
         StoreUpdateValues<[component]>(menuCommand);
      }
```
