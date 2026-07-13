using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

namespace GodotVMF;

public class VTFLoader
{
    public enum SRGBConversionMethod { Disabled, DuringImport, ProcessInShader }

    public enum ImageFormatEnum
    {
        Rgba8888 = 0, Abgr8888, Rgb888, Bgr888, Rgb565, I8, Ia88, P8, A8,
        Rgb888Bluescreen, Bgr888Bluescreen, Argb8888, Bgra8888,
        Dxt1 = 13, Dxt3 = 14, Dxt5 = 15,
        Bgrx8888, Bgr565, Bgrx5551, Bgra4444, Dxt1Onebitalpha, Bgra5551,
        Uv88, Uvwq8888, Rgba16161616F, Rgba16161616, Uvlx8888,
        None = -1
    }

    [Flags]
    public enum VTFFlags : uint
    {
        Pointsample = 0x00000001, Trilinear = 0x00000002, Clamps = 0x00000004,
        Clampt = 0x00000008, Anisotropic = 0x00000010, HintDxt5 = 0x00000020,
        Srgb = 0x00000040, Normal = 0x00000080, Nomip = 0x00000100,
        Nolod = 0x00000200, AllMips = 0x00000400, Procedural = 0x00000800,
        Onebitalpha = 0x00001000, Eightbitalpha = 0x00002000,
        Envmap = 0x00004000, Rendertarget = 0x00008000,
        Depthrendertarget = 0x00010000, Nodebugoverride = 0x00020000,
        Singlecopy = 0x00040000, PreSrgb = 0x00080000,
        Clampu = 0x02000000, Vertextexture = 0x04000000,
        Ssbump = 0x08000000, Border = 0x20000000,
    }

    private static readonly Dictionary<int, Image.Format> FormatMap = new()
    {
        { (int)ImageFormatEnum.Rgba8888, Image.Format.Rgba8 },
        { (int)ImageFormatEnum.Bgra8888, Image.Format.Rgba8 },
        { (int)ImageFormatEnum.Rgb888, Image.Format.Rgb8 },
        { (int)ImageFormatEnum.Bgr888, Image.Format.Rgb8 },
        { (int)ImageFormatEnum.I8, Image.Format.L8 },
        { (int)ImageFormatEnum.Ia88, Image.Format.La8 },
        { (int)ImageFormatEnum.Dxt1, Image.Format.Dxt1 },
        { (int)ImageFormatEnum.Dxt3, Image.Format.Dxt3 },
        { (int)ImageFormatEnum.Dxt5, Image.Format.Dxt5 },
    };

    private static readonly Dictionary<int, int> BytesPerPixelMap = new()
    {
        { (int)ImageFormatEnum.Rgba8888, 4 },
        { (int)ImageFormatEnum.Bgra8888, 4 },
        { (int)ImageFormatEnum.Rgb888, 3 },
        { (int)ImageFormatEnum.Bgr888, 3 },
        { (int)ImageFormatEnum.I8, 1 },
        { (int)ImageFormatEnum.Ia88, 2 },
    };

    private static readonly HashSet<int> BgrSwapFormats = new()
    {
        (int)ImageFormatEnum.Bgra8888, (int)ImageFormatEnum.Bgr888
    };

    private static readonly HashSet<int> SupportedFormats = new(FormatMap.Keys);

    private FileAccess? _file;
    private float _frameDuration;
    public string Path { get; private set; } = "";
    public bool Alpha { get; private set; }

    public string Signature { get { _file!.Seek(0); return _file.GetBuffer(16).GetStringFromUtf8(); } }

    public float Version
    {
        get
        {
            _file!.Seek(4);
            return float.Parse($"{_file.Get32()}.{_file.Get32()}", CultureInfo.InvariantCulture);
        }
    }

    public int HeaderSize { get { _file!.Seek(12); return (int)_file.Get32(); } }

    public int Width
    {
        get { _file!.Seek(16); int w = _file.Get16(); return w > 0 ? w : 512; }
    }

    public int Height
    {
        get { _file!.Seek(18); int h = _file.Get16(); return h > 0 ? h : 512; }
    }

    public uint Flags { get { _file!.Seek(20); return _file.Get32(); } }
    public int Frames { get { _file!.Seek(24); return _file.Get16(); } }
    public int FirstFrame { get { _file!.Seek(26); return _file.Get16(); } }

    public Vector3 Reflectivity
    {
        get { _file!.Seek(32); return new Vector3(_file.GetFloat(), _file.GetFloat(), _file.GetFloat()); }
    }

    public float BumpScale { get { _file!.Seek(48); return _file.GetFloat(); } }
    public int HiresImageFormat { get { _file!.Seek(52); return (int)_file.Get32(); } }

    public int MipmapCount
    {
        get
        {
            if ((Flags & (uint)VTFFlags.Nomip) != 0) return 1;
            _file!.Seek(56); return _file.Get8();
        }
    }

    public int LowResImageFormat { get { _file!.Seek(57); return (int)_file.Get32(); } }
    public int LowResImageWidth { get { _file!.Seek(61); return _file.Get8(); } }
    public int LowResImageHeight { get { _file!.Seek(62); return _file.Get8(); } }

    public int Depth
    {
        get { if (Version < 7.2f) return 0; _file!.Seek(63); return _file.Get8(); }
    }

    public int NumResources
    {
        get { if (Version < 7.3f) return 0; _file!.Seek(75); return (int)_file.Get32(); }
    }

    public VTFLoader(string path, float duration)
    {
        Path = path;
        _frameDuration = duration;
        _file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
    }

    public void Done() => _file?.Close();

    public static VTFLoader? Create(string path, float duration = 0f)
    {
        if (!FileAccess.FileExists(path))
        {
            GD.PushError($"File {path} does not exist");
            return null;
        }

        var vtf = new VTFLoader(path, duration);
        if (!SupportedFormats.Contains(vtf.HiresImageFormat))
        {
            GD.PushWarning($"Texture format {vtf.HiresImageFormat} is not supported ({System.IO.Path.GetFileName(path)})");
            vtf.Done();
            return null;
        }

        return vtf;
    }

    public Texture2D? CompileTexture(SRGBConversionMethod srgbMethod)
    {
        if (Width == 0 || Height == 0)
        {
            GD.PushError($"Corrupted file: {_file?.GetPath()}");
            return null;
        }

        int frames = Frames;
        Texture2D? tex;

        if (frames > 1)
        {
            var animTex = new AnimatedTexture { Frames = frames };
            for (int frame = 0; frame < frames; frame++)
            {
                animTex.SetFrameTexture(frame, ReadFrame(frame, srgbMethod));
                animTex.SetFrameDuration(frame, _frameDuration);
            }
            tex = animTex;
        }
        else
        {
            tex = ReadFrame(0, srgbMethod);
        }

        if (tex == null)
        {
            GD.PushError($"Texture not loaded: {Path}");
            return null;
        }

        return tex;
    }

    private Texture2D? ReadFrame(int frame, SRGBConversionMethod srgbMethod)
    {
        var dataList = new List<byte>();
        long byteRead = 0;
        bool isDxt1 = HiresImageFormat == (int)ImageFormatEnum.Dxt1;
        bool useMipmaps = (Flags & (uint)VTFFlags.Nomip) == 0;

        if (!FormatMap.TryGetValue(HiresImageFormat, out var format))
            return null;

        bool isUncompressed = BytesPerPixelMap.TryGetValue(HiresImageFormat, out int bytesPerPixel);

        int totalFrames = Frames;
        frame = totalFrames - 1 - frame;

        for (int i = 0; i < MipmapCount; i++)
        {
            int mipW = Math.Max(1, Width >> i);
            int mipH = Math.Max(1, Height >> i);
            int mipSize;
            if (isUncompressed)
            {
                mipSize = mipW * mipH * bytesPerPixel;
            }
            else
            {
                int multiplier = isDxt1 ? 8 : 16;
                mipSize = Math.Max(1, mipW / 4) * Math.Max(1, mipH / 4) * multiplier;
            }
            long fileLen = (long)_file!.GetLength();
            _file!.Seek((ulong)(fileLen - byteRead - mipSize - (long)mipSize * frame));
            dataList.AddRange(_file.GetBuffer(mipSize));
            byteRead += mipSize + (long)mipSize * (totalFrames - 1);
        }

        var frameData = dataList.ToArray();
        if (BgrSwapFormats.Contains(HiresImageFormat))
            SwapBgrChannels(frameData, bytesPerPixel);

        var img = Image.CreateFromData(Width, Height, useMipmaps, format, frameData);
        if (img == null)
        {
            GD.PushError($"Corrupted file: {_file?.GetPath()}");
            return null;
        }

        if (srgbMethod == SRGBConversionMethod.DuringImport)
        {
            img.Decompress();
            img.Compress(Image.CompressMode.S3Tc);
        }

        Alpha = (Flags & (uint)(VTFFlags.Onebitalpha | VTFFlags.Eightbitalpha)) != 0;

        return ImageTexture.CreateFromImage(img);
    }

    private static void SwapBgrChannels(byte[] data, int bytesPerPixel)
    {
        for (int i = 0; i + 2 < data.Length; i += bytesPerPixel)
            (data[i], data[i + 2]) = (data[i + 2], data[i]);
    }

    public static Texture2D? GetTexture(string texture)
    {
        texture = texture.ToLower();
        string[] extensions = { ".vtf", ".tga", ".png", ".jpg" };

        foreach (var ext in extensions)
        {
            string texPath = VMFUtils.NormalizePath(VMFConfig.Materials.TargetFolder + "/" + texture + ext);
            if (ResourceLoader.Exists(texPath))
                return ResourceLoader.Load<Texture2D>(texPath);
        }

        VMFLogger.Warn("Texture not found: " + texture);
        return null;
    }
}
