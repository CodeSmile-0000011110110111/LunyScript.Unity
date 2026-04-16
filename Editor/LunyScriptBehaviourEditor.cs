using LunyScript.Unity;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace LunyScript.UnityEditor
{
	[CustomEditor(typeof(LunyScriptBehaviour))]
	public class LunyScriptBehaviourEditor : Editor
	{
		public override VisualElement CreateInspectorGUI()
		{
			var root = new VisualElement();

			// Flatten the nested Data elements
			var dataProp = serializedObject.FindProperty("_data");
			if (dataProp != null)
			{
				var child = dataProp.Copy();
				var endProperty = dataProp.GetEndProperty();

				if (child.NextVisible(true) && !SerializedProperty.EqualContents(child, endProperty))
				{
					do
					{
						var field = new PropertyField(child.Copy());
						field.Bind(serializedObject);
						root.Add(field);
					} while (child.NextVisible(false) && !SerializedProperty.EqualContents(child, endProperty));
				}
			}
			else
				root.Add(new Label("Could not find _data property."));

			return root;
		}
	}
}
