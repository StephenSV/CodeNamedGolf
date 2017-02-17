using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AIBehavior
{
	public class MecanimAttackState : AttackState
	{
		private Animator animator;
		float prevNormalizedTime = 0.0f;
		public int mecanimLayerIndex = 0;


		protected override void Awake()
		{
			base.Awake();

			animator = transform.parent.GetComponentInChildren<Animator>();

			if ( animator == null )
			{
				Debug.LogWarning("An Animator component must be attached when using the " + this.GetType());
			}
		}


		protected override void Init (AIBehaviors fsm)
		{
			prevNormalizedTime = 0.0f;
			base.Init(fsm);
		}


		protected override void HandleAnimationAttackMode (AIBehaviors fsm, Transform target)
		{
			if ( scriptWithAttackMethod != null && !string.IsNullOrEmpty(methodName) )
			{
				AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(mecanimLayerIndex);
				int hash = Animator.StringToHash(animationStates[0].name);

				if ( hash == stateInfo.fullPathHash )
				{
					float curNormalizedTime = stateInfo.normalizedTime % 1.0f;

					if ( curNormalizedTime > attackPoint && prevNormalizedTime < attackPoint )
					{
						Attack(fsm, target);
					}

					prevNormalizedTime = curNormalizedTime;
				}
			}
		}


		protected override void StateEnded (AIBehaviors fsm)
		{
			base.StateEnded(fsm);
		}
		
		
		public override string DefaultDisplayName()
		{
			return "Mecanim Attack";
		}


#if UNITY_EDITOR
		// === Editor Methods === //
		protected override void DrawStateInspectorEditor(SerializedObject m_State, AIBehaviors fsm)
		{
			SerializedProperty m_property = m_State.FindProperty("mecanimLayerIndex");

			EditorGUILayout.PropertyField(m_property);

			base.DrawStateInspectorEditor(m_State, fsm);
		}
#endif
	}
}