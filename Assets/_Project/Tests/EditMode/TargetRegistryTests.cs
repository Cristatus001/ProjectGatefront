using NUnit.Framework;
using System.Collections.Generic;
using ProjectGatefront.Gameplay.Combat;
using ProjectGatefront.Gameplay.Gates;
using UnityEngine;

namespace ProjectGatefront.Tests
{
    public sealed class RegistryTestTarget : MonoBehaviour, ITargetable, IDamageable
    {
        public bool Targetable = true;
        public float Radius = 0.5f;

        public Transform TargetTransform => transform;

        public Vector3 AimPosition => transform.position;

        public float TargetRadius => Radius;

        public bool IsTargetable => Targetable;

        public DamageResult ApplyDamage(float damage)
        {
            return new DamageResult(damage, 0f, true);
        }
    }

    public sealed class RegistryTestGate : MonoBehaviour,
        IShootableGate,
        IProjectileHitVolume
    {
        public Transform TargetTransform => transform;
        public Vector3 AimPosition => transform.position;
        public float TargetRadius => 2f;
        public bool IsTargetable => true;

        public bool CanReceiveFire(float squadX, float squadContactZ)
        {
            return true;
        }

        public bool ApplyProjectileHit()
        {
            return true;
        }

        public bool TryGetProjectileIntersection(
            Vector3 segmentStart,
            Vector3 segmentEnd,
            float projectileRadius,
            out float normalizedDistance)
        {
            float padding = Mathf.Max(0f, projectileRadius);
            return TargetingMath.SegmentIntersectsAabb(
                segmentStart,
                segmentEnd,
                AimPosition,
                new Vector3(2f + padding, 2f + padding, 0.05f + padding),
                out normalizedDistance);
        }
    }

    public sealed class TargetRegistryTests
    {
        private GameObject registryObject;
        private TargetRegistry registry;

        [SetUp]
        public void SetUp()
        {
            registryObject = new GameObject("TargetRegistryTest");
            registry = registryObject.AddComponent<TargetRegistry>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(registryObject);
            foreach (RegistryTestTarget target in
                Object.FindObjectsByType<RegistryTestTarget>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(target.gameObject);
            }
            foreach (RegistryTestGate gate in
                Object.FindObjectsByType<RegistryTestGate>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(gate.gameObject);
            }
        }

        [Test]
        public void ClosestForwardTarget_RejectsBehindDeadAndOutOfRangeTargets()
        {
            RegistryTestTarget behind = CreateTarget("Behind", new Vector3(0f, 0f, -2f));
            RegistryTestTarget dead = CreateTarget("Dead", new Vector3(0f, 0f, 2f));
            RegistryTestTarget valid = CreateTarget("Valid", new Vector3(0f, 0f, 6f));
            RegistryTestTarget far = CreateTarget("Far", new Vector3(0f, 0f, 20f));
            dead.Targetable = false;
            registry.Register(behind);
            registry.Register(dead);
            registry.Register(valid);
            registry.Register(far);

            ITargetable selected = registry.FindClosestForwardTarget(
                Vector3.zero,
                Vector3.forward,
                10f,
                0f);

            Assert.That(selected, Is.SameAs(valid));
        }

        [Test]
        public void RegisterAndUnregister_AreDuplicateSafe()
        {
            RegistryTestTarget target = CreateTarget("Target", Vector3.forward);
            Assert.That(registry.Register(target), Is.True);
            Assert.That(registry.Register(target), Is.False);
            Assert.That(registry.ActiveTargetCount, Is.EqualTo(1));
            Assert.That(registry.Unregister(target), Is.True);
            Assert.That(registry.Unregister(target), Is.False);
            Assert.That(registry.ActiveTargetCount, Is.Zero);
        }

        [Test]
        public void SegmentHit_SelectsFirstTargetWithoutTunneling()
        {
            RegistryTestTarget near = CreateTarget("Near", new Vector3(0f, 0f, 4f));
            RegistryTestTarget far = CreateTarget("Far", new Vector3(0f, 0f, 8f));
            registry.Register(far);
            registry.Register(near);

            bool hit = registry.TryFindFirstSegmentHit(
                Vector3.zero,
                Vector3.forward * 10f,
                0.05f,
                null,
                out ITargetable target);

            Assert.That(hit, Is.True);
            Assert.That(target, Is.SameAs(near));
        }

        [Test]
        public void SegmentHit_EnemyBeforeGate_SelectsEnemy()
        {
            RegistryTestTarget enemy = CreateTarget(
                "Enemy", new Vector3(0f, 0f, 5f));
            RegistryTestGate gate = CreateGate(
                "Gate", new Vector3(0f, 0f, 10f));
            registry.Register(gate);
            registry.Register(enemy);

            Assert.That(TryHit(out ITargetable hit), Is.True);
            Assert.That(hit, Is.SameAs(enemy));
        }

        [Test]
        public void SegmentHit_GateBeforeEnemy_SelectsGate()
        {
            RegistryTestGate gate = CreateGate(
                "Gate", new Vector3(0f, 0f, 5f));
            RegistryTestTarget enemy = CreateTarget(
                "Enemy", new Vector3(0f, 0f, 10f));
            registry.Register(enemy);
            registry.Register(gate);

            Assert.That(TryHit(out ITargetable hit), Is.True);
            Assert.That(hit, Is.SameAs(gate));
        }

        [Test]
        public void SegmentHit_EnemyOutsideCorridor_SelectsGate()
        {
            RegistryTestTarget enemy = CreateTarget(
                "Enemy", new Vector3(2f, 0f, 5f));
            RegistryTestGate gate = CreateGate(
                "Gate", new Vector3(0f, 0f, 10f));
            registry.Register(gate);
            registry.Register(enemy);

            Assert.That(TryHit(out ITargetable hit), Is.True);
            Assert.That(hit, Is.SameAs(gate));
        }

        [Test]
        public void SegmentHit_ApproximatelyEqualSurface_PrefersEnemy()
        {
            RegistryTestTarget enemy = CreateTarget(
                "Enemy", new Vector3(0f, 0f, 5f));
            RegistryTestGate gate = CreateGate(
                "Gate", new Vector3(0f, 0f, 4.545f));
            registry.Register(enemy);
            registry.Register(gate);

            Assert.That(TryHit(out ITargetable hit), Is.True);
            Assert.That(hit, Is.SameAs(enemy));
        }

        [Test]
        public void CandidateCollection_ReturnsNearestTargetsInOrder()
        {
            RegistryTestTarget far = CreateTarget("Far", new Vector3(0f, 0f, 9f));
            RegistryTestTarget near = CreateTarget("Near", new Vector3(0f, 0f, 3f));
            RegistryTestTarget middle = CreateTarget("Middle", new Vector3(0f, 0f, 6f));
            registry.Register(far);
            registry.Register(near);
            registry.Register(middle);
            var results = new List<ITargetable>();

            int count = registry.CollectClosestForwardTargets(
                Vector3.zero,
                Vector3.forward,
                10f,
                0f,
                2,
                results);

            Assert.That(count, Is.EqualTo(2));
            Assert.That(results[0], Is.SameAs(near));
            Assert.That(results[1], Is.SameAs(middle));
        }

        private static RegistryTestTarget CreateTarget(
            string name,
            Vector3 position)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.position = position;
            return gameObject.AddComponent<RegistryTestTarget>();
        }

        private static RegistryTestGate CreateGate(
            string name,
            Vector3 position)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.position = position;
            return gameObject.AddComponent<RegistryTestGate>();
        }

        private bool TryHit(out ITargetable hit)
        {
            return registry.TryFindFirstSegmentHit(
                Vector3.zero,
                Vector3.forward * 15f,
                0f,
                null,
                out hit);
        }
    }
}
