using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

namespace GodotVMF;

[Tool]
public partial class VMFMaterialConversionContextMenu : EditorContextMenuPlugin
{
    private const string VmtTemplate =
        "\"LightmappedGeneric\" {\n\t$basetexture \"%s\" \n}";

    private const string VmtBlendTemplate =
        "\"WorldVertexTransition\" {\n" +
        "\t$basetexture \"%s\" \n\t$bumpmap \"%s\" \n\t$roughnesstexture \"%s\" \n\t$ambientocclusiontexture \"%s\" \n" +
        "\t$basetexture2 \"%s\" \n\t$bumpmap2 \"%s\" \n\t$roughnesstexture2 \"%s\" \n\t$ambientocclusiontexture2 \"%s\" \n}";

    private static bool IsResource(string p) => p.EndsWith(".tres");
    private static bool IsTexture(string p) { var e = System.IO.Path.GetExtension(p).TrimStart('.').ToLower(); return e is "png" or "jpg" or "tga"; }
    private static bool IsVmt(string p) => System.IO.Path.GetExtension(p).ToLower() == ".vmt";

    public override void _PopupMenu(string[] paths)
    {
        if (paths.Any(IsResource))
            AddContextMenuItem("Convert to VMT", Callable.From<string[]>(ConvertResourceToVmt));
        if (paths.Any(IsTexture))
            AddContextMenuItem("Create VMT materials", Callable.From<string[]>(CreateVmtsFromTextures));
        if (paths.Count(IsVmt) > 1)
            AddContextMenuItem("Create VMT Blend Material", Callable.From<string[]>(CreateBlendMaterial));
    }

    private static void CreateBlendMaterial(string[] paths)
    {
        var vmts = paths.Where(IsVmt).ToArray();
        if (vmts.Length < 2) return;

        var vmt1 = ResourceLoader.Load<Material>(vmts[0]);
        var vmt2 = ResourceLoader.Load<Material>(vmts[1]);
        if (vmt1 == null || vmt2 == null) return;

        string blendName = System.IO.Path.GetFileNameWithoutExtension(vmts[0]) + "_" +
                           System.IO.Path.GetFileNameWithoutExtension(vmts[1]) + "_blend.vmt";
        string savePath = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(vmts[0]))!
                          .Replace("\\", "/") + "/" + blendName;

        var d1 = vmt1.GetMeta("details", new Godot.Collections.Dictionary()).AsGodotDictionary();
        var d2 = vmt2.GetMeta("details", new Godot.Collections.Dictionary()).AsGodotDictionary();

        string Get(Godot.Collections.Dictionary d, string k) =>
            d.TryGetValue(k, out var v) ? v.AsString() : "";

        string content = string.Format(VmtBlendTemplate,
            Get(d1, "$basetexture"), Get(d1, "$bumpmap"),
            Get(d1, "$roughnesstexture"), Get(d1, "$ambientocclusiontexture"),
            Get(d2, "$basetexture"), Get(d2, "$bumpmap"),
            Get(d2, "$roughnesstexture"), Get(d2, "$ambientocclusiontexture"));

        using var file = FileAccess.Open(savePath, FileAccess.ModeFlags.Write);
        file?.StoreString(content);

        EditorInterface.Singleton.GetResourceFilesystem().Scan();
    }

    private static void CreateVmtsFromTextures(string[] paths)
    {
        foreach (var path in paths.Where(IsTexture))
        {
            var texture = ResourceLoader.Load<Texture2D>(path);
            if (texture == null) continue;

            string basePath = path.Replace(VMFConfig.Materials.TargetFolder, "").TrimStart('/')
                                  .Replace("." + System.IO.Path.GetExtension(path).TrimStart('.'), "");
            string vmtPath = VMFUtils.NormalizePath(VMFConfig.Materials.TargetFolder + "/" + basePath + ".vmt");

            using (var file = FileAccess.Open(vmtPath, FileAccess.ModeFlags.Write))
                file?.StoreString(string.Format(VmtTemplate, basePath));

            var bytes = GenerateVtfFile(texture);
            using (var vtfFile = FileAccess.Open(vmtPath.Replace(".vmt", ".vtf"), FileAccess.ModeFlags.Write))
                vtfFile?.StoreBuffer(bytes);
        }
        EditorInterface.Singleton.GetResourceFilesystem().Scan();
    }

    private static void ConvertResourceToVmt(string[] paths)
    {
        foreach (var resFile in paths.Where(IsResource))
        {
            var resource = ResourceLoader.Load(resFile);
            if (resource is not BaseMaterial3D mat)
            {
                VMFLogger.Warn("Resource is not a BaseMaterial3D: " + resFile);
                continue;
            }
            CreateVmtFile(mat);
        }
        EditorInterface.Singleton.GetResourceFilesystem().Scan();
    }

    private static void CreateVmtFile(BaseMaterial3D material)
    {
        var baseTexture = material.AlbedoTexture;
        string vmtPath = material.ResourcePath.Replace(".tres", ".vmt");
        string vtfPath = baseTexture != null
            ? System.IO.Path.ChangeExtension(baseTexture.ResourcePath, ".vtf")
            : "";

        if (ResourceLoader.Exists(vmtPath)) { VMFLogger.Warn("VMT already exists: " + vmtPath); return; }

        string baseTexturePath = vtfPath
            .Replace(VMFConfig.Materials.TargetFolder, "")
            .Replace(".vtf", "")
            .TrimStart('/');

        using (var file = FileAccess.Open(vmtPath, FileAccess.ModeFlags.Write))
            file?.StoreString(string.Format(VmtTemplate, baseTexturePath));

        if (baseTexture == null) return;
        if (ResourceLoader.Exists(vtfPath)) { VMFLogger.Warn("VTF already exists: " + vtfPath); return; }

        var bytes = GenerateVtfFile(baseTexture);
        using var vtfFile = FileAccess.Open(vtfPath, FileAccess.ModeFlags.Write);
        vtfFile?.StoreBuffer(bytes);
    }

    private static byte[] GenerateVtfFile(Texture2D texture)
    {
        var image = texture.GetImage().Duplicate() as Image ?? texture.GetImage();
        float aspectRatio = (float)image.GetWidth() / image.GetHeight();
        var bytes = new List<byte>();

        bool isDxt = image.GetFormat() is Image.Format.Dxt1 or Image.Format.Dxt3 or Image.Format.Dxt5;
        if (!isDxt)
        {
            image.Decompress();
            image.Convert(Image.Format.Rgba8);
            image.Compress(Image.CompressMode.S3Tc);
        }

        bytes.AddRange(Encoding.UTF8.GetBytes("VTF"));
        bytes.Add(0);
        bytes.AddRange(Int32ToBytes(7));   // version major
        bytes.AddRange(Int32ToBytes(4));   // version minor
        bytes.AddRange(Int32ToBytes(80));  // header size
        bytes.AddRange(ShortToBytes(image.GetWidth()));
        bytes.AddRange(ShortToBytes(image.GetHeight()));

        uint flags = (uint)(VTFLoader.VTFFlags.Srgb | VTFLoader.VTFFlags.Nomip);
        int mipLevels = 1;
        if (image.HasMipmaps())
        {
            flags &= ~(uint)VTFLoader.VTFFlags.Nomip;
            mipLevels = image.GetMipmapCount() + 1;
        }

        bytes.AddRange(Int32ToBytes((int)flags));
        bytes.AddRange(ShortToBytes(1));  // frames
        bytes.AddRange(ShortToBytes(0));  // first frame
        bytes.AddRange(new byte[4]);      // padding
        bytes.AddRange(FloatToBytes(0f)); bytes.AddRange(FloatToBytes(0f)); bytes.AddRange(FloatToBytes(0f)); // reflectivity
        bytes.AddRange(new byte[4]);      // padding
        bytes.AddRange(FloatToBytes(1f)); // bump scale

        int format = image.GetFormat() == Image.Format.Dxt1 ? 13 : image.GetFormat() == Image.Format.Dxt3 ? 14 : 15;
        bytes.AddRange(Int32ToBytes(format));
        bytes.Add((byte)mipLevels);
        bytes.AddRange(Int32ToBytes(13)); // low res format

        var lowres = image.Duplicate() as Image ?? image;
        int lrW = 16, lrH = System.Math.Max(1, System.Math.Min(16, (int)(16f / aspectRatio)));
        lowres.Decompress();
        lowres.Resize(lrW, lrH, Image.Interpolation.Bilinear);
        lowres.Convert(Image.Format.Rgb8);
        lowres.Compress(Image.CompressMode.S3Tc);

        bytes.Add((byte)lrW);
        bytes.Add((byte)lrH);
        bytes.Add(1); // depth
        bytes.AddRange(new byte[3]); // padding
        bytes.AddRange(Int32ToBytes(0)); // resource count

        while (bytes.Count < 80) bytes.Add(0);

        var lowresData = lowres.GetData();
        int lowresSize = (int)(lrW * lrH * 0.5f);
        if (lowresData.Length > lowresSize)
        {
            var sliced = new byte[lowresSize];
            System.Array.Copy(lowresData, sliced, lowresSize);
            lowresData = sliced;
        }
        bytes.AddRange(lowresData);

        var imageData = image.GetData();
        var mipDataList = new List<byte[]>();
        int offset = 0;
        for (int mip = 0; mip < mipLevels; mip++)
        {
            int blockSize = format is 14 or 15 ? 16 : 8;
            int mipW = System.Math.Max(1, image.GetWidth() >> mip);
            int mipH = System.Math.Max(1, image.GetHeight() >> mip);
            int blocksX = System.Math.Max(1, (mipW + 3) / 4);
            int blocksY = System.Math.Max(1, (mipH + 3) / 4);
            int mipSize = blocksX * blocksY * blockSize;
            var mipData = new byte[mipSize];
            System.Array.Copy(imageData, offset, mipData, 0, System.Math.Min(mipSize, imageData.Length - offset));
            mipDataList.Add(mipData);
            offset += mipSize;
        }
        mipDataList.Reverse();
        foreach (var d in mipDataList) bytes.AddRange(d);

        return bytes.ToArray();
    }

    private static byte[] Int32ToBytes(int v) => new[] { (byte)(v & 0xFF), (byte)((v >> 8) & 0xFF), (byte)((v >> 16) & 0xFF), (byte)((v >> 24) & 0xFF) };
    private static byte[] ShortToBytes(int v) => new[] { (byte)(v & 0xFF), (byte)((v >> 8) & 0xFF) };
    private static byte[] FloatToBytes(float v) { int i = (int)v; return Int32ToBytes(i); }
}
