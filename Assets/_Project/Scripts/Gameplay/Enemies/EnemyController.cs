using System;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay.Combat;
using ProjectGatefront.Gameplay.Squad;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyMotor))]
    public sealed class EnemyController : MonoBehaviour, ITargetable, IDamageable
    {
        private const float HitFeedbackDuration = 0.07f;
        private const float DeathFeedbackDuration = 0.14f;

        [SerializeField] private Transform visualRoot;
        [SerializeField, Min(0f)] private float aimHeight = 1f;

        private EnemyPool owner;
        private TargetRegistry registry;
        private EnemyMotor motor;
        private EnemyConfig config;
        private Vector3 authoredVisualScale = Vector3.one;
        private Vector3 activeVisualScale = Vector3.one;
        private float hitFeedbackRemaining;
        private float deathFeedbackRemaining;
        private bool contactEvaluated;

        public Transform TargetTransform => transform;

        public Vector3 AimPosition => transform.position
            + (Vector3.up * aimHeight * Mathf.Max(0.1f, ConfigVisualScale));

        public float TargetRadius => config == null ? 0f : config.TargetRadius;

        public bool IsTargetable => State == EnemyRuntimeState.Approaching
            && gameObject.activeInHierarchy
            && CurrentHealth > 0f;

        public EnemyRuntimeState State { get; private set; }

        public float CurrentHealth { get; private set; }

        public float MaximumHealth => config == null ? 0f : config.MaximumHealth;

        public EnemyConfig Config => config;

        public int WaveIndex { get; private set; } = -1;

        public int SpawnSequence { get; private set; }

        public int PoolIndex { get; set; } = -1;

        private float ConfigVisualScale => config == null
            ? 1f
            : config.VisualScale;

        private void Awake()
        {
            motor = GetComponent<EnemyMotor>();
            if (visualRoot != null)
            {
                authoredVisualScale = visualRoot.localScale;
            }
        }

        public void Configure(Transform visuals, float verticalAimOffset)
        {
            visualRoot = visuals;
            aimHeight = Mathf.Max(0f, verticalAimOffset);
            if (visualRoot != null)
            {
                authoredVisualScale = visualRoot.localScale;
            }
        }

        public void Activate(
            EnemyPool enemyPool,
            TargetRegistry targetRegistry,
            EnemyConfig enemyConfig,
            Vector3 worldPosition,
            int waveIndex,
            int spawnSequence)
        {
            owner = enemyPool != null
                ? enemyPool
                : throw new ArgumentNullException(nameof(enemyPool));
            registry = targetRegistry != null
                ? targetRegistry
                : throw new ArgumentNullException(nameof(targetRegistry));
            config = enemyConfig != null
                ? enemyConfig
                : throw new ArgumentNullException(nameof(enemyConfig));
            motor = motor != null ? motor : GetComponent<EnemyMotor>();
            motor.Configure(config.ApproachSpeed);
            WaveIndex = waveIndex;
            SpawnSequence = spawnSequence;
            CurrentHealth = config.MaximumHealth;
            State = EnemyRuntimeState.Approaching;
            contactEvaluated = false;
            hitFeedbackRemaining = 0f;
            deathFeedbackRemaining = 0f;
            transform.SetPositionAndRotation(worldPosition, Quaternion.identity);
            activeVisualScale = authoredVisualScale * config.VisualScale;
            if (visualRoot != null)
            {
                visualRoot.localScale = activeVisualScale;
                visualRoot.localRotation = Quaternion.identity;
            }

            gameObject.SetActive(true);
            registry.Register(this);
        }

        public DamageResult ApplyDamage(float damage)
        {
            if (!IsTargetable
                || float.IsNaN(damage)
                || float.IsInfinity(damage)
                || damage <= 0f)
            {
                return new DamageResult(0f, Mathf.Max(0f, CurrentHealth), false);
            }

            float previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            float appliedDamage = previousHealth - CurrentHealth;
            bool killed = CurrentHealth <= 0f;
            if (killed)
            {
                BeginDeath();
            }
            else
            {
                hitFeedbackRemaining = HitFeedbackDuration;
            }

            return new DamageResult(appliedDamage, CurrentHealth, killed);
        }

        public void Tick(
            float deltaTime,
            float worldScrollSpeed,
            bool worldMovementEnabled,
            SquadController squad,
            float contactPadding,
            float passBehindDistance)
        {
            if (State == EnemyRuntimeState.Inactive || deltaTime <= 0f)
            {
                return;
            }

            if (State == EnemyRuntimeState.Dying)
            {
                TickDeathFeedback(deltaTime);
                return;
            }

            motor.Advance(worldScrollSpeed, deltaTime, worldMovementEnabled);
            TickHitFeedback(deltaTime);

            if (State == EnemyRuntimeState.Approaching
                && !contactEvaluated
                && EnemyContactMath.HasReachedContactLine(
                    transform.position.z,
                    TargetRadius,
                    squad.CombatContactZ))
            {
                contactEvaluated = true;
                if (squad.CurrentUnitCount > 0
                    && EnemyContactMath.OverlapsSquadHorizontally(
                        transform.position.x,
                        TargetRadius,
                        squad.CurrentWorldMinimumX,
                        squad.CurrentWorldMaximumX,
                        contactPadding))
                {
                    registry.Unregister(this);
                    owner.HandleEnemyContact(this);
                    return;
                }

                State = EnemyRuntimeState.Passed;
                registry.Unregister(this);
            }

            if (State == EnemyRuntimeState.Passed
                && transform.position.z + TargetRadius
                    <= squad.CombatContactZ - Mathf.Max(0f, passBehindDistance))
            {
                owner.Release(this, EnemyReleaseReason.MissedSquad);
            }
        }

        public void ReturnToPoolState()
        {
            if (registry != null)
            {
                registry.Unregister(this);
            }

            State = EnemyRuntimeState.Inactive;
            CurrentHealth = 0f;
            WaveIndex = -1;
            SpawnSequence = 0;
            PoolIndex = -1;
            contactEvaluated = false;
            hitFeedbackRemaining = 0f;
            deathFeedbackRemaining = 0f;
            if (visualRoot != null)
            {
                visualRoot.localScale = authoredVisualScale;
                visualRoot.localRotation = Quaternion.identity;
            }

            config = null;
            registry = null;
            owner = null;
            gameObject.SetActive(false);
        }

        private void BeginDeath()
        {
            if (State != EnemyRuntimeState.Approaching)
            {
                return;
            }

            State = EnemyRuntimeState.Dying;
            registry.Unregister(this);
            deathFeedbackRemaining = DeathFeedbackDuration;
            owner.NotifyEnemyKilled(this);
        }

        private void TickHitFeedback(float deltaTime)
        {
            if (visualRoot == null)
            {
                return;
            }

            if (hitFeedbackRemaining > 0f)
            {
                hitFeedbackRemaining = Mathf.Max(
                    0f,
                    hitFeedbackRemaining - deltaTime);
                float normalized = hitFeedbackRemaining / HitFeedbackDuration;
                visualRoot.localScale = activeVisualScale
                    * (1f + (normalized * 0.12f));
            }
            else if (visualRoot.localScale != activeVisualScale)
            {
                visualRoot.localScale = activeVisualScale;
            }
        }

        private void TickDeathFeedback(float deltaTime)
        {
            deathFeedbackRemaining = Mathf.Max(
                0f,
                deathFeedbackRemaining - deltaTime);
            if (visualRoot != null)
            {
                float normalized = deathFeedbackRemaining
                    / DeathFeedbackDuration;
                visualRoot.localScale = activeVisualScale * normalized;
                visualRoot.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    (1f - normalized) * 24f);
            }

            if (deathFeedbackRemaining <= 0f)
            {
                owner.Release(this, EnemyReleaseReason.Killed);
            }
        }
    }
}
