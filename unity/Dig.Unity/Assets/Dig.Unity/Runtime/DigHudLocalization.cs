using System;
using System.Collections.Generic;
using Dig.Presentation.Agents;
using Dig.Presentation.Notifications;

namespace Dig.Unity
{
internal static class DigHudLocalization
{
    private static readonly IReadOnlyDictionary<string, string> Russian =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["resident.need.health"] = "Здоровье",
            ["resident.need.nutrition"] = "Сытость",
            ["resident.need.alertness.vigor"] = "Бодрость",
            ["resident.need.mood"] = "Настроение",
            ["resident.activity.freetime"] = "Свободное время",
            ["resident.activity.move"] = "Перемещается",
            ["resident.activity.attack"] = "Атакует",
            ["resident.activity.cook"] = "Готовит",
            ["resident.activity.unpackbuilding"] = "Собирает здание",
            ["resident.activity.packbuilding"] = "Упаковывает здание",
            ["resident.activity.dig"] = "Копает",
            ["resident.activity.craft"] = "Производит",
            ["resident.activity.pickup"] = "Подбирает предмет",
            ["resident.activity.service"] = "Обслуживает",
            ["resident.activity.train"] = "Тренируется",
            ["resident.activity.study"] = "Учится",
            ["resident.activity.logistics"] = "Переносит ресурсы",
            ["resident.activity.eat"] = "Ест",
            ["resident.activity.sleep"] = "Спит",
            ["resident.activity.rest"] = "Отдыхает",
            ["resident.activity.flee"] = "Убегает",
            ["resident.activity.idle"] = "Бездействует",
            ["resident.activity.work"] = "Работает",
            ["resident.activity.blocked"] = "Задание заблокировано",
            ["reason.path_missing"] = "нет маршрута",
            ["notification.resident.attacked"] = "⚔ {source}: нападение",
            ["notification.resident.born"] = "★ Родился житель: {source}",
            ["notification.resident.hungry"] = "! {source}: голод",
            ["notification.resident.old"] = "◷ {source}: пожилой возраст",
            ["notification.resident.mood_critical"] =
                "! {source}: критическое настроение",
            ["notification.resident.died"] = "† {source}: смерть",
            ["notification.technology.unlocked"] =
                "◆ Открыта технология: {source}",
            ["notification.job.completed"] = "✓ Задание завершено: {source}",
        };

    internal static string Resolve(string key)
    {
        return Russian.TryGetValue(key, out string? value) ? value : key;
    }

    internal static string FormatActivity(ResidentActivityDescriptor activity)
    {
        string progress = activity.ProgressMaximum > 0
            ? $" {activity.ProgressCurrent}/{activity.ProgressMaximum}"
            : string.Empty;
        string blocked = string.IsNullOrWhiteSpace(activity.BlockReasonCode)
            ? string.Empty
            : " · " + Resolve("reason." + activity.BlockReasonCode);
        return "Статус: " + Resolve(activity.LocalizationKey) + progress + blocked;
    }

    internal static string FormatNotification(
        GameNotification notification,
        string source)
    {
        Dictionary<string, string> arguments = new Dictionary<string, string>(
            StringComparer.Ordinal);
        for (int index = 0; index < notification.LocalizationArguments.Count; index++)
        {
            GameNotificationArgument argument = notification.LocalizationArguments[index];
            arguments[argument.Key] = argument.Value;
        }

        arguments["source"] = source;
        string value = Resolve(notification.LocalizationKey);
        foreach (KeyValuePair<string, string> argument in arguments)
        {
            value = value.Replace("{" + argument.Key + "}", argument.Value);
        }

        return value;
    }
}
}
