using System.Collections.Generic;
using UnityEngine;

namespace Dig.Unity
{
    internal static class DigVisualCatalogDiagnostics
    {
        internal static void LogValidation(
            DigVisualCatalog? catalog,
            Object context,
            string catalogName)
        {
            if (catalog == null)
            {
                Debug.LogWarning(
                    $"{catalogName} visual catalog is not assigned; "
                    + "runtime fallback visuals remain active.",
                    context);
                return;
            }

            IReadOnlyList<string> errors = catalog.ValidateCatalog();
            for (int index = 0; index < errors.Count; index++)
            {
                Debug.LogError(
                    $"{catalogName} visual catalog: {errors[index]}",
                    context);
            }
        }
    }
}
