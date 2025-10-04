// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using GlobalEnums;
// using GlobalSettings;
// using HutongGames.PlayMaker;
// using TeamCherry.SharedUtils;
// using UnityEngine;
// using UnityEngine.Events;

// // Token: 0x020002C8 RID: 712
// public class DamageHero : MonoBehaviour, IInitialisable
// {
// 	// Token: 0x14000047 RID: 71
// 	// (add) Token: 0x0600196C RID: 6508 RVA: 0x000748A0 File Offset: 0x00072AA0
// 	// (remove) Token: 0x0600196D RID: 6509 RVA: 0x000748D8 File Offset: 0x00072AD8
// 	public event Action HeroDamaged;

// 	// Token: 0x1700029E RID: 670
// 	// (get) Token: 0x0600196E RID: 6510 RVA: 0x0007490D File Offset: 0x00072B0D
// 	public bool OverrideCollisionSide
// 	{
// 		get
// 		{
// 			return this.overrideCollisionSide;
// 		}
// 	}

// 	// Token: 0x1700029F RID: 671
// 	// (get) Token: 0x0600196F RID: 6511 RVA: 0x00074915 File Offset: 0x00072B15
// 	public CollisionSide CollisionSide
// 	{
// 		get
// 		{
// 			return this.collisionSide;
// 		}
// 	}

// 	// Token: 0x170002A0 RID: 672
// 	// (get) Token: 0x06001970 RID: 6512 RVA: 0x0007491D File Offset: 0x00072B1D
// 	public bool InvertCollisionSide
// 	{
// 		get
// 		{
// 			return this.invertCollisionSide;
// 		}
// 	}

// 	// Token: 0x170002A1 RID: 673
// 	// (get) Token: 0x06001971 RID: 6513 RVA: 0x00074925 File Offset: 0x00072B25
// 	public bool CanCauseDamage
// 	{
// 		get
// 		{
// 			return Time.timeAsDouble >= this.damageAllowedTime;
// 		}
// 	}

// 	// Token: 0x06001972 RID: 6514 RVA: 0x00074937 File Offset: 0x00072B37
// 	private bool? IsFsmEventValid(string eventName)
// 	{
// 		return this.HeroDamagedFSM.IsEventValid(eventName, false);
// 	}

// 	// Token: 0x06001973 RID: 6515 RVA: 0x00074948 File Offset: 0x00072B48
// 	private bool? IsFsmBoolValid(string eventName)
// 	{
// 		if (string.IsNullOrEmpty(eventName) || !this.HeroDamagedFSM)
// 		{
// 			return null;
// 		}
// 		return new bool?(this.HeroDamagedFSM.FsmVariables.BoolVariables.Any((FsmBool fsmBool) => fsmBool.Name == eventName));
// 	}

// 	// Token: 0x06001974 RID: 6516 RVA: 0x000749AC File Offset: 0x00072BAC
// 	public bool OnAwake()
// 	{
// 		if (this.hasAwaken)
// 		{
// 			return false;
// 		}
// 		this.hasAwaken = true;
// 		if (this.damageAsset)
// 		{
// 			this.damageDealt = this.damageAsset.Value;
// 		}
// 		this.healthManager = base.GetComponentInParent<HealthManager>();
// 		if (this.healthManager)
// 		{
// 			this.healthManagerColliders = this.healthManager.GetComponents<Collider2D>();
// 			this.healthManager.TookDamage += this.OnDamaged;
// 		}
// 		this.recoil = base.GetComponentInParent<Recoil>();
// 		this.nonBouncer = base.GetComponentInParent<NonBouncer>();
// 		this.hasNonBouncer = (this.nonBouncer != null);
// 		Rigidbody2D rigidbody2D = base.GetComponent<Rigidbody2D>();
// 		this.collider = base.GetComponent<Collider2D>();
// 		if (this.canClashTink && !this.noTerrainThunk && !rigidbody2D && this.collider)
// 		{
// 			this.collider.isTrigger = false;
// 			rigidbody2D = base.gameObject.AddComponent<Rigidbody2D>();
// 			rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
// 			rigidbody2D.simulated = true;
// 			rigidbody2D.useFullKinematicContacts = true;
// 		}
// 		if (base.transform.parent)
// 		{
// 			this.parentCollider = base.transform.parent.GetComponent<Collider2D>();
// 		}
// 		if (this.hazardType == HazardType.STEAM)
// 		{
// 			FsmTemplate hornetMultiWounderFsmTemplate = Gameplay.HornetMultiWounderFsmTemplate;
// 			PlayMakerFSM playMakerFSM = base.gameObject.AddComponent<PlayMakerFSM>();
// 			playMakerFSM.Reset();
// 			playMakerFSM.SetFsmTemplate(hornetMultiWounderFsmTemplate);
// 			playMakerFSM.FsmVariables.FindFsmBool("z2 Steam Hazard").Value = true;
// 			this.damageDealt = 0;
// 		}
// 		return true;
// 	}

// 	// Token: 0x06001975 RID: 6517 RVA: 0x00074B25 File Offset: 0x00072D25
// 	public bool OnStart()
// 	{
// 		this.OnAwake();
// 		if (this.hasStarted)
// 		{
// 			return false;
// 		}
// 		this.hasStarted = true;
// 		return true;
// 	}

// 	// Token: 0x06001976 RID: 6518 RVA: 0x00074B40 File Offset: 0x00072D40
// 	private void Awake()
// 	{
// 		DamageHero._damageHeroes[base.gameObject] = this;
// 		this.OnAwake();
// 	}

// 	// Token: 0x06001977 RID: 6519 RVA: 0x00074B5A File Offset: 0x00072D5A
// 	private void OnDestroy()
// 	{
// 		if (this.healthManager)
// 		{
// 			this.healthManager.TookDamage -= this.OnDamaged;
// 		}
// 		DamageHero._damageHeroes.Remove(base.gameObject);
// 	}

// 	// Token: 0x06001978 RID: 6520 RVA: 0x00074B94 File Offset: 0x00072D94
// 	private void OnEnable()
// 	{
// 		if (this.resetOnEnable)
// 		{
// 			if (this.initialValue == null)
// 			{
// 				this.initialValue = new int?(this.damageDealt);
// 			}
// 			else
// 			{
// 				this.damageDealt = this.initialValue.Value;
// 			}
// 		}
// 		this.nailClashRoutine = null;
// 		this.preventClashTink = false;
// 		if (base.transform.parent && this.collider)
// 		{
// 			Rigidbody2D componentInParent = base.transform.parent.GetComponentInParent<Rigidbody2D>();
// 			if (componentInParent)
// 			{
// 				int attachedColliders = componentInParent.GetAttachedColliders(this.parentAttachedColliders);
// 				for (int i = 0; i < attachedColliders; i++)
// 				{
// 					Physics2D.IgnoreCollision(this.parentAttachedColliders[i], this.collider);
// 				}
// 			}
// 		}
// 		DebugDrawColliderRuntime.AddOrUpdate(base.gameObject, DebugDrawColliderRuntime.ColorType.Danger, false);
// 	}

// 	// Token: 0x06001979 RID: 6521 RVA: 0x00074C5C File Offset: 0x00072E5C
// 	private void OnDisable()
// 	{
// 		if (this.cancelAttack)
// 		{
// 			HeroController instance = HeroController.instance;
// 			if (instance)
// 			{
// 				instance.NailParryRecover();
// 			}
// 			this.cancelAttack = false;
// 		}
// 	}

// 	// Token: 0x0600197A RID: 6522 RVA: 0x00074C8C File Offset: 0x00072E8C
// 	public static bool TryGet(GameObject gameObject, out DamageHero damageHero)
// 	{
// 		return DamageHero._damageHeroes.TryGetValue(gameObject, out damageHero);
// 	}

// 	// Token: 0x0600197B RID: 6523 RVA: 0x00074C9A File Offset: 0x00072E9A
// 	private void OnTriggerEnter2D(Collider2D collision)
// 	{
// 		if (!base.enabled)
// 		{
// 			return;
// 		}
// 		this.TryClashTinkCollider(collision);
// 	}

// 	// Token: 0x0600197C RID: 6524 RVA: 0x00074CAC File Offset: 0x00072EAC
// 	private void OnCollisionEnter2D(Collision2D collision)
// 	{
// 		if (this.canClashTink && collision.gameObject.layer == 8 && !this.noTerrainThunk)
// 		{
// 			if (!this.noTerrainRecoil && this.recoil)
// 			{
// 				int attackDirection;
// 				int num;
// 				TerrainThunkUtils.GenerateTerrainThunk(collision, this.contactsTempStore, TerrainThunkUtils.SlashDirection.None, this.recoil.transform.position, out attackDirection, out num, new TerrainThunkUtils.TerrainThunkConditionDelegate(this.TerrainThunkCondition));
// 				if (num != 3 && num != 1)
// 				{
// 					this.recoil.RecoilByDirection(attackDirection, 0.5f);
// 				}
// 			}
// 			else
// 			{
// 				int attackDirection;
// 				int num;
// 				TerrainThunkUtils.GenerateTerrainThunk(collision, this.contactsTempStore, TerrainThunkUtils.SlashDirection.None, Vector2.zero, out attackDirection, out num, new TerrainThunkUtils.TerrainThunkConditionDelegate(this.TerrainThunkCondition));
// 			}
// 		}
// 		this.TryClashTinkCollider(collision.collider);
// 	}

// 	// Token: 0x0600197D RID: 6525 RVA: 0x00074D74 File Offset: 0x00072F74
// 	private bool TerrainThunkCondition(TerrainThunkUtils.TerrainThunkConditionArgs args)
// 	{
// 		if (args.RecoilDirection == 1)
// 		{
// 			return false;
// 		}
// 		if (!this.parentCollider)
// 		{
// 			return true;
// 		}
// 		Vector3 min = this.parentCollider.bounds.min;
// 		return args.ThunkPos.y >= min.y + 0.05f;
// 	}

// 	// Token: 0x0600197E RID: 6526 RVA: 0x00074DCC File Offset: 0x00072FCC
// 	private void TryClashTinkCollider(Collider2D collision)
// 	{
// 		if (this.hazardType == HazardType.SPIKES)
// 		{
// 			DamageEnemies component = collision.GetComponent<DamageEnemies>();
// 			if (component)
// 			{
// 				component.OnHitSpikes();
// 			}
// 		}
// 		if (!this.canClashTink)
// 		{
// 			return;
// 		}
// 		if (this.nailClashRoutine != null)
// 		{
// 			return;
// 		}
// 		if (this.preventClashTink)
// 		{
// 			return;
// 		}
// 		if (collision.gameObject.layer != 16)
// 		{
// 			return;
// 		}
// 		string tag = collision.gameObject.tag;
// 		Transform transform = collision.transform;
// 		Vector3 position = transform.position;
// 		Transform parent = transform.parent;
// 		if (!parent)
// 		{
// 			return;
// 		}
// 		DamageEnemies componentInChildren = parent.GetComponentInChildren<DamageEnemies>();
// 		if (!componentInChildren)
// 		{
// 			return;
// 		}
// 		if (this.healthManager != null)
// 		{
// 			if (componentInChildren.HasBeenDamaged(this.healthManager))
// 			{
// 				return;
// 			}
// 			componentInChildren.PreventDamage(this.healthManager);
// 			this.healthManager.CancelLagHitsForSource(componentInChildren.gameObject);
// 		}
// 		HeroController instance = HeroController.instance;
// 		if (componentInChildren.doesNotParry)
// 		{
// 			return;
// 		}
// 		if (tag == "Nail Attack" && !this.noClashFreeze && instance.parryInvulnTimer < Mathf.Epsilon)
// 		{
// 			GameManager.instance.FreezeMoment(FreezeMomentTypes.NailClashEffect, null);
// 		}
// 		componentInChildren.SendParried(!this.hasNonBouncer || !this.nonBouncer.active);
// 		if (this.healthManagerColliders != null)
// 		{
// 			foreach (Collider2D col in this.healthManagerColliders)
// 			{
// 				componentInChildren.PreventDamage(col);
// 			}
// 		}
// 		NailAttackBase component2 = componentInChildren.GetComponent<NailAttackBase>();
// 		if (component2 && !component2.CanHitSpikes)
// 		{
// 			CollisionSide damageSide;
// 			if (this.overrideCollisionSide)
// 			{
// 				damageSide = this.collisionSide;
// 			}
// 			else
// 			{
// 				CollisionSide collisionSide;
// 				switch (DirectionUtils.GetCardinalDirection(componentInChildren.direction))
// 				{
// 				case 0:
// 					collisionSide = CollisionSide.right;
// 					break;
// 				case 1:
// 					collisionSide = CollisionSide.top;
// 					break;
// 				case 2:
// 					collisionSide = CollisionSide.left;
// 					break;
// 				case 3:
// 					collisionSide = CollisionSide.bottom;
// 					break;
// 				default:
// 					collisionSide = CollisionSide.other;
// 					break;
// 				}
// 				damageSide = collisionSide;
// 			}
// 			instance.TakeDamage(base.gameObject, damageSide, 1, HazardType.ENEMY, this.damagePropertyFlags);
// 			return;
// 		}
// 		this.nailClashRoutine = base.StartCoroutine(this.NailClash(componentInChildren.direction, tag, position));
// 	}

// 	// Token: 0x0600197F RID: 6527 RVA: 0x00074FCD File Offset: 0x000731CD
// 	private IEnumerator NailClash(float direction, string colliderTag, Vector3 clasherPos)
// 	{
// 		HeroController hc = HeroController.instance;
// 		Effects.NailClashTinkShake.DoShake(this, false);
// 		Effects.NailClashParrySound.SpawnAndPlayOneShot(base.transform.position, null);
// 		if (colliderTag == "Nail Attack")
// 		{
// 			hc.NailParry();
// 			this.cancelAttack = true;
// 			if (direction < 45f)
// 			{
// 				if (this.noClashFreeze)
// 				{
// 					Effects.NailClashParryEffectSmall.Spawn(clasherPos + new Vector3(1.5f, 0f, 0f));
// 				}
// 				else
// 				{
// 					hc.RecoilLeft();
// 					Effects.NailClashParryEffect.Spawn(clasherPos + new Vector3(1.5f, 0f, 0f));
// 				}
// 				this.ClashEvents.OnClashRight.Invoke();
// 			}
// 			else if (direction < 135f)
// 			{
// 				if (this.noClashFreeze)
// 				{
// 					Effects.NailClashParryEffectSmall.Spawn(clasherPos + new Vector3(0f, 1.5f, 0f));
// 				}
// 				else
// 				{
// 					hc.RecoilDown();
// 					Effects.NailClashParryEffect.Spawn(clasherPos + new Vector3(0f, 1.5f, 0f));
// 				}
// 				this.ClashEvents.OnClashUp.Invoke();
// 			}
// 			else if (direction < 225f)
// 			{
// 				if (this.noClashFreeze)
// 				{
// 					Effects.NailClashParryEffectSmall.Spawn(clasherPos + new Vector3(-1.5f, 0f, 0f));
// 				}
// 				else
// 				{
// 					hc.RecoilRight();
// 					Effects.NailClashParryEffect.Spawn(clasherPos + new Vector3(-1.5f, 0f, 0f));
// 				}
// 				this.ClashEvents.OnClashLeft.Invoke();
// 			}
// 			else if (direction < 360f)
// 			{
// 				if (this.noClashFreeze)
// 				{
// 					Effects.NailClashParryEffectSmall.Spawn(clasherPos + new Vector3(-1.5f * hc.gameObject.transform.localScale.x, -1f, 0f));
// 				}
// 				else
// 				{
// 					hc.DownspikeBounce(false, null);
// 					Effects.NailClashParryEffect.Spawn(clasherPos + new Vector3(-1.5f * hc.gameObject.transform.localScale.x, -1f, 0f));
// 				}
// 				this.ClashEvents.OnClashDown.Invoke();
// 			}
// 		}
// 		else
// 		{
// 			this.cancelAttack = false;
// 			Effects.NailClashParryEffect.Spawn(clasherPos);
// 		}
// 		FSMUtility.SendEventToGameObject(base.gameObject, "PARRIED", false);
// 		if (base.transform.parent)
// 		{
// 			FSMUtility.SendEventToGameObject(base.transform.parent.gameObject, "PARRIED", false);
// 		}
// 		yield return new WaitForSeconds(0.1f);
// 		if (this.cancelAttack)
// 		{
// 			hc.NailParryRecover();
// 			this.cancelAttack = false;
// 		}
// 		yield return null;
// 		this.nailClashRoutine = null;
// 		yield break;
// 	}

// 	// Token: 0x06001980 RID: 6528 RVA: 0x00074FF1 File Offset: 0x000731F1
// 	private void OnDamaged()
// 	{
// 		this.preventClashTink = true;
// 	}

// 	// Token: 0x06001981 RID: 6529 RVA: 0x00074FFC File Offset: 0x000731FC
// 	public void SendHeroDamagedEvent()
// 	{
// 		if (this.HeroDamaged != null)
// 		{
// 			this.HeroDamaged();
// 		}
// 		if (this.HeroDamagedFSM != null)
// 		{
// 			if (!string.IsNullOrEmpty(this.HeroDamagedFSMEvent))
// 			{
// 				this.HeroDamagedFSM.SendEvent(this.HeroDamagedFSMEvent);
// 			}
// 			if (!string.IsNullOrEmpty(this.HeroDamagedFSMBool))
// 			{
// 				FsmBool fsmBool = this.HeroDamagedFSM.FsmVariables.BoolVariables.FirstOrDefault((FsmBool b) => b.Name == this.HeroDamagedFSMBool);
// 				if (fsmBool != null)
// 				{
// 					fsmBool.Value = true;
// 				}
// 			}
// 			if (!string.IsNullOrEmpty(this.HeroDamagedFSMGameObject))
// 			{
// 				FsmGameObject fsmGameObject = this.HeroDamagedFSM.FsmVariables.GameObjectVariables.FirstOrDefault((FsmGameObject b) => b.Name == this.HeroDamagedFSMGameObject);
// 				if (fsmGameObject != null)
// 				{
// 					fsmGameObject.Value = base.transform.gameObject;
// 				}
// 			}
// 		}
// 		this.OnDamagedHero.Invoke();
// 	}

// 	// Token: 0x06001982 RID: 6530 RVA: 0x000750D2 File Offset: 0x000732D2
// 	public void SetDamageAmount(int amount)
// 	{
// 		this.damageDealt = amount;
// 	}

// 	// Token: 0x06001983 RID: 6531 RVA: 0x000750DC File Offset: 0x000732DC
// 	public void SetCooldown(float cooldown)
// 	{
// 		if (cooldown <= 0f)
// 		{
// 			return;
// 		}
// 		double num = Time.timeAsDouble + (double)cooldown;
// 		if (num > this.damageAllowedTime)
// 		{
// 			this.damageAllowedTime = num;
// 		}
// 	}

// 	// Token: 0x06001984 RID: 6532 RVA: 0x0007510B File Offset: 0x0007330B
// 	public bool IsDamagerSpikes()
// 	{
// 		return this.hazardType == HazardType.SPIKES;
// 	}

// 	// Token: 0x06001985 RID: 6533 RVA: 0x00075119 File Offset: 0x00073319
// 	[ContextMenu("Test", true)]
// 	private bool CanTest()
// 	{
// 		return Application.isPlaying;
// 	}

// 	// Token: 0x06001986 RID: 6534 RVA: 0x00075120 File Offset: 0x00073320
// 	[ContextMenu("Test")]
// 	private void Test()
// 	{
// 		HeroController.instance.GetComponentInChildren<HeroBox>().TakeDamageFromDamager(this, base.gameObject);
// 	}

// 	// Token: 0x06001989 RID: 6537 RVA: 0x00075174 File Offset: 0x00073374
// 	GameObject IInitialisable.get_gameObject()
// 	{
// 		return base.gameObject;
// 	}

// 	// Token: 0x04001865 RID: 6245
// 	[ModifiableProperty]
// 	[Conditional("damageAsset", false, false, false)]
// 	public int damageDealt = 1;

// 	// Token: 0x04001866 RID: 6246
// 	public HazardType hazardType = HazardType.ENEMY;

// 	// Token: 0x04001867 RID: 6247
// 	[SerializeField]
// 	[QuickCreateAsset("Data Assets/Damages", "damageDealt", "value")]
// 	private DamageReference damageAsset;

// 	// Token: 0x04001868 RID: 6248
// 	[Space]
// 	[EnumPickerBitmask]
// 	public DamagePropertyFlags damagePropertyFlags;

// 	// Token: 0x04001869 RID: 6249
// 	[Space]
// 	public bool resetOnEnable;

// 	// Token: 0x0400186A RID: 6250
// 	private int? initialValue;

// 	// Token: 0x0400186B RID: 6251
// 	public bool canClashTink;

// 	// Token: 0x0400186C RID: 6252
// 	public bool forceParry;

// 	// Token: 0x0400186D RID: 6253
// 	[ModifiableProperty]
// 	[Conditional("canClashTink", true, false, false)]
// 	public bool noClashFreeze;

// 	// Token: 0x0400186E RID: 6254
// 	[ModifiableProperty]
// 	[Conditional("canClashTink", true, false, false)]
// 	public bool noTerrainThunk;

// 	// Token: 0x0400186F RID: 6255
// 	public bool noTerrainRecoil;

// 	// Token: 0x04001870 RID: 6256
// 	public bool noCorpseSpikeStick;

// 	// Token: 0x04001871 RID: 6257
// 	public bool noBounceCooldown;

// 	// Token: 0x04001872 RID: 6258
// 	[SerializeField]
// 	[Space]
// 	private bool overrideCollisionSide;

// 	// Token: 0x04001873 RID: 6259
// 	[ModifiableProperty]
// 	[Conditional("overrideCollisionSide", true, false, true)]
// 	[SerializeField]
// 	private CollisionSide collisionSide;

// 	// Token: 0x04001874 RID: 6260
// 	[SerializeField]
// 	private bool invertCollisionSide;

// 	// Token: 0x04001875 RID: 6261
// 	[Space]
// 	public PlayMakerFSM HeroDamagedFSM;

// 	// Token: 0x04001876 RID: 6262
// 	[ModifiableProperty]
// 	[Conditional("HeroDamagedFSM", true, false, true)]
// 	public bool AlwaysSendDamaged;

// 	// Token: 0x04001877 RID: 6263
// 	[ModifiableProperty]
// 	[Conditional("HeroDamagedFSM", true, false, true)]
// 	[InspectorValidation("IsFsmEventValid")]
// 	public string HeroDamagedFSMEvent;

// 	// Token: 0x04001878 RID: 6264
// 	[ModifiableProperty]
// 	[Conditional("HeroDamagedFSM", true, false, true)]
// 	[InspectorValidation("IsFsmBoolValid")]
// 	public string HeroDamagedFSMBool;

// 	// Token: 0x04001879 RID: 6265
// 	[ModifiableProperty]
// 	[Conditional("HeroDamagedFSM", true, false, true)]
// 	public string HeroDamagedFSMGameObject;

// 	// Token: 0x0400187A RID: 6266
// 	[Space]
// 	public DamageHero.ClashEventsWrapper ClashEvents;

// 	// Token: 0x0400187B RID: 6267
// 	public UnityEvent OnDamagedHero;

// 	// Token: 0x0400187C RID: 6268
// 	private bool preventClashTink;

// 	// Token: 0x0400187D RID: 6269
// 	private double damageAllowedTime;

// 	// Token: 0x0400187E RID: 6270
// 	private Coroutine nailClashRoutine;

// 	// Token: 0x0400187F RID: 6271
// 	private Collider2D collider;

// 	// Token: 0x04001880 RID: 6272
// 	private readonly ContactPoint2D[] contactsTempStore = new ContactPoint2D[10];

// 	// Token: 0x04001881 RID: 6273
// 	private readonly Collider2D[] parentAttachedColliders = new Collider2D[10];

// 	// Token: 0x04001882 RID: 6274
// 	private Collider2D parentCollider;

// 	// Token: 0x04001883 RID: 6275
// 	private HealthManager healthManager;

// 	// Token: 0x04001884 RID: 6276
// 	private Collider2D[] healthManagerColliders;

// 	// Token: 0x04001885 RID: 6277
// 	private Recoil recoil;

// 	// Token: 0x04001886 RID: 6278
// 	private static readonly Dictionary<GameObject, DamageHero> _damageHeroes = new Dictionary<GameObject, DamageHero>();

// 	// Token: 0x04001887 RID: 6279
// 	private bool hasAwaken;

// 	// Token: 0x04001888 RID: 6280
// 	private bool hasStarted;

// 	// Token: 0x04001889 RID: 6281
// 	private bool hasNonBouncer;

// 	// Token: 0x0400188A RID: 6282
// 	private NonBouncer nonBouncer;

// 	// Token: 0x0400188B RID: 6283
// 	private bool cancelAttack;

// 	// Token: 0x020015B5 RID: 5557
// 	[Serializable]
// 	public class ClashEventsWrapper
// 	{
// 		// Token: 0x0400885C RID: 34908
// 		public UnityEvent OnClashUp;

// 		// Token: 0x0400885D RID: 34909
// 		public UnityEvent OnClashDown;

// 		// Token: 0x0400885E RID: 34910
// 		public UnityEvent OnClashLeft;

// 		// Token: 0x0400885F RID: 34911
// 		public UnityEvent OnClashRight;
// 	}
// }
