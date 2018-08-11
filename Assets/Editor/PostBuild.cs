using UnityEditor;
using UnityEditor.Callbacks;

public class PostBuild 
{
	[PostProcessBuild(1)]
	public static void CopyLevelsToBuild(BuildTarget target, string buildPath)
	{
		FileUtil.ReplaceDirectory(Constants.TILEMAP_SAVE_FOLDER, System.IO.Path.GetDirectoryName(buildPath) + "/Maps");
	}
	
}
