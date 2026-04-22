using Luny;
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LunyScript.UnityEditor
{
	internal sealed class CreateLunyScriptAsset : EndNameEditAction
	{
		private static readonly String s_DefaultLunyScript = @"using LunyScript;

public partial class $CLASSNAME$ : Script
{
	public override void Build()
	{
		var hello = Var.Define(""hello"", ""Hello, "");
		var world = Var.Define(""world"", nameof($CLASSNAME$));

		On.Ready(Debug.Log(hello + world + ""!""));
	}
}";

		[MenuItem("Assets/Create/Luny Script", priority = -222)]
		[MenuItem("Assets/Create/Scripting/Luny Script", priority = -140)]
		private static void CreateLunyScript()
		{
			var icon = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
			var endEditAction = CreateInstance<CreateLunyScriptAsset>();

			// This starts the "rename mode" in the Project Window
			ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, endEditAction, "NewLunyScript.cs", icon, null, true);
		}

		private static String SetLineEndings(String content, LineEndingsMode lineEndingsMode) =>
			Regex.Replace(content, "\\r\\n?|\\n", GetLineEndings(lineEndingsMode));

		private static String GetLineEndings(LineEndingsMode lineEndingsMode) => lineEndingsMode switch
		{
			LineEndingsMode.OSNative => Application.platform != RuntimePlatform.WindowsEditor ? "\n" : "\r\n",
			LineEndingsMode.Unix => "\n",
			LineEndingsMode.Windows => "\r\n",
			var _ => "\n",
		};

		public override void Action(Int32 instanceId, String assetPath, String resourceFilePath)
		{
			try
			{
				var className = Path.GetFileNameWithoutExtension(assetPath).SanitizeIdentifier();
				var directoryName = Path.GetDirectoryName(assetPath);
				assetPath = $"{directoryName}/{className}.cs";

				var script = s_DefaultLunyScript.Replace("$CLASSNAME$", className);
				script = SetLineEndings(script, EditorSettings.lineEndingsForNewScripts);

				File.WriteAllText(assetPath, script);
				AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

				var scriptAsset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
				ProjectWindowUtil.ShowCreatedAsset(scriptAsset);
			}
			catch (Exception e)
			{
				LunyLogger.LogException(e, nameof(CreateLunyScriptAsset));
			}
		}
	}
}
