using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using AIBehaviorEditor;
#endif


namespace AIBehavior
{
	public class SeekState : BaseState
	{
		public bool specifySeekTarget = false;
		public Transform seekTarget = null;
		Vector3 targetPosition = Vector3.zero;

		public BaseState seekTargetReachedState;
		public BaseState noSeekTargetFoundState;
		public float distanceToTargetThreshold = 0.25f;
		private float sqrDistanceToTargetThreshold = 1.0f;

		public bool destroyTargetWhenReached = false;

		public bool getAsCloseAsPossible = false;
		public BaseState noMovementState;
		private NavMeshAgent navMeshAgent = null;
		private NavMeshPath navMeshPath = null;

		Vector3 previousPosition = Vector3.zero;
		float previousNoMovementDistance = -1;


		protected override void Init(AIBehaviors fsm)
		{
			navMeshAgent = fsm.GetComponentInChildren<NavMeshAgent>();
			sqrDistanceToTargetThreshold = GetSquareDistanceThreshold();
			fsm.PlayAudio();

			getAsCloseAsPossible &= navMeshAgent != null;
			previousNoMovementDistance = -1;

			if ( getAsCloseAsPossible )
			{
				previousPosition = Vector3.one * Mathf.Infinity;
				navMeshPath = new NavMeshPath();
			}

			if ( seekTarget != null )
			{
				targetPosition = seekTarget.position;
			}
		}


		protected override void StateEnded(AIBehaviors fsm)
		{
			if ( !specifySeekTarget )
			{
				seekTarget = null;
			}
		}


		protected override bool Reason(AIBehaviors fsm)
		{
			if ( seekTarget == null )
			{
				Transform[] seekTfms = objectFinder.GetTransforms();
				Vector3 pos = fsm.transform.position;
				Vector3 diff;
				float nearestItem = Mathf.Infinity;

				if ( seekTfms == null )
				{
					fsm.ChangeActiveState(noSeekTargetFoundState);
				}

				foreach ( Transform tfm in seekTfms )
				{
					float sqrMagnitude;

					diff = tfm.position - pos;
					sqrMagnitude = diff.sqrMagnitude;

					if ( sqrMagnitude < nearestItem )
					{
						seekTarget = tfm;
						nearestItem = sqrMagnitude;
						targetPosition = seekTarget.position;
					}
				}
			}
			else
			{
				float sqrDist = (fsm.transform.position - targetPosition).sqrMagnitude;

				if ( sqrDist < sqrDistanceToTargetThreshold )
				{
					HandleTargetReached(fsm);
					return false;
				}
			}

			return true;
		}


		void HandleTargetReached(AIBehaviors fsm)
		{
			if ( destroyTargetWhenReached )
			{
				Destroy(seekTarget.gameObject);
			}

			fsm.ChangeActiveState(seekTargetReachedState);
		}


		protected override void Action(AIBehaviors fsm)
		{
			if ( seekTarget != null )
			{
				targetPosition = GetNextMovement(fsm);

				if ( HasMovement(fsm.transform.position) )
				{
					fsm.MoveAgent(targetPosition, movementSpeed, rotationSpeed);
				}
				else
				{
					fsm.ChangeActiveState(noMovementState);
				}
			}

			previousPosition = fsm.transform.position;
		}


		bool HasMovement(Vector3 currentPosition)
		{
			float movementDistance = (previousPosition - currentPosition).sqrMagnitude;

			if ( movementDistance < 0.001f * 0.001f )
			{
				if ( movementDistance <= previousNoMovementDistance )
				{
					return false;
				}

				previousNoMovementDistance = movementDistance;
			}

			return true;
		}


		public override Vector3 GetNextMovement (AIBehaviors fsm)
		{
			if ( seekTarget != null )
			{
				if ( getAsCloseAsPossible )
				{
					if ( navMeshPath.status == NavMeshPathStatus.PathInvalid )
					{
						RecalculatePath();
						return seekTarget.position;
					}
					else
					{
						Vector3[] corners = navMeshPath.corners;

						if ( corners.Length > 0 )
						{
							Vector3 target = corners[corners.Length-1];
							navMeshAgent.CalculatePath(seekTarget.position, navMeshPath);

							return target;
						}
						else
						{
							RecalculatePath();
							return seekTarget.position;
						}
					}

					return targetPosition;
				}
				else
				{
					return seekTarget.position;
				}
			}

			return base.GetNextMovement (fsm);
		}


		float checkInterval = 1f;
		float nextCheck = 0.0f;

		void RecalculatePath()
		{
			if ( Time.realtimeSinceStartup > nextCheck )
			{
				navMeshAgent.CalculatePath(seekTarget.position, navMeshPath);
				nextCheck = Time.realtimeSinceStartup + checkInterval;
			}
		}


		protected virtual float GetSquareDistanceThreshold ()
		{
			return distanceToTargetThreshold * distanceToTargetThreshold;
		}
		
		
		public override string DefaultDisplayName()
		{
			return "Seek";
		}


		void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(targetPosition, 0.5f);
		}


		#if UNITY_EDITOR
		// === Editor Methods === //

		public override void OnStateInspectorEnabled(SerializedObject m_ParentObject)
		{
		}


		protected override void DrawStateInspectorEditor(SerializedObject stateObject, AIBehaviors fsm)
		{
			SerializedProperty property;

			GUILayout.Label ("Seek Properties:", EditorStyles.boldLabel);
			
			GUILayout.BeginVertical(GUI.skin.box);

			property = stateObject.FindProperty("specifySeekTarget");
			EditorGUILayout.PropertyField(property);

			if ( property.boolValue )
			{
				property = stateObject.FindProperty("seekTarget");
				EditorGUILayout.PropertyField(property);
			}

			EditorGUILayout.Separator();
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("No Seek Target Transition:");
				property = stateObject.FindProperty("noSeekTargetFoundState");
				property.objectReferenceValue = AIBehaviorsStatePopups.DrawEnabledStatePopup(fsm, property.objectReferenceValue as BaseState);
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("No Movement Transition:");
				property = stateObject.FindProperty("noMovementState");
				property.objectReferenceValue = AIBehaviorsStatePopups.DrawEnabledStatePopup(fsm, property.objectReferenceValue as BaseState);
			}
			GUILayout.EndHorizontal();

			EditorGUILayout.Separator();
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Seek Target Reached Transition:");
				property = stateObject.FindProperty("seekTargetReachedState");
				property.objectReferenceValue = AIBehaviorsStatePopups.DrawEnabledStatePopup(fsm, property.objectReferenceValue as BaseState);
			}
			GUILayout.EndHorizontal();

			property = stateObject.FindProperty("distanceToTargetThreshold");
			float prevValue = property.floatValue;
			EditorGUILayout.PropertyField(property);

			if ( property.floatValue <= 0.0f )
				property.floatValue = prevValue;

			property = stateObject.FindProperty("destroyTargetWhenReached");
			EditorGUILayout.PropertyField(property);

			property = stateObject.FindProperty("getAsCloseAsPossible");
			EditorGUILayout.PropertyField(property);

			if ( property.boolValue )
			{
				if ( fsm.GetComponentInChildren<NavMeshAgent>() == null )
				{
					EditorGUILayout.HelpBox("This trigger only works with an AI that uses a NavMeshAgent", MessageType.Warning);
				}
			}

			GUILayout.EndVertical();

			stateObject.ApplyModifiedProperties();

			if ( Application.isPlaying )
			{
				GUILayout.Label ("Debug:", EditorStyles.boldLabel);

				GUILayout.BeginVertical(GUI.skin.box);
				if ( seekTarget != null )
				{
					GUILayout.Label("Distance to target: " + (transform.position - targetPosition).magnitude.ToString(), EditorStyles.boldLabel);
				}
				else
				{
					GUILayout.Label("No Seek Target Found", EditorStyles.boldLabel);
				}
				GUILayout.EndVertical();
			}
		}
	#endif
	}
}