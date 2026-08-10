using System;
using System.Linq;
using Dig.Presentation.Notifications;
using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigWorldInteraction
{
    internal void NavigateToNotification(GameNotificationNavigationTarget target)
    {
        if (target.Kind != GameNotificationNavigationKind.Technology)
        {
            _hud!.DismissTechnologyDescription();
        }

        switch (target.Kind)
        {
            case GameNotificationNavigationKind.Resident:
                NavigateToResidentNotification(target);
                break;
            case GameNotificationNavigationKind.Job:
                NavigateToJobNotification(target.EntityId!);
                break;
            case GameNotificationNavigationKind.Building:
                NavigateToBuildingNotification(target.EntityId!);
                break;
            case GameNotificationNavigationKind.Cell:
                FocusNotificationCell(target);
                break;
            case GameNotificationNavigationKind.Technology:
                _hud!.ShowTechnologyDescription(target.EntityId!);
                break;
            case GameNotificationNavigationKind.None:
                break;
            default:
                throw new InvalidOperationException("Unknown notification target.");
        }
    }

    private void NavigateToResidentNotification(
        GameNotificationNavigationTarget target)
    {
        string id = target.EntityId!;
        if (_agentRenderer!.GetHudModels().Any(resident => string.Equals(
            resident.Id,
            id,
            StringComparison.Ordinal)))
        {
            SelectResidentFromHud(id);
            if (_agentRenderer.TryGetWorldPosition(id, out Vector3 position))
            {
                _cameraController!.Focus(position);
            }

            return;
        }

        if (target.Cell.HasValue)
        {
            _cameraController!.Focus(DigSideViewProjection.CellCenter(
                target.Cell.Value.X,
                target.Cell.Value.Y));
        }
        else
        {
            _hud!.SetStatus("Notification source is no longer present.");
        }
    }

    private void NavigateToJobNotification(string jobId)
    {
        var job = _terrainSession!.LoadJobs().FirstOrDefault(value =>
            string.Equals(value.Id, jobId, StringComparison.Ordinal));
        if (job == null)
        {
            _hud!.SetStatus("Notification job is no longer present.");
            return;
        }

        SelectJobFromHud(jobId);
        if (job.HasTarget)
        {
            _cameraController!.Focus(DigSideViewProjection.CellCenter(
                job.TargetX!.Value,
                job.TargetY!.Value,
                -job.TargetZ!.Value));
        }
    }

    private void NavigateToBuildingNotification(string buildingId)
    {
        var building = _terrainSession!.LoadBuildings().FirstOrDefault(value =>
            string.Equals(value.Id, buildingId, StringComparison.Ordinal));
        if (building == null)
        {
            _hud!.SetStatus("Notification building is no longer present.");
            return;
        }

        SelectBuildingFromHud(buildingId);
        _cameraController!.Focus(DigSideViewProjection.CellCenter(
            building.OriginX,
            building.OriginY,
            DigSideViewProjection.BuildingDepth));
    }

    private void FocusNotificationCell(GameNotificationNavigationTarget target)
    {
        _cameraController!.Focus(DigSideViewProjection.CellCenter(
            target.Cell!.Value.X,
            target.Cell.Value.Y));
    }
}

}
