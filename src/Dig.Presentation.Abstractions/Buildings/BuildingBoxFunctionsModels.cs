using System;

namespace Dig.Presentation.Buildings
{
    public enum BuildingBoxFunctionActionKind
    {
        Unpack = 0,
    }

    public sealed class BuildingBoxFunctionActionViewModel
    {
        public BuildingBoxFunctionActionViewModel(
            BuildingBoxFunctionActionKind kind,
            string labelKey,
            bool isEnabled,
            bool isActive,
            string? disabledReasonCode)
        {
            if (string.IsNullOrWhiteSpace(labelKey))
            {
                throw new ArgumentException("Action label key is required.", nameof(labelKey));
            }

            if (!isEnabled && string.IsNullOrWhiteSpace(disabledReasonCode))
            {
                throw new ArgumentException(
                    "A disabled action requires a reason code.",
                    nameof(disabledReasonCode));
            }

            if (isActive && !isEnabled)
            {
                throw new ArgumentException("An active action must be enabled.", nameof(isActive));
            }

            Kind = kind;
            LabelKey = labelKey.Trim();
            IsEnabled = isEnabled;
            IsActive = isActive;
            DisabledReasonCode = disabledReasonCode;
        }

        public BuildingBoxFunctionActionKind Kind { get; }

        public string LabelKey { get; }

        public bool IsEnabled { get; }

        public bool IsActive { get; }

        public string? DisabledReasonCode { get; }
    }

    public sealed class BuildingBoxFunctionsViewModel
    {
        public BuildingBoxFunctionsViewModel(
            string stackId,
            string itemId,
            BuildingBoxFunctionActionViewModel unpackAction)
        {
            if (string.IsNullOrWhiteSpace(stackId) || string.IsNullOrWhiteSpace(itemId))
            {
                throw new ArgumentException("Stack and item ids are required.");
            }

            StackId = stackId.Trim();
            ItemId = itemId.Trim();
            UnpackAction = unpackAction
                ?? throw new ArgumentNullException(nameof(unpackAction));
        }

        public string StackId { get; }

        public string ItemId { get; }

        public BuildingBoxFunctionActionViewModel UnpackAction { get; }
    }
}
