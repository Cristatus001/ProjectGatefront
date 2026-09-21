using NUnit.Framework;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay.Combat;
using ProjectGatefront.Gameplay.Gates;
using ProjectGatefront.Gameplay.World;
using UnityEngine;

namespace ProjectGatefront.Tests.EditMode
{
    public sealed class GateControllerTests
    {
        [Test]
        public void ProjectileHits_AddExactlyOne_ThenLockAndReset()
        {
            var root = new GameObject("GateTest");
            var gateObject = new GameObject("Gate");
            var balance = ScriptableObject.CreateInstance<GameBalanceConfig>();
            try
            {
                WorldScroller world = root.AddComponent<WorldScroller>();
                world.Configure(balance, null, -28f);
                world.Initialize();
                TargetRegistry registry = root.AddComponent<TargetRegistry>();
                gateObject.AddComponent<ScrollItem>();
                gateObject.AddComponent<WorldContentCarrier>();
                GateController gate = gateObject.AddComponent<GateController>();
                var definition = new GateEncounterDefinition(60f,
                    GateEncounterLayout.SingleLarge, -50, -50, 50,
                    2, 0.5f, 18f, 8f, 34f, 26f);
                gate.Activate(definition, GateKind.ShootableAdditive,
                    world, registry, new Vector3(0f, 0f, 12f), 8f, -1f);
                Assert.That(gate.CanReceiveFire(0f, 0f), Is.True);
                Assert.That(gate.CanReceiveFire(5f, 0f), Is.False);
                Assert.That(gate.CanReceiveFire(0f, -20f), Is.False);
                int notifications = 0;
                gate.GateValueChanged += (_, __) => notifications++;
                for (int index = 0; index < 100; index++)
                {
                    Assert.That(gate.ApplyProjectileHit(), Is.True);
                }

                Assert.That(gate.CurrentValue, Is.EqualTo(50));
                Assert.That(gate.ApplyProjectileHit(), Is.False);
                Assert.That(gate.CanReceiveFire(0f, 0f), Is.False);
                Assert.That(notifications, Is.EqualTo(100));
                Assert.That(gate.LockValue(), Is.EqualTo(50));
                Assert.That(gate.ApplyProjectileHit(), Is.False);
                Assert.That(registry.ActiveTargetCount, Is.Zero);
                gate.Activate(definition, GateKind.ShootableAdditive,
                    world, registry, new Vector3(0f, 0f, 12f), 8f, -1f);
                Assert.That(gate.CurrentValue, Is.EqualTo(-50));
                Assert.That(gate.IsLocked, Is.False);
                Assert.That(registry.ActiveTargetCount, Is.EqualTo(1));

                gate.Activate(definition, GateKind.Divide,
                    world, registry, new Vector3(2f, 0f, 12f), 4f, -1f);
                Assert.That(gate.IsTargetable, Is.False);
                Assert.That(gate.ApplyProjectileHit(), Is.False);
                Assert.That(registry.ActiveTargetCount, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(gateObject);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(balance);
            }
        }
    }
}
