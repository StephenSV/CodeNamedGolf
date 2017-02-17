using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using AIBehaviorEditor;
#endif

using Random = UnityEngine.Random;


namespace AIBehavior
{
	public abstract class BaseState : AIComponent
	{
		public bool isEnabled = true;

		new public string name = "";

		// === Triggers === //

		public BaseTrigger[] triggers = new BaseTrigger[0];

		// === Tagged Object Finder === //

		public bool ownsObjectFinder = false;
		public TaggedObjectFinder objectFinder;

		// === General Variables === //

		public float movementSpeed = 1.0f;
		public float rotationSpeed = 360.0f;
		private float lastActionTime = 0.0f;
		protected float deltaTime = 0.0f;

		// === Animation Variables === //

		public AIAnimationStates animationStatesComponent;
		public AIAnimationState[] animationStates = new AIAnimationState[1];
		protected virtual bool playAnimation { get { return true; } }

		// === Audio Variables === //

		public AudioClip audioClip = null;
		public float audioVolume = 1.0f;
		public float audioPitch = 1.0f;
		public float audioPitchRandomness = 0.0f;
		public bool loopAudio = false;

		// === Item Spawning Variables === //

		public bool spawnItemsOnStateEnter;
		public ItemSpawnMode itemSpawnMode = ItemSpawnMode.AIPosition;
		public Vector3 itemSpawnPoint;
		public Transform itemSpawnTfm;
		public bool exactPosition = false;
		public float totalAmount = 1;
		public GameObject[] spawnableItems;

		public enum ItemSpawnMode
		{
			AIPosition,
			SpawnPoint,
			Transform
		}

		// === State Methods === //

		protected abstract void Init(AIBehaviors fsm);
		protected abstract bool Reason(AIBehaviors fsm);
		protected abstract void Action(AIBehaviors fsm);
		protected abstract void StateEnded(AIBehaviors fsm);


		// === Init === //
		
		public BaseState()
		{
			objectFinder = CreateObjectFinder();
		}
		
		
		protected virtual TaggedObjectFinder CreateObjectFinder()
		{
			return new TaggedObjectFinder();
		}
		
		
		protected virtual void Awake()
		{
			objectFinder.CacheTransforms(CachePoint.Awake);
		}


		public virtual void InitState(AIBehaviors fsm)
		{
			lastActionTime = Time.time;
			deltaTime = 0.0f;

			InitObjectFinder(fsm);
			InitTriggers(fsm);
			Init(fsm);

			if ( playAnimation )
			{
				PlayRandomAnimation(fsm);
			}

			if (spawnItemsOnStateEnter) 
			{
				SpawnItems ();
			}
		}


		void InitObjectFinder (AIBehaviors fsm)
		{
			if ( !objectFinder.useCustomTags )
			{
				objectFinder = fsm.objectFinder;
			}
			else
			{
				ownsObjectFinder = true;
				objectFinder.CacheTransforms(CachePoint.StateChanged);
			}
		}


		// === EndState === //

		public void EndState(AIBehaviors fsm)
		{
			StateEnded(fsm);
		}


		private void InitTriggers(AIBehaviors fsm)
		{
			foreach ( BaseTrigger trigger in triggers )
			{
				if ( trigger != null )
				{
					trigger.HandleInit(fsm, objectFinder);
				}
				else
				{
					Debug.LogError("Null subTrigger in state: " + fsm.currentState.GetDisplayName());
				}
			}
		}


		// === Reason === //
		// Returns true if the state remained the same

		public bool HandleReason(AIBehaviors fsm)
		{
			objectFinder.CacheTransforms(CachePoint.EveryFrame);

			if ( CheckTriggers(fsm) )
			{
				return false;
			}

			return Reason(fsm);
		}


		protected bool CheckTriggers(AIBehaviors fsm)
		{
			foreach ( BaseTrigger trigger in triggers )
			{
				if ( trigger.HandleEvaluate(fsm) )
				{
					return true;
				}
			}

			return false;
		}


		// === Action === //

		public virtual void HandleAction(AIBehaviors fsm)
		{
			CalculateDeltaTime();
			Action(fsm);
		}


		public virtual Vector3 GetNextMovement(AIBehaviors fsm)
		{
			return fsm.transform.position;
		}


		private void CalculateDeltaTime ()
		{
			deltaTime = Time.time - lastActionTime;
			lastActionTime = Time.time;
		}


		// === Animation === //

		public void PlayRandomAnimation(AIBehaviors fsm)
		{
			int animationIndex = 0;

			if ( animationStates.Length > 0 )
			{
				animationIndex = (int)(Random.value * animationStates.Length);
			}

			fsm.PlayAnimation(animationStates[animationIndex]);
		}

		// === Item Spawning === //

		void SpawnItems()
		{
			if (spawnableItems.Length > 0) 
			{
				Vector3 spawnPoint = new Vector3();
				if (itemSpawnMode == ItemSpawnMode.AIPosition) 
				{
					spawnPoint = transform.position;
				} 
				else if (itemSpawnMode == ItemSpawnMode.SpawnPoint) 
				{
					spawnPoint = itemSpawnPoint;
				}
				else if (itemSpawnMode == ItemSpawnMode.Transform) 
				{
					spawnPoint = itemSpawnTfm.position;
				}
					
				for ( int i = 0; i < totalAmount; i++ )
				{
					int itemIndex = Random.Range (0, spawnableItems.Length); // Randomize type of item spawned

					GameObject instance = GameObject.Instantiate(spawnableItems[itemIndex]);

					if (exactPosition) 
					{
						instance.transform.position = spawnPoint;
					} 
					else 
					{
						float spawnDistance = 0.5f;
						float radian = Mathf.Lerp(0, Mathf.PI*2, Mathf.InverseLerp(0, totalAmount, i));
						float maxRandomness = Mathf.PI / (totalAmount+1);
						float x = Mathf.Cos(radian*Random.value*maxRandomness);
						float z = Mathf.Sin(radian*Random.value*maxRandomness);
						instance.transform.position = transform.position + new Vector3(x*spawnDistance, 2, z*spawnDistance);
					}
				}
			} 
			else 
			{
				Debug.LogWarning ("No items in the item list");
			}
		}


		// === Other === //

		public override string ToString ()
		{
			return name;
		}


		public virtual bool RotatesTowardTarget()
		{
			return false;
		}

		public virtual bool CanSwitchToState()
		{
			return true;
		}


#if UNITY_EDITOR
		// === Editor Methods === //

		public virtual void OnStateInspectorEnabled(SerializedObject m_ParentObject) {}


		protected virtual void DrawStateInspectorEditor (SerializedObject stateObject, AIBehaviors stateMachine)
		{
			InspectorHelper.DrawInspector(stateObject);
			stateObject.ApplyModifiedProperties();
		}


		public void OnInspectorEnabled(SerializedObject m_ParentObject)
		{
			SerializedObject m_Object = new SerializedObject(this);

			AIBehaviorsAnimationEditorGUI.OnInspectorEnabled(m_ParentObject, m_Object);

			OnStateInspectorEnabled(m_ParentObject);
		}


		public void DrawInspectorEditor(AIBehaviors fsm)
		{
			// Return in case this is null in the middle of the OnGUI calls
			if ( this == null )
				return;

			SerializedObject stateObject = new SerializedObject(this);
			bool oldEnabled = GUI.enabled;
			bool drawEnabled = DrawIsEnabled(stateObject);

			GUI.enabled = oldEnabled & drawEnabled;

			EditorGUILayout.Separator();
			objectFinder.DrawPlayerTagsSelection(fsm, stateObject, "objectFinder", false);

			AIBehaviorsTriggersGUI.Draw(stateObject, fsm, "Triggers:", "AIBehaviors_TriggersFoldout");
			EditorGUILayout.Separator();

			this.DrawAnimationFields(stateObject);

			DrawFoldouts(stateObject, fsm);

			DrawStateInspectorEditor(stateObject, fsm);

			stateObject.ApplyModifiedProperties();

			GUI.enabled = oldEnabled;
		}

		protected virtual void DrawItemSpawningProperties(SerializedObject m_Object)
		{
			// Item spawning options
			SerializedProperty property = m_Object.FindProperty("spawnItemsOnStateEnter");
			EditorGUILayout.PropertyField (property);

			if (property.boolValue) 
			{
				property = m_Object.FindProperty("itemSpawnMode");
				EditorGUILayout.PropertyField(property);

				if (itemSpawnMode == ItemSpawnMode.SpawnPoint) 
				{
					property = m_Object.FindProperty("itemSpawnPoint");
					EditorGUILayout.PropertyField(property);
				} 
				else if (itemSpawnMode == ItemSpawnMode.Transform) 
				{
					property = m_Object.FindProperty("itemSpawnTfm");
					EditorGUILayout.PropertyField(property);
				}

				property = m_Object.FindProperty("exactPosition");
				EditorGUILayout.PropertyField (property);

				InspectorHelper.DrawArray (m_Object, "spawnableItems");

				property = m_Object.FindProperty("totalAmount");
				EditorGUILayout.PropertyField (property);
			}
		}

		protected virtual void DrawFoldouts(SerializedObject m_Object, AIBehaviors fsm)
		{
			if ( HasMovementOptions() )
			{
				if ( DrawFoldout("movementFoldout", "Movement Properties:") )
				{
					GUILayout.BeginVertical(GUI.skin.box);
					DrawMovementOptions(m_Object);
					GUILayout.EndVertical();
				}
				
				EditorGUILayout.Separator();
			}
			
			if ( DrawFoldout("audioFoldout", "Audio Properties:") )
			{
				GUILayout.BeginVertical(GUI.skin.box);
				DrawAudioProperties(m_Object);
				GUILayout.EndVertical();
			}

			if ( DrawFoldout("itemSpawningFoldout", "Item Spawning Properties:") )
			{
				GUILayout.BeginVertical(GUI.skin.box);
				DrawItemSpawningProperties(m_Object);
				GUILayout.EndVertical();
			}
			
			EditorGUILayout.Separator();
		}



		public bool DrawIsEnabled(SerializedObject m_Object)
		{
			if ( m_Object.targetObject != null )
			{
				SerializedProperty m_isEnabled = m_Object.FindProperty("isEnabled");
				EditorGUILayout.PropertyField(m_isEnabled);
				m_Object.ApplyModifiedProperties();

				return m_isEnabled.boolValue;
			}

			return false;
		}


		protected virtual void DrawAnimationFields(SerializedObject mObject)
		{
			AIBehaviorsAnimationEditorGUI.DrawAnimationFields(mObject, UsesMultipleAnimations());
			EditorGUILayout.Separator();
		}


		protected virtual bool UsesMultipleAnimations()
		{
			return true;
		}


		public void DrawPlayerFields(SerializedObject m_ParentObject)
		{
			SerializedProperty m_Prop = m_ParentObject.FindProperty("checkForNewPlayersInterval");
			EditorGUILayout.PropertyField(m_Prop);
		}


		protected bool DrawFoldout(string foldoutKey, string label)
		{
			bool showProperties = EditorPrefs.GetBool(foldoutKey, false);

			if ( EditorGUILayout.Foldout(showProperties, label, EditorStyles.foldoutPreDrop) != showProperties )
			{
				showProperties = !showProperties;
				EditorPrefs.SetBool(foldoutKey, showProperties);
			}

			return showProperties;
		}


		public void DrawTransitionStatePopup(AIBehaviors fsm, SerializedObject m_Object, string propertyName)
		{
			DrawTransitionStatePopup("Transition to state:", fsm, m_Object, propertyName);
		}


		public void DrawTransitionStatePopup(string label, AIBehaviors fsm, SerializedObject m_Object, string propertyName)
		{
			SerializedProperty m_InitialState = m_Object.FindProperty(propertyName);
			BaseState state = m_InitialState.objectReferenceValue as BaseState;
			BaseState updatedState;

			EditorGUILayout.Separator();

			GUILayout.Label(label, EditorStyles.boldLabel);
			updatedState = AIBehaviorsStatePopups.DrawEnabledStatePopup(fsm, state);
			if ( updatedState != state )
			{
				m_InitialState.objectReferenceValue = updatedState;
				m_Object.ApplyModifiedProperties();
			}
		}


		public void DrawAudioProperties(SerializedObject m_State)
		{
			SerializedProperty m_Property;

			m_Property = m_State.FindProperty("audioClip");
			EditorGUILayout.PropertyField(m_Property);

			m_Property = m_State.FindProperty("audioVolume");
			EditorGUILayout.PropertyField(m_Property);

			m_Property = m_State.FindProperty("audioPitch");
			EditorGUILayout.PropertyField(m_Property);

			m_Property = m_State.FindProperty("audioPitchRandomness");
			EditorGUILayout.PropertyField(m_Property, new GUIContent("Random Pitch (+/-)"));

			m_Property = m_State.FindProperty("loopAudio");
			EditorGUILayout.PropertyField(m_Property);
		}


		protected virtual bool HasMovementOptions()
		{
			return true;
		}


		public void DrawMovementOptions(SerializedObject m_State)
		{
			SerializedProperty m_property;

			// Movement Speed

			m_property = m_State.FindProperty("movementSpeed");
			EditorGUILayout.PropertyField(m_property);

			if ( m_property.floatValue < 0.0f )
				m_property.floatValue = 0.0f;

			// Rotation Speed

			m_property = m_State.FindProperty("rotationSpeed");
			EditorGUILayout.PropertyField(m_property);

			if ( m_property.floatValue < 0.0f )
				m_property.floatValue = 0.0f;
		}
#endif
	}
}