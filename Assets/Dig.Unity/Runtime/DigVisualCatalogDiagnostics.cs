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
