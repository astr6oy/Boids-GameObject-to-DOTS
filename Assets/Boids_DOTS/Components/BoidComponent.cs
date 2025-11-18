using Unity.Entities;

public struct BoidComponent : IComponentData
{
    public float NoiseOffset;
    public int Layer;
}
