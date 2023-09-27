using System;
using UnityEngine;

namespace Systems.MobAIs
{
	public class MimicAI : GenericHostileAI
	{
		[Header("Mimic Data")]

		[Tooltip("If player enough close mimic transofrms from object to itself and starts attacking")]
		[SerializeField] private float attackDistance;
		[Tooltip("For correct behaviour hide distance must be higher than attack distance")]
		[SerializeField] private float hideDistance;

		[Tooltip("Distance for searching object to transform into it")]
		[SerializeField, Range(0, 10)] private float searchObjectStep;
		[Tooltip("Layers where mimic finds object to transform")]
		[SerializeField] private LayerMask searchObjectMask;
		[Tooltip("Own sprite handler")]
		[SerializeField] private SpriteHandler spriteHandler;

		private bool isHided;
		private SpriteDataSO originalSpriteData;

		protected override void Awake()
		{
			base.Awake();
			isHided = false;
			originalSpriteData = spriteHandler.GetCurrentSpriteSO();
			mobFlee.Deactivate();
		}

		/// <summary>
		/// Prioirity must be increased to avoid expolring
		/// </summary>
		public override void ContemplatePriority()
		{
			base.ContemplatePriority();
			Priority++;
		}

		/// <summary>
		/// Mimic moves randomly in case player is not too close
		/// If player is in hide distance mimic transofrms to random object
		/// If player is in attack distance mimic transforms to itself and begins attacking
		/// If player went away from hide distance mimic returns to own view
		/// </summary>
		protected override void HandleSearch()
		{
			moveWaitTime += MobController.UpdateTimeInterval;
			searchWaitTime += MobController.UpdateTimeInterval;
			if (!(searchWaitTime >= searchTickRate)) return;
			searchWaitTime = 0f;

			var findTarget = SearchForTarget();
			if (findTarget != null)
			{
				float distance = Vector3.Distance(registerObject.WorldPositionServer, findTarget.AssumedWorldPosServer());

				if(distance <= attackDistance && !mobFlee.activated)
				{
					TurnIntoOrigin();
					BeginAttack(findTarget);
					return;
				}

				bool isFarToHide = distance > hideDistance;
				if(isHided)
				{
					if(isFarToHide)
					{
						TurnIntoOrigin();
					}
					return;
				}

				if(!isFarToHide)
				{
					Hide();
					return;
				}
			}

			if(moveWaitTime > movementTickRate)
			{
				DoRandomMove();
				moveWaitTime = 0;
			}
			
			BeginSearch();
		}

		/// <summary>
		/// If mimic gets damage it starts fleeing
		/// </summary>
		/// <param name="damagedBy"></param>
		protected override void OnAttackReceived(GameObject damagedBy = null)
		{
			if(isHided)
			{
				TurnIntoOrigin();
			}

			StopFollowing();
			StartFleeing(damagedBy);
		}

		/// <summary>
		/// Ovverided to avoid action from local chat
		/// </summary>
		/// <param name="chatEvent"></param>
		public override void LocalChatReceived(ChatEvent chatEvent) { }

		/// <summary>
		/// Transforms to random object which is near
		/// </summary>
		private void Hide()
		{
			isHided = true;

			if (mobFlee.activated)
			{
				StopFleeing();
			}

			TurnIntoObject(SearchForObject());
		}

		/// <summary>
		/// Searching random object near itselft
		/// Increases searching distance if can`t find an object
		/// </summary>
		/// <returns></returns>
		private GameObject SearchForObject()
		{
			int iterationCountLimit = 50;
			float currentStep = searchObjectStep;
			Collider2D[] colliders;

			do
			{
				colliders = Physics2D.OverlapCircleAll(registerObject.WorldPositionServer.To2Int(), currentStep, searchObjectMask);
				currentStep += searchObjectStep;
				iterationCountLimit--;
			} while (iterationCountLimit > 0 && colliders.Length == 0);

			return colliders.PickRandom().gameObject;
		}

		/// <summary>
		/// Returns to own view
		/// </summary>
		private void TurnIntoOrigin()
		{
			isHided = false;
			spriteHandler.SetSpriteSO(originalSpriteData);
		}

		/// <summary>
		/// Takes view of other object
		/// </summary>
		/// <param name="gameObject"></param>
		private void TurnIntoObject(GameObject gameObject)
		{
			isHided = true;

			var objectSpriteHander = gameObject.GetComponentInChildren<SpriteHandler>();
			if(objectSpriteHander != null)
			{
				spriteHandler.SetSpriteSO(objectSpriteHander.GetCurrentSpriteSO());
				string objName = gameObject.GetComponent<ObjectAttributes>().name;
				Chat.AddActionMsgToChat(gameObject ,$"{mobName} has morphed into {objName}");
			}
		}

		private void OnValidate()
		{
			if(attackDistance >= hideDistance)
			{
				hideDistance++;
			}
		}
	}
}
