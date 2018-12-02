using UnityEditor;
using UnityEngine;
using System.IO;
using Sone;
using Simplex;

public static class ScriptableObjectUtility
{
	/// <summary>
	///	This makes it easy to create, name and place unique new ScriptableObject asset files.
	/// Instantiates new ScriptableObject at selected directory.
	/// </summary>
	public static T CreateAsset<T>() where T : ScriptableObject
	{
		return CreateAsset<T>(AssetDatabase.GetAssetPath(Selection.activeObject), "New " + typeof(T).ToString());
	}

	/// <summary>
	///	This makes it easy to create, name and place unique new ScriptableObject asset files.
	/// Instantiates new ScriptableObject at given directory.
	/// </summary>
	public static T CreateAsset<T>(string path) where T : ScriptableObject
	{
		return CreateAsset<T>(path, "New " + typeof(T).ToString());
	}

	/// <summary>
	///	This makes it easy to create, name and place unique new ScriptableObject asset files.
	/// Instantiates new ScriptableObject at given directory with custom name.
	/// </summary>
	public static T CreateAsset<T>(string path, string name) where T : ScriptableObject
	{
		T asset = ScriptableObject.CreateInstance<T>();

		if (path == "")
			path = "Assets";
		else if (Path.GetExtension(path) != "")
			path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");

		string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + name + ".asset");

		AssetDatabase.CreateAsset(asset, assetPathAndName);

        Selection.activeObject = asset;

        AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		EditorUtility.FocusProjectWindow();

		return asset;
	}

    [MenuItem("Assets/Create/UniversalControlSettings")]
    public static void CreateUniversalControlSettings()
    {
        CreateAsset<UniversalControlSettings>();
    }
    
}