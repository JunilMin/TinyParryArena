using System;
using UnityEngine;

public sealed class ObsoleteFearGaugeCleaner : MonoBehaviour
{
    private static readonly string[] ObsoleteObjectNames =
    {
        "__TinyParryArena_FearGauge",
        "__TinyParryArena_HUD",
        "FearGauge",
        "Fear Gauge",
        "FearGaugeRoot",
        "FearGaugeCanvas"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        GameObject cleaner = new GameObject("__FearGaugeCleanup");
        DontDestroyOnLoad(cleaner);
        cleaner.hideFlags = HideFlags.HideInHierarchy;
        cleaner.AddComponent<ObsoleteFearGaugeCleaner>();
    }

    private void Update()
    {
        RemoveByName();
        RemoveOldFearGaugeComponents();
    }

    private static void RemoveByName()
    {
        foreach (string objectName in ObsoleteObjectNames)
        {
            GameObject found = GameObject.Find(objectName);
            if (found != null)
                Destroy(found);
        }
    }

    private static void RemoveOldFearGaugeComponents()
    {
#if UNITY_2023_1_OR_NEWER
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
#else
        MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>(true);
#endif

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
                continue;

            Type type = behaviour.GetType();
            string typeName = type.Name;

            if (typeName == "FearGaugeUI" ||
                typeName == "FearGaugeRuntimeBootstrap" ||
                typeName == "FearGaugeCreator")
            {
                GameObject target = behaviour.gameObject;
                if (target != null)
                    Destroy(target);
            }
        }
    }
}
