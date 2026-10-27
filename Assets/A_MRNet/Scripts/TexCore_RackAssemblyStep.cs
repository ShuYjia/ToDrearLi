using UnityEngine;

[DisallowMultipleComponent]
public sealed class TexCore_RackAssemblyStep : TexCore_InteracterBase
{
    [SerializeField] private TexCore_RackAssemblyController controller;
    [SerializeField] private int rackStepIndex;

    public void Configure(TexCore_RackAssemblyController value, int index)
    {
        controller = value;
        rackStepIndex = index;
    }

    public override void UnFinished()
    {
        controller?.ResetBeforeStep(rackStepIndex);
    }

    public override void UnderWay()
    {
        controller?.ActivateStep(rackStepIndex, gameObject);
    }

    public override void OnTrigger()
    {
        controller?.FinishStep(rackStepIndex);
    }

    public override void Finish()
    {
        controller?.FinishStep(rackStepIndex);
    }
}
