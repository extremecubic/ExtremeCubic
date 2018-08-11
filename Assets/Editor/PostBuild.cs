using UnityEditor;
using UnityEditor.Callbacks;

public class PostBuild 
{
	[PostProcessBuild(1)]
	public static void CopyLevelsToBuild(BuildTarget target, string buildPath)
	{
		// FileUtil src directory can be relative to the root folder of the project
		FileUtil.ReplaceDirectory("Maps", System.IO.Path.GetDirectoryName(buildPath) + "/Maps");
	}
	
}
