using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine.UI.Extension.Tools;

namespace UnityEngine.UI.Extension.ScrollRectItemsAdapter.Editor.CustomEditors
{
	[CustomEditor(typeof(SRIAStandaloneInputModule))]
	public class SmartScrollViewStandaloneInputModuleCustomEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			EditorGUILayout.HelpBox("SRIA: This component is mandatory if building for Universal Windows Platform, but recommended in all cases", MessageType.Info);
		}
	}
}
