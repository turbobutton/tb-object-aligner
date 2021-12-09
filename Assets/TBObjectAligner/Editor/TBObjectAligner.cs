using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace TButt.Tools
{
	public class TBObjectAligner : EditorWindow
	{
		private GameObject[] _selected;
		private string[] _selectedNames;
		private GUIContent _content;
		private Vector2 _scroll = Vector2.zero;
		private int _toolbarSelection = 0;
		private Vector2 _distributeScroll1 = Vector2.zero;
		private Vector2 _distributeScroll2 = Vector2.zero;

		private int _distributeFirstSelection;
		private int _distributeLastSelection;

		private GUIStyle _styleLabel;
		private GUIStyle _styleBoldLabel;

		private Space _alignmentSpace = Space.World;

		#region CONSTANTS
		private static readonly string LABEL_WINDOW_TITLE = "Object Aligner";

		private static readonly string CENTER_LABEL = "Center";

		private static readonly string TOOLTIP_ALIGN_WORLD_FORMAT = "Align all objects with {0}'s {1} position.";
		private static readonly string TOOLTIP_ALIGN_LOCAL_FORMAT = "Align all objects with {0}'s local {1} axis.";
		private static readonly string TOOLTIP_CENTER_ALIGN_FORMAT = "Align all objects to average {0} position.";

		private static readonly string LABEL_X = "X";
		private static readonly string LABEL_Y = "Y";
		private static readonly string LABEL_Z = "Z";

		private static readonly string UNDO_MESSAGE_ALIGN_FORMAT = "Align objects to {0}'s {1} position";
		private static readonly string UNDO_MESSAGE_CENTER_ALIGN_FORMAT = "Align objects to average {0} position";
		private static readonly string UNDO_MESSAGE_DISTRIBUTE_FORMAT = "Distribute objects on {0} axis";

		private static readonly string[] TOOLBAR_LABELS = new string[] { "Center", "Distribute" };

		private static readonly GUILayoutOption[] LAYOUT_WIDTH_50 = new GUILayoutOption[] { GUILayout.Width(50f) };
		private static readonly GUILayoutOption[] LAYOUT_WIDTH_50_HEIGHT_30 = new GUILayoutOption[] { GUILayout.Width(50f), GUILayout.Height(30f) };
		private static readonly GUILayoutOption[] LAYOUT_MIN_WIDTH_100 = new GUILayoutOption[] { GUILayout.MinWidth(100f) };
		private static readonly GUILayoutOption[] LAYOUT_MIN_WIDTH_100_HEIGHT_30 = new GUILayoutOption[] { GUILayout.MinWidth(100f), GUILayout.Height(30f) };
		private static readonly GUILayoutOption[] LAYOUT_HEIGHT_30 = new GUILayoutOption[] { GUILayout.Height(30f) };
		private static readonly GUILayoutOption[] LAYOUT_WIDTH_200_HEIGHT_30 = new GUILayoutOption[] { GUILayout.Width(200f), GUILayout.Height(30f) };

		private static readonly string LABEL_FIRST = "(First) ";
		private static readonly string LABEL_LAST = "(Last) ";

		private static readonly string HELP_BOX_FORMAT = "Requires at least {0} objects to be selected.";
		private static readonly GUIContent FIRST_OBJECT_LABEL = new GUIContent("First Object", "The other objects in the list will be distributed evenly between this object and the last object.");
		private static readonly GUIContent LAST_OBJECT_LABEL = new GUIContent("Last Object", "The other objects in the list will be distributed evenly between this object and the first object.");
		private static readonly string DISTRIBUTE_BUTTON_X_TOOLTIP = "Distribute objects evenly along the world X axis.";
		private static readonly string DISTRIBUTE_BUTTON_Y_TOOLTIP = "Distribute objects evenly along the world Y axis.";
		private static readonly string DISTRIBUTE_BUTTON_Z_TOOLTIP = "Distribute objects evenly along the world Z axis.";
		private static readonly string DISTRIBUTE_BUTTON_ALL_TOOLTIP = "Distribute objects evenly along the axis defined by the vector between the first and last objects.";
		private static readonly string LABEL_ALL = "All";

		private static readonly GUIContent ALIGNMENT_WORLD_LABEL = new GUIContent ("Alignment: World", "Align with the world axis.");
		private static readonly GUIContent ALIGNMENT_LOCAL_LABEL = new GUIContent ("Alignment: Local", "Align with the selected object's local axis.");

		private static readonly int REQUIRED_SELECTION_COUNT_CENTER = 2;
		private static readonly int REQUIRED_SELECTION_COUNT_DISTRIBUTE = 3;
		#endregion

		[MenuItem("Tools/Object Aligner...")]
		static void Init()
		{
			TBObjectAligner window = (TBObjectAligner)EditorWindow.GetWindow(typeof(TBObjectAligner));

			window.titleContent.text = LABEL_WINDOW_TITLE;
			window.Show();
		}

		private void OnEnable()
		{
			_selected = Selection.gameObjects;
			PopulateSelectedNames();

			_styleBoldLabel = new GUIStyle(EditorStyles.boldLabel);
			_styleBoldLabel.fontSize += 2;

			_styleLabel = new GUIStyle(EditorStyles.label);
			_styleLabel.fontSize += 2;
		}

		void OnFocus()
		{
			// Remove delegate listener if it has previously
			// been assigned.
			SceneView.duringSceneGui -= OnSceneGUI;
			// Add (or re-add) the delegate.
			SceneView.duringSceneGui += OnSceneGUI;
		}

		void OnDestroy()
		{
			// When the window is destroyed, remove the delegate
			// so that it will no longer do any drawing.
			SceneView.duringSceneGui -= OnSceneGUI;
		}

		void OnSceneGUI (SceneView sceneView)
		{
			bool hasSelected = _selected != null && _selected.Length > 0;
			switch (_toolbarSelection)
			{
				case 0:
					if (!hasSelected || _selected.Length < REQUIRED_SELECTION_COUNT_CENTER)
						return;
					break;
				case 1:
					if (!hasSelected || _selected.Length < REQUIRED_SELECTION_COUNT_DISTRIBUTE)
						return;
					break;
			}

			Color startingColor = GUI.color;
			GUI.color = Color.black;

			for (int i = 0; i < _selected.Length; i++)
			{
				GameObject selectedObj = _selected[i];
				string label = selectedObj.name;

				GUIStyle style = _styleLabel;

				switch (_toolbarSelection)
				{
					case 0:
						break;
					case 1:
						bool isFirstSelected = i == _distributeFirstSelection;
						bool isLastSelected = i == _distributeLastSelection;
						if (isFirstSelected || isLastSelected)
						{
							style = _styleBoldLabel;

							if (isFirstSelected)
							{
								label = LABEL_FIRST + label;
							}
							else
							{
								label = LABEL_LAST + label;
							}
						}
						break;
				}

				Handles.Label(selectedObj.transform.position, label, style);
			}

			GUI.color = startingColor;
		}

		private void OnGUI()
		{
			_toolbarSelection = GUILayout.Toolbar(_toolbarSelection, TOOLBAR_LABELS, LAYOUT_HEIGHT_30);

			EditorGUILayout.Space();

			switch (_toolbarSelection)
			{
				case 0:
					DrawCenterSection();
					break;
				case 1:
					DrawDistributeSection();
					break;
			}
		}

		private void OnSelectionChange()
		{
			_selected = Selection.gameObjects;
			PopulateSelectedNames();

			int selectedLength = _selected.Length;

			bool firstWasCappedByLength = false;
			if (_distributeFirstSelection >= selectedLength)
			{
				_distributeFirstSelection = selectedLength - 1;
				firstWasCappedByLength = true;
			}

			if (_distributeLastSelection >= selectedLength)
			{
				_distributeLastSelection = selectedLength - 1;
			}

			if (_distributeFirstSelection == _distributeLastSelection)
			{
				if (firstWasCappedByLength)
				{
					if (_distributeFirstSelection + 1 < selectedLength)
					{
						_distributeFirstSelection += 1;
					}
					else
					{
						_distributeFirstSelection -= 1;
					}
				}
				else
				{
					if (_distributeLastSelection + 1 < selectedLength)
					{
						_distributeLastSelection += 1;
					}
					else
					{
						_distributeLastSelection -= 1;
					}
				}
			}

			_distributeFirstSelection = Mathf.Clamp(_distributeFirstSelection, 0, selectedLength - 1);
			_distributeLastSelection = Mathf.Clamp(_distributeLastSelection, 0, selectedLength - 1);

			EditorWindow.GetWindow(typeof(TBObjectAligner)).Repaint();
		}

		#region CENTERING GUI
		void DrawCenterSection()
		{
			bool hasSelected = _selected != null && _selected.Length > 0;

			EditorGUI.BeginDisabledGroup(!hasSelected || _selected.Length < REQUIRED_SELECTION_COUNT_CENTER);
			DrawCenterButtons();
			EditorGUI.EndDisabledGroup();

			if (!hasSelected || _selected.Length < REQUIRED_SELECTION_COUNT_CENTER)
			{
				EditorGUILayout.HelpBox(string.Format (HELP_BOX_FORMAT, REQUIRED_SELECTION_COUNT_CENTER.ToString ()), MessageType.Warning);
				return;
			}

			bool isWorldSpace = _alignmentSpace == Space.World;
			GUIContent label = isWorldSpace ? ALIGNMENT_WORLD_LABEL : ALIGNMENT_LOCAL_LABEL;
			Color startColor = GUI.backgroundColor;
			GUI.backgroundColor = isWorldSpace ? Color.cyan : Color.yellow;
			if (GUILayout.Button(label, LAYOUT_WIDTH_200_HEIGHT_30))
			{
				if (_alignmentSpace == Space.World)
					_alignmentSpace = Space.Self;
				else
					_alignmentSpace = Space.World;
			}
			GUI.backgroundColor = startColor;

			_scroll = EditorGUILayout.BeginScrollView(_scroll);
			int selectedCount = _selected.Length;
			for (int i = 0; i < selectedCount; i++)
			{
				DrawCenterEntry(_selected[i], i % 2 == 0 ? GUI.color : Color.black);
			}
			EditorGUILayout.EndScrollView();
		}

		void DrawCenterButtons()
		{
			Rect rect = EditorGUILayout.BeginHorizontal();

			Color startColor = GUI.color;
			GUI.color = Color.black;
			GUI.Box(rect, string.Empty);
			GUI.color = startColor;

			EditorGUILayout.LabelField(CENTER_LABEL, EditorStyles.boldLabel, LAYOUT_MIN_WIDTH_100_HEIGHT_30);

			startColor = GUI.backgroundColor;

			GUI.backgroundColor = Color.red;
			if (GUILayout.Button(MakeLabel(LABEL_X, string.Format(TOOLTIP_CENTER_ALIGN_FORMAT, LABEL_X)), LAYOUT_WIDTH_50_HEIGHT_30))
			{
				AlignObjectsCenter(Axis.X);
			}

			GUI.backgroundColor = Color.green;
			if (GUILayout.Button(MakeLabel(LABEL_Y, string.Format(TOOLTIP_CENTER_ALIGN_FORMAT, LABEL_Y)), LAYOUT_WIDTH_50_HEIGHT_30))
			{
				AlignObjectsCenter(Axis.Y);
			}

			GUI.backgroundColor = Color.blue;
			if (GUILayout.Button(MakeLabel(LABEL_Z, string.Format(TOOLTIP_CENTER_ALIGN_FORMAT, LABEL_Z)), LAYOUT_WIDTH_50_HEIGHT_30))
			{
				AlignObjectsCenter(Axis.Z);
			}

			GUI.backgroundColor = startColor;

			EditorGUILayout.EndHorizontal();

			DrawUILine(Color.gray, 1);
		}

		void DrawCenterEntry(GameObject obj, Color backgroundColor)
		{
			Rect rect = EditorGUILayout.BeginHorizontal();

			Color startColor = GUI.color;
			GUI.color = backgroundColor;
			GUI.Box(rect, string.Empty);
			GUI.color = startColor;

			EditorGUILayout.LabelField(obj.name, LAYOUT_MIN_WIDTH_100);

			Color startBackgroundColor = GUI.backgroundColor;

			GUI.backgroundColor = Color.red;
			string tooltip = _alignmentSpace == Space.World ? TOOLTIP_ALIGN_WORLD_FORMAT : TOOLTIP_ALIGN_LOCAL_FORMAT;
			string objectName = obj.name;
			if (GUILayout.Button(MakeLabel(LABEL_X, string.Format(tooltip, objectName, LABEL_X)), LAYOUT_WIDTH_50))
			{
				AlignToObject(obj, Axis.X);
			}

			GUI.backgroundColor = Color.green;
			if (GUILayout.Button(MakeLabel(LABEL_Y, string.Format(tooltip, objectName, LABEL_Y)), LAYOUT_WIDTH_50))
			{
				AlignToObject(obj, Axis.Y);
			}

			GUI.backgroundColor = Color.blue;

			if (GUILayout.Button(MakeLabel(LABEL_Z, string.Format(tooltip, objectName, LABEL_Z)), LAYOUT_WIDTH_50))
			{
				AlignToObject(obj, Axis.Z);
			}

			GUI.backgroundColor = startBackgroundColor;

			EditorGUILayout.EndHorizontal();
		}

		void AlignToObject(GameObject baseObject, Axis axis)
		{
			Transform baseObjectTransform = baseObject.transform;
			Vector3 baseObjectPos = baseObject.transform.position;

			foreach (GameObject obj in _selected)
			{
				if (obj == baseObject)
					continue;

				Vector3 objPos = obj.transform.position;

				switch (_alignmentSpace)
				{
					case Space.World:
						switch (axis)
						{
							case Axis.X:
								objPos.x = baseObjectPos.x;
								break;
							case Axis.Y:
								objPos.y = baseObjectPos.y;
								break;
							case Axis.Z:
								objPos.z = baseObjectPos.z;
								break;
						}
						break;
					case Space.Self:
						switch (axis)
						{
							case Axis.X:
								objPos = ClosestPointOnLine(baseObjectTransform.right, baseObjectPos, objPos);
								break;
							case Axis.Y:
								objPos = ClosestPointOnLine(baseObjectTransform.up, baseObjectPos, objPos);
								break;
							case Axis.Z:
								objPos = ClosestPointOnLine(baseObjectTransform.forward, baseObjectPos, objPos);
								break;
						}
						break;
				}

				Undo.RecordObject(obj.transform, string.Format(UNDO_MESSAGE_ALIGN_FORMAT, baseObject.name, axis.ToString()));

				obj.transform.position = objPos;
			}

			EditorSceneManager.MarkAllScenesDirty();
		}
		#endregion

		#region DISTRIBUTE GUI
		void DrawDistributeSection()
		{
			bool hasSelected = _selected != null && _selected.Length > 0;
			bool disabled = !hasSelected || _selected.Length < REQUIRED_SELECTION_COUNT_DISTRIBUTE;

			if (disabled)
			{
				//EditorGUILayout.BeginScrollView(_distributeScroll1);
				//EditorGUILayout.EndScrollView();
				EditorGUILayout.HelpBox(string.Format (HELP_BOX_FORMAT, REQUIRED_SELECTION_COUNT_DISTRIBUTE.ToString ()), MessageType.Warning);
			}
			else
			{
				EditorGUILayout.BeginVertical();

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(FIRST_OBJECT_LABEL, EditorStyles.whiteLargeLabel);
				EditorGUILayout.LabelField(LAST_OBJECT_LABEL, EditorStyles.whiteLargeLabel);
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space();

				EditorGUILayout.BeginHorizontal();

				EditorGUILayout.BeginVertical();

				_distributeScroll1 = EditorGUILayout.BeginScrollView(_distributeScroll1);
				//_distributeFirstSelection = EditorGUILayout.Popup("First Object", _distributeFirstSelection, _selectedNames);
				//_distributeLastSelection = EditorGUILayout.Popup("Last Object", _distributeLastSelection, _selectedNames);
				int length = _selected.Length;
				Color startColor = GUI.backgroundColor;
				for (int i = 0; i < length; i++)
				{
					if (i == _distributeFirstSelection)
					{
						GUI.backgroundColor = Color.gray;
					}
					else
					{
						GUI.backgroundColor = startColor;
					}

					EditorGUI.BeginDisabledGroup(i == _distributeLastSelection);
					if (GUILayout.Button(_selectedNames[i], EditorStyles.toolbarButton))
					{
						_distributeFirstSelection = i;
					}
					EditorGUI.EndDisabledGroup();
				}

				EditorGUILayout.EndScrollView();

				EditorGUILayout.EndVertical();

				EditorGUILayout.BeginVertical();

				_distributeScroll2 = EditorGUILayout.BeginScrollView(_distributeScroll2);
				for (int i = 0; i < length; i++)
				{
					if (i == _distributeLastSelection)
					{
						GUI.backgroundColor = Color.gray;
					}
					else
					{
						GUI.backgroundColor = startColor;
					}

					EditorGUI.BeginDisabledGroup(i == _distributeFirstSelection);
					if (GUILayout.Button(_selectedNames[i], EditorStyles.toolbarButton))
					{
						_distributeLastSelection = i;
					}
					EditorGUI.EndDisabledGroup();
				}

				EditorGUILayout.EndScrollView();

				EditorGUILayout.EndVertical();

				EditorGUILayout.EndHorizontal();

				EditorGUILayout.EndVertical();
			}

			GUILayout.FlexibleSpace();
			EditorGUI.BeginDisabledGroup(disabled);
			EditorGUILayout.BeginHorizontal();
			Color startBackgroundColor = GUI.backgroundColor;

			GUI.backgroundColor = Color.red;
			if (GUILayout.Button(MakeLabel(LABEL_X, DISTRIBUTE_BUTTON_X_TOOLTIP)))
			{
				DistributeBetweenObjects(_distributeFirstSelection, _distributeLastSelection, Axis.X);
			}

			GUI.backgroundColor = Color.green;
			if (GUILayout.Button(MakeLabel(LABEL_Y, DISTRIBUTE_BUTTON_Y_TOOLTIP)))
			{
				DistributeBetweenObjects(_distributeFirstSelection, _distributeLastSelection, Axis.Y);
			}

			GUI.backgroundColor = Color.blue;

			if (GUILayout.Button(MakeLabel(LABEL_Z, DISTRIBUTE_BUTTON_Z_TOOLTIP)))
			{
				DistributeBetweenObjects(_distributeFirstSelection, _distributeLastSelection, Axis.Z);
			}

			GUI.backgroundColor = Color.yellow;
			if (GUILayout.Button(MakeLabel(LABEL_ALL, DISTRIBUTE_BUTTON_ALL_TOOLTIP)))
			{
				DistributeBetweenObjects(_distributeFirstSelection, _distributeLastSelection, Axis.All);
			}

			GUI.backgroundColor = startBackgroundColor;
			EditorGUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();
		}

		#endregion

		#region FUNCTIONALITY
		void AlignObjectsCenter(Axis axis)
		{
			Vector3 pos = Vector3.zero;

			foreach (GameObject obj in _selected)
			{
				pos += obj.transform.position;
			}

			pos /= _selected.Length;

			foreach (GameObject obj in _selected)
			{
				Vector3 newPosition = obj.transform.position;
				switch (axis)
				{
					case Axis.X:
						newPosition.x = pos.x;
						break;
					case Axis.Y:
						newPosition.y = pos.y;
						break;
					case Axis.Z:
						newPosition.z = pos.z;
						break;
				}

				Undo.RecordObject(obj.transform, string.Format(UNDO_MESSAGE_CENTER_ALIGN_FORMAT, axis.ToString()));
				obj.transform.position = newPosition;
			}
		}

		void DistributeBetweenObjects(int firstSelected, int lastSelected, Axis axis)
		{
			int length = _selected.Length;

			Vector3 firstSelectedPos = _selected[firstSelected].transform.position;
			Vector3 lastSelectedPos = _selected[lastSelected].transform.position;

			Vector3 vecBtwnSelected = lastSelectedPos - firstSelectedPos;

			DistributedObject[] temp = new DistributedObject[length];

			for (int i = 0; i < length; i++)
			{
				GameObject obj = _selected[i];
				temp[i] = new DistributedObject(obj, Vector3.Distance(obj.transform.position, firstSelectedPos) * Mathf.Sign(Vector3.Dot(vecBtwnSelected, obj.transform.position - firstSelectedPos)), i == _distributeFirstSelection || i == _distributeLastSelection);
			}

			List<DistributedObject> sortedObjects = new List<DistributedObject>(temp);
			sortedObjects.Sort((obj1, obj2) => obj1.signedDistFromStart.CompareTo(obj2.signedDistFromStart));

			switch (axis)
			{
				case Axis.X:
					vecBtwnSelected = Vector3.Project(vecBtwnSelected, Vector3.right);
					break;
				case Axis.Y:
					vecBtwnSelected = Vector3.Project(vecBtwnSelected, Vector3.up);
					break;
				case Axis.Z:
					vecBtwnSelected = Vector3.Project(vecBtwnSelected, Vector3.forward);
					break;
			}

			float distBtwnSelected = vecBtwnSelected.magnitude;
			Vector3 dirBtwnSelected = vecBtwnSelected.normalized;
			float increment = distBtwnSelected / (length - 1);

			int movedObjectCount = 0;

			for (int i = 0; i < length; i++)
			{
				if (sortedObjects[i].isStartOrEndObject)
					continue;

				Vector3 targetPos = firstSelectedPos + increment * (movedObjectCount + 1) * dirBtwnSelected;
				Transform thisTransform = sortedObjects[i].gameObject.transform;

				Vector3 newPos = thisTransform.position;
				switch (axis)
				{
					case Axis.X:
						newPos.x = targetPos.x;
						break;
					case Axis.Y:
						newPos.y = targetPos.y;
						break;
					case Axis.Z:
						newPos.z = targetPos.z;
						break;
					case Axis.All:
						newPos = targetPos;
						break;
				}

				string axisString = axis == Axis.All ? "custom" : axis.ToString();
				Undo.RecordObject(thisTransform, string.Format(UNDO_MESSAGE_DISTRIBUTE_FORMAT, axisString));
				thisTransform.position = newPos;
				movedObjectCount++;
			}
		}

		void PopulateSelectedNames()
		{
			int length = _selected.Length;
			_selectedNames = new string[length];
			for (int i = 0; i < length; i++)
			{
				_selectedNames[i] = _selected[i].name;
			}
		}
		#endregion

		#region HELPERS
		private enum Axis
		{
			X,
			Y,
			Z,
			All
		}

		private GUIContent MakeLabel(string label, string tooltip)
		{
			if (_content == null)
				_content = new GUIContent();

			_content.text = label;
			_content.tooltip = tooltip;

			return _content;
		}

		void DrawUILine(Color color, int thickness = 2, int padding = 10)
		{
			Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
			r.height = thickness;
			r.y += padding / 2;
			r.x -= 2;
			r.width += 6;
			EditorGUI.DrawRect(r, color);
		}

		struct DistributedObject
		{
			public GameObject gameObject;
			public float signedDistFromStart;
			public bool isStartOrEndObject;

			public float DistFromStart { get { return Mathf.Abs(signedDistFromStart); } }

			public DistributedObject (GameObject gameObject, float signedDistFromStart, bool isStartOrEndObject)
			{
				this.gameObject = gameObject;
				this.signedDistFromStart = signedDistFromStart;
				this.isStartOrEndObject = isStartOrEndObject;
			}
		}

		Vector3 ClosestPointOnLine(Vector3 v, Vector3 pointOnLine, Vector3 worldPoint)
		{
			Vector3 dir = v.normalized;
			Vector3 vectorBtwnPoints = worldPoint - pointOnLine;
			float dot = Vector3.Dot(vectorBtwnPoints, dir);
			return pointOnLine + dot * dir;
		}
		#endregion
	}
}