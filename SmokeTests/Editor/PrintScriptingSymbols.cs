using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace SmokeTests.Editor
{
	public class PrintScriptingSymbols : EditorWindow
	{
		[MenuItem("Window/CodeSmile/Print User Script Symbols")]
		public static void PrintActiveSymbols()
		{
			var activeTarget = EditorUserBuildSettings.activeBuildTarget;
			var targetGroup = BuildPipeline.GetBuildTargetGroup(activeTarget);
			var namedTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
			var symbols = PlayerSettings.GetScriptingDefineSymbols(namedTarget);

			Debug.Log($"<b>User Script Symbols for {targetGroup}:</b>\n{symbols}");
		}

		[MenuItem("Window/CodeSmile/Print All Script Symbols")]
		public static void Print()
		{
			var mainProjectFile = "Assembly-CSharp.csproj";
			var projectPath = Path.Combine(Directory.GetCurrentDirectory(), mainProjectFile);
			if (!File.Exists(projectPath))
			{
				Debug.LogError($"{mainProjectFile} does not exist. You may have to 'Generate .csproj files' first.");
				return;
			}

			var doc = XDocument.Load(projectPath);
			var ns = doc.Root.Name.Namespace;
			var defines = doc.Descendants(ns + "DefineConstants").FirstOrDefault()?.Value;

			if (!String.IsNullOrEmpty(defines))
			{
				var sortedList = defines.Split(';').OrderBy(symbol => symbol).ToArray();
				string formattedList = string.Join("\n", sortedList);
				Debug.Log($"<b>All active Script Symbols:</b>\n{formattedList}");
			}
		}
	}
}
