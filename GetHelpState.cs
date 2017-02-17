using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace AIBehavior
{
	public class GetHelpState : CooldownableState
	{
		public float helpRadius = 30.0f;
		public string helpTag = "Untagged";


		protected override void Init(AIBehaviors fsm)
		{
			GameObject[] gos = GetPotentialHelpers();
			Vector3 tfmPosition = fsm.transform.position;

			foreach ( GameObject go in gos )
			{
				if ( Vector3.Distance(go.transform.position, tfmPosition) < helpRadius )
				{
					Vector3 aiTargetPosition = tfmPosition;
					aiTargetPosition.y = go.transform.position.y;

					AIBehaviors helperFSM = go.GetComponent<AIBehaviors>();

					if ( helperFSM != null )
					{
						HelpAnotherFSM(aiTargetPosition, helperFSM);
					}
				}
			}

			fsm.PlayAudio();
			fsm.gameObject.SendMessage("OnGetHelp", SendMessageOptions.DontRequireReceiver);
		}


		protected virtual GameObject[] GetPotentialHelpers()
		{
			return GameObject.FindGameObjectsWithTag(helpTag);
		}


		// Change the other FSMs state to the HelpState
		private void HelpAnotherFSM(Vector3 helpTargetPosition, AIBehaviors otherFSM)
		{
			HelpState helpState = null;

			helpState = otherFSM.GetState<HelpState>();

			if ( helpState != null && helpState.CanHelp(otherFSM))
			{
				helpState.helpPoint = helpTargetPosition;
				otherFSM.ChangeActiveState(helpState);
			}
		}


		protected override void StateEnded(AIBehaviors fsm)
		{
			base.StateEnded (fsm);
		}


		protected override bool Reason(AIBehaviors fsm)
		{
			return true;
		}


		protected override void Action(AIBehaviors fsm)
		{
		}
		
		
		public override string DefaultDisplayName()
		{
			return "Get Help";
		}

		
	#if UNITY_EDITOR
		// === Editor Methods === //

		public override void OnStateInspectorEnabled(SerializedObject m_ParentObject)
		{
		}


		protected override void DrawStateInspectorEditor(SerializedObject stateObject, AIBehaviors fsm)
		{
			SerializedProperty property;

			GUILayout.Label ("Get Help Properties:", EditorStyles.boldLabel);
			
			GUILayout.BeginVertical(GUI.skin.box);

			GUILayout.BeginHorizontal ();
			GUILayout.Label("Get Help From Objects With Tag");
			string newTag = EditorGUILayout.TagField(helpTag);

			if ( newTag != helpTag )
			{
				property = stateObject.FindProperty("helpTag");
				property.stringValue = newTag;
			}
			GUILayout.EndHorizontal ();

			EditorGUILayout.Separator();

			property = stateObject.FindProperty("helpRadius");
			EditorGUILayout.PropertyField(property);

			GUILayout.EndVertical();

			stateObject.ApplyModifiedProperties();
		}
	#endif
	}
}