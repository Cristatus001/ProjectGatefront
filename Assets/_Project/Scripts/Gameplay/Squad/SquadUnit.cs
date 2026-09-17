using UnityEngine;

namespace ProjectGatefront.Gameplay.Squad
{
    [DisallowMultipleComponent]
    public sealed class SquadUnit : MonoBehaviour
    {
        private Transform formationRoot;
        private Vector3 targetLocalPosition;
        private Vector3 smoothedWorldPosition;
        private float transitionSpeed;
        private float rotationSpeed;

        public int SlotIndex { get; private set; } = -1;

        public bool IsActive => gameObject.activeSelf;

        private void LateUpdate()
        {
            if (formationRoot == null)
            {
                return;
            }

            Vector3 targetWorldPosition =
                formationRoot.TransformPoint(targetLocalPosition);
            smoothedWorldPosition = Vector3.MoveTowards(
                smoothedWorldPosition,
                targetWorldPosition,
                transitionSpeed * Time.deltaTime);
            transform.position = smoothedWorldPosition;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                formationRoot.rotation,
                rotationSpeed * Time.deltaTime);
        }

        public void Activate(
            Transform root,
            Vector3 spawnLocalPosition,
            float movementSpeed,
            float facingSpeed)
        {
            formationRoot = root;
            transitionSpeed = movementSpeed;
            rotationSpeed = facingSpeed;
            smoothedWorldPosition = root.TransformPoint(spawnLocalPosition);
            transform.position = smoothedWorldPosition;
            transform.rotation = root.rotation;
            gameObject.SetActive(true);
        }

        public void SetFormationSlot(FormationSlot slot)
        {
            SlotIndex = slot.Index;
            targetLocalPosition = slot.LocalPosition;
        }

        public void Deactivate()
        {
            SlotIndex = -1;
            formationRoot = null;
            gameObject.SetActive(false);
        }
    }
}
