using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies deterministic scene-object visibility for a TexCore step. It is
/// intentionally generic so 08/09 can reuse the existing TexCore step tree
/// without importing old legacy task scripts.
/// </summary>
[DisallowMultipleComponent]
public sealed class TexCore_StepObjectState : TexCore_InteracterBase
{
    [SerializeField] private List<GameObject> showWhileActive = new List<GameObject>();
    [SerializeField] private List<GameObject> hideWhileActive = new List<GameObject>();
    [SerializeField] private List<GameObject> showWhenFinished = new List<GameObject>();
    [SerializeField] private List<GameObject> hideWhenFinished = new List<GameObject>();
    [SerializeField] private List<GameObject> showWhenUnfinished = new List<GameObject>();
    [SerializeField] private List<GameObject> hideWhenUnfinished = new List<GameObject>();

    public void Configure(
        IEnumerable<GameObject> activeShows,
        IEnumerable<GameObject> activeHides,
        IEnumerable<GameObject> finishedShows,
        IEnumerable<GameObject> finishedHides,
        IEnumerable<GameObject> unfinishedShows,
        IEnumerable<GameObject> unfinishedHides)
    {
        showWhileActive = Copy(activeShows);
        hideWhileActive = Copy(activeHides);
        showWhenFinished = Copy(finishedShows);
        hideWhenFinished = Copy(finishedHides);
        showWhenUnfinished = Copy(unfinishedShows);
        hideWhenUnfinished = Copy(unfinishedHides);
    }

    public override void UnFinished()
    {
        Apply(showWhenUnfinished, true);
        Apply(hideWhenUnfinished, false);
    }

    public override void UnderWay()
    {
        Apply(showWhileActive, true);
        Apply(hideWhileActive, false);
    }

    public override void OnTrigger()
    {
        Apply(showWhenFinished, true);
        Apply(hideWhenFinished, false);
    }

    public override void Finish()
    {
        Apply(showWhenFinished, true);
        Apply(hideWhenFinished, false);
    }

    private static List<GameObject> Copy(IEnumerable<GameObject> values)
    {
        var result = new List<GameObject>();
        if (values == null)
        {
            return result;
        }

        foreach (GameObject value in values)
        {
            if (value != null && !result.Contains(value))
            {
                result.Add(value);
            }
        }

        return result;
    }

    private static void Apply(IEnumerable<GameObject> values, bool active)
    {
        if (values == null)
        {
            return;
        }

        foreach (GameObject value in values)
        {
            if (value != null)
            {
                value.SetActive(active);
            }
        }
    }
}
