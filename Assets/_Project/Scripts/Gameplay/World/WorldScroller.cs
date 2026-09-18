using System;
using System.Collections.Generic;
using ProjectGatefront.Data;
using UnityEngine;

namespace ProjectGatefront.Gameplay.World
{
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class WorldScroller : MonoBehaviour
    {
        [SerializeField] private GameBalanceConfig config;
        [SerializeField] private List<ScrollItem> scrollItems = new List<ScrollItem>();
        [SerializeField] private float recycleBehindZ = -28f;
        [SerializeField] private bool scrollingEnabledOnStart = true;

        public bool IsInitialized { get; private set; }

        public bool ScrollingEnabled { get; private set; }

        public float CurrentScrollSpeed { get; private set; }

        public float DistanceTravelled { get; private set; }

        public int RecycleCount { get; private set; }

        public int RegisteredItemCount => scrollItems.Count;

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Configure(
            GameBalanceConfig balanceConfig,
            IList<ScrollItem> items,
            float recycleThresholdZ)
        {
            config = balanceConfig;
            recycleBehindZ = recycleThresholdZ;
            scrollItems.Clear();

            if (items == null)
            {
                return;
            }

            for (int index = 0; index < items.Count; index++)
            {
                ScrollItem item = items[index];
                if (item != null && !scrollItems.Contains(item))
                {
                    scrollItems.Add(item);
                }
            }
        }

        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            if (config == null)
            {
                throw new InvalidOperationException(
                    "WorldScroller requires a GameBalanceConfig.");
            }

            for (int index = 0; index < scrollItems.Count; index++)
            {
                if (scrollItems[index] != null)
                {
                    scrollItems[index].CaptureInitialState();
                }
            }

            CurrentScrollSpeed = WorldScrollMath.SanitizeSpeed(
                config.WorldScrollSpeed);
            DistanceTravelled = 0f;
            RecycleCount = 0;
            ScrollingEnabled = scrollingEnabledOnStart;
            IsInitialized = true;
        }

        public void Tick(float deltaTime)
        {
            if (!IsInitialized)
            {
                return;
            }

            float distanceDelta = WorldScrollMath.CalculateDistanceDelta(
                CurrentScrollSpeed,
                deltaTime,
                ScrollingEnabled);
            if (distanceDelta <= 0f)
            {
                return;
            }

            DistanceTravelled += distanceDelta;
            for (int index = 0; index < scrollItems.Count; index++)
            {
                ScrollItem item = scrollItems[index];
                if (item != null)
                {
                    item.AdvanceTowardsPlayer(distanceDelta);
                }
            }

            RecyclePassedItems();
        }

        public void SetScrollingEnabled(bool enabled)
        {
            ScrollingEnabled = enabled;
        }

        public void SetScrollSpeed(float speed)
        {
            CurrentScrollSpeed = WorldScrollMath.SanitizeSpeed(speed);
        }

        public void RestoreConfiguredScrollSpeed()
        {
            if (config != null)
            {
                CurrentScrollSpeed = WorldScrollMath.SanitizeSpeed(
                    config.WorldScrollSpeed);
            }
        }

        public void ResetScroll()
        {
            for (int index = 0; index < scrollItems.Count; index++)
            {
                if (scrollItems[index] != null)
                {
                    scrollItems[index].ResetToInitialState();
                }
            }

            DistanceTravelled = 0f;
            RecycleCount = 0;
            RestoreConfiguredScrollSpeed();
            ScrollingEnabled = scrollingEnabledOnStart;
        }

        public bool Register(ScrollItem item)
        {
            if (item == null || scrollItems.Contains(item))
            {
                return false;
            }

            scrollItems.Add(item);
            if (IsInitialized)
            {
                item.CaptureInitialState();
            }

            return true;
        }

        public bool Unregister(ScrollItem item)
        {
            return item != null && scrollItems.Remove(item);
        }

        private void RecyclePassedItems()
        {
            int safetyCounter = 0;
            const int maximumRecycleOperationsPerTick = 10000;

            while (safetyCounter < maximumRecycleOperationsPerTick)
            {
                ScrollItem itemToRecycle = null;
                float furthestFrontZ = float.NegativeInfinity;
                float furthestBehindZ = float.PositiveInfinity;

                for (int index = 0; index < scrollItems.Count; index++)
                {
                    ScrollItem item = scrollItems[index];
                    if (item == null || !item.IsRecyclable)
                    {
                        continue;
                    }

                    furthestFrontZ = Mathf.Max(furthestFrontZ, item.WorldZ);
                    if (item.ShouldRecycle(recycleBehindZ)
                        && item.WorldZ < furthestBehindZ)
                    {
                        furthestBehindZ = item.WorldZ;
                        itemToRecycle = item;
                    }
                }

                if (itemToRecycle == null)
                {
                    return;
                }

                float targetZ = WorldScrollMath.CalculateRecycleTargetZ(
                    furthestFrontZ,
                    itemToRecycle.RecycleLength);
                itemToRecycle.RecycleTo(targetZ);
                RecycleCount++;
                safetyCounter++;
            }

            Debug.LogError(
                "WorldScroller exceeded its recycle safety limit in one tick.",
                this);
        }
    }
}
