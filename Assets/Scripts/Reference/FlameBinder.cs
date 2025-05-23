using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

[VFXBinder("Transform/Distance")]
public class FlameBinder : VFXBinderBase
{

    [VFXPropertyBinding("System.Single")]
    public ExposedProperty FlameScale;
    public ExposedProperty SmokeSize;
    public ExposedProperty SparksSize;

    public Vector3 FlameScaleValue;
    public float SmokeSizeValue;
    public float SparksSizeValue;

    public Transform target;

    public override bool IsValid(VisualEffect component) => target != null && component.HasVector3(FlameScale) && component.HasFloat(SmokeSize) && component.HasFloat(SparksSize);

    public override void UpdateBinding(VisualEffect component)
    {
        component.SetVector3(FlameScale, FlameScaleValue);
        component.SetFloat(SmokeSize, SmokeSizeValue);
        component.SetFloat(SparksSize, SparksSizeValue);
    }
}
