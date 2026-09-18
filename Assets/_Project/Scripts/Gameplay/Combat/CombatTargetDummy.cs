using System;
using ProjectGatefront.Gameplay.World;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScrollItem))]
    public sealed class CombatTargetDummy : MonoBehaviour, ITargetable, IDamageable
    {
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField, Min(0.05f)] private float targetRadius = 0.6f;
        [SerializeField, Min(0f)] private float aimHeight = 0.8f;
        [SerializeField] private float despawnBehindZ = -5f;

        private TargetRegistry registry;
        private WorldScroller worldScroller;
        private ScrollItem scrollItem;
        private Vector3 initialWorldPosition;
        private bool isInitialized;

        public event Action<CombatTargetDummy> Died;

        public Transform TargetTransform => transform;

        public Vector3 AimPosition => transform.position + (Vector3.up * aimHeight);

        public float TargetRadius => targetRadius;

        public bool IsTargetable => isInitialized
            && gameObject.activeInHierarchy
            && CurrentHealth > 0f;

        public float MaximumHealth => maximumHealth;

        public float CurrentHealth { get; private set; }

        public bool IsAlive => CurrentHealth > 0f;

        private void Update()
        {
            if (IsTargetable && transform.position.z <= despawnBehindZ)
            {
                DeactivateTarget();
            }
        }

        public void Configure(
            float health,
            float radius,
            float verticalAimOffset,
            float passThroughZ)
        {
            maximumHealth = Mathf.Max(1f, health);
            targetRadius = Mathf.Max(0.05f, radius);
            aimHeight = Mathf.Max(0f, verticalAimOffset);
            despawnBehindZ = passThroughZ;
        }

        public void Initialize(
            TargetRegistry targetRegistry,
            WorldScroller scroller)
        {
            registry = targetRegistry != null
                ? targetRegistry
                : throw new ArgumentNullException(nameof(targetRegistry));
            worldScroller = scroller != null
                ? scroller
                : throw new ArgumentNullException(nameof(scroller));
            scrollItem = GetComponent<ScrollItem>();
            initialWorldPosition = transform.position;
            CurrentHealth = maximumHealth;
            isInitialized = true;
            registry.Register(this);
            worldScroller.Register(scrollItem);
        }

        public DamageResult ApplyDamage(float damage)
        {
            if (!IsTargetable || !IsValidDamage(damage))
            {
                return new DamageResult(0f, Mathf.Max(0f, CurrentHealth), false);
            }

            float previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            float appliedDamage = previousHealth - CurrentHealth;
            bool wasKilled = CurrentHealth <= 0f;
            if (wasKilled)
            {
                Died?.Invoke(this);
                DeactivateTarget();
            }

            return new DamageResult(appliedDamage, CurrentHealth, wasKilled);
        }

        public void ResetTarget()
        {
            if (!isInitialized)
            {
                return;
            }

            transform.position = initialWorldPosition;
            CurrentHealth = maximumHealth;
            gameObject.SetActive(true);
            registry.Register(this);
            worldScroller.Register(scrollItem);
        }

        public void DeactivateTarget()
        {
            if (registry != null)
            {
                registry.Unregister(this);
            }

            if (worldScroller != null && scrollItem != null)
            {
                worldScroller.Unregister(scrollItem);
            }

            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (registry != null)
            {
                registry.Unregister(this);
            }

            if (worldScroller != null && scrollItem != null)
            {
                worldScroller.Unregister(scrollItem);
            }
        }

        private static bool IsValidDamage(float damage)
        {
            return !float.IsNaN(damage)
                && !float.IsInfinity(damage)
                && damage > 0f;
        }
    }
}
