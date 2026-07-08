using Godot;

namespace GodotVMF;

[Tool]
public partial class info_overlay : VMFEntityNode
{
    public Vector3 Uv0 => Entity.GetVector3("uv0");
    public Vector3 Uv1 => Entity.GetVector3("uv1");
    public Vector3 Uv2 => Entity.GetVector3("uv2");
    public Vector3 Uv3 => Entity.GetVector3("uv3");
    public string MaterialPath => Entity.GetString("material");
    public Vector3 BasisNormal => ConvertVector(Entity.GetVector3("BasisNormal", Vector3.Up));
    public Vector3 BasisU => ConvertVector(Entity.GetVector3("BasisU", Vector3.Right));
    public Vector3 BasisV => ConvertVector(Entity.GetVector3("BasisV", Vector3.Forward));

    private Decal DecalNode => GetNode<Decal>("decal");

    public override void EntitySetup(VMFEntity entity)
    {
        var material = VMTLoader.GetMaterial(MaterialPath);
        if (material == null)
        {
            QueueFree();
            return;
        }

        float minX = Mathf.Min(Mathf.Min(Uv0.X, Uv1.X), Mathf.Min(Uv2.X, Uv3.X)) * VMFConfig.Import.Scale;
        float minY = Mathf.Min(Mathf.Min(Uv0.Y, Uv1.Y), Mathf.Min(Uv2.Y, Uv3.Y)) * VMFConfig.Import.Scale;
        float maxX = Mathf.Max(Mathf.Max(Uv0.X, Uv1.X), Mathf.Max(Uv2.X, Uv3.X)) * VMFConfig.Import.Scale;
        float maxY = Mathf.Max(Mathf.Max(Uv0.Y, Uv1.Y), Mathf.Max(Uv2.Y, Uv3.Y)) * VMFConfig.Import.Scale;
        float width = maxX - minX;
        float height = maxY - minY;

        var decal = DecalNode;
        var size = decal.Size;
        size.X = width;
        size.Z = height;
        decal.Size = size;

        float side = BasisNormal.Dot(Vector3.Back) > 0
            || BasisNormal.Dot(Vector3.Right) > 0
            || BasisNormal.Dot(Vector3.Up) > 0
            ? -1f : 1f;

        var baseMaterial = material as BaseMaterial3D;

        GD.Print(BasisU, BasisV, BasisNormal);

        decal.TextureAlbedo = baseMaterial?.AlbedoTexture;
        decal.TextureNormal = baseMaterial?.NormalTexture;

        var basis = Basis;
        basis.X = -BasisU * side;
        basis.Z = BasisV * side;
        basis.Y = BasisNormal;
        Basis = basis;
    }
}
