using System;
using UnityEngine;
using ModUI.Internals;
using System.IO;
using System.ComponentModel;

namespace ModUI.Assets
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class IModAssetsExpansion
    {
        public static string GetModAssetsFolder(this IModAssets modAssets)
        {
            if (!(modAssets is Mod)) throw new Exception("GetModAssetsFolder is for Mods only!");
            var mod = (Mod)modAssets;

            return Path.Combine(_ModUI.assetsPath, mod.ID);
        }
    }
    public interface IModAssets
    {
        bool UseAssetsFolder { get; }
    }

    public class ModAssets
    {
        public static Texture2D LoadTexture(Mod mod, string fileName)
        {
            if (!(mod is IModAssets)) throw new Exception($"ModUI: {mod.ID} is missing the IModAssets interface!");

            string fn = Path.Combine(IModAssetsExpansion.GetModAssetsFolder((IModAssets)mod));

            if (!File.Exists(fn))
            {
                throw new FileNotFoundException($"<b>LoadTexture() Error:</b> File not found: {fn}{Environment.NewLine}", fn);
            }
            string ext = Path.GetExtension(fn).ToLower();
            if (ext == ".png" || ext == ".jpg")
            {
                Texture2D t2d = new Texture2D(1, 1);
                t2d.LoadImage(File.ReadAllBytes(fn));
                return t2d;
            }
            else if (ext == ".dds")
            {
                Texture2D returnTex = LoadDDS(fn);
                return returnTex;
            }
            else if (ext == ".tga")
            {
                Texture2D returnTex = LoadTGA(fn);
                return returnTex;
            }
            else
            {
                throw new NotSupportedException($"LoadTexture() Error: Texture not supported: {fileName}{Environment.NewLine}");
            }
        }

        public static AssetBundle LoadBundle(byte[] assetBundleFromResources)
        {
            if (assetBundleFromResources != null) return AssetBundle.LoadFromMemory(assetBundleFromResources);
            else throw new Exception($"LoadBundle() Error: Resource doesn't exist {Environment.NewLine}");
        }
        public static AssetBundle LoadBundle(Mod mod, string bundleName)
        {
            if (!(mod is IModAssets)) throw new Exception($"ModUI: {mod.ID} is missing the IModAssets interface!");

            string bundle = Path.Combine(_ModUI.assetsPath, mod.ID, bundleName);
            if (File.Exists(bundle))
            {
                Debug.Log($"Loading Asset: {bundleName}...");
                return LoadBundle(File.ReadAllBytes(bundle));
            }
            else
            {
                throw new FileNotFoundException($"LoadBundle() Error: File not found: {bundle}{Environment.NewLine}", bundleName);
            }
        }
        public static AssetBundle LoadBundle(string assetBundleEmbeddedResources)
        {
            System.Reflection.Assembly a = System.Reflection.Assembly.GetCallingAssembly();
            using (Stream resFilestream = a.GetManifestResourceStream(assetBundleEmbeddedResources))
            {
                if (resFilestream == null)
                {
                    throw new Exception($"LoadBundle() Error: Resource doesn't exist {Environment.NewLine}");
                }
                else
                {
                    byte[] ba = new byte[resFilestream.Length];
                    resFilestream.Read(ba, 0, ba.Length);
                    return AssetBundle.LoadFromMemory(ba);
                }
            }
        }

        // TGALoader by https://gist.github.com/mikezila/10557162
        internal static Texture2D LoadTGA(string fileName)
        {
            using (var imageFile = File.OpenRead(fileName))
            {
                return LoadTGA(imageFile);
            }
        }

        //DDS loader based on https://raw.githubusercontent.com/hobbitinisengard/crashday-3d-editor/7e7c6c78c9f67588156787af1af92cfad1019de9/Assets/IO/DDSDecoder.cs
        internal static Texture2D LoadDDS(string ddsPath)
        {
            try
            {
                byte[] ddsBytes = File.ReadAllBytes(ddsPath);

                byte ddsSizeCheck = ddsBytes[4];
                if (ddsSizeCheck != 124)
                    throw new Exception("Invalid DDS DXTn texture. Unable to read"); //header byte should be 124 for DDS image files

                int height = ddsBytes[13] * 256 + ddsBytes[12];
                int width = ddsBytes[17] * 256 + ddsBytes[16];

                byte DXTType = ddsBytes[87];
                TextureFormat textureFormat = TextureFormat.DXT5;
                if (DXTType == 49)
                {
                    textureFormat = TextureFormat.DXT1;
                }

                if (DXTType == 53)
                {
                    textureFormat = TextureFormat.DXT5;
                }
                int DDS_HEADER_SIZE = 128;
                byte[] dxtBytes = new byte[ddsBytes.Length - DDS_HEADER_SIZE];
                Buffer.BlockCopy(ddsBytes, DDS_HEADER_SIZE, dxtBytes, 0, ddsBytes.Length - DDS_HEADER_SIZE);

                FileInfo finf = new FileInfo(ddsPath);
                Texture2D texture = new Texture2D(width, height, textureFormat, false);
                texture.LoadRawTextureData(dxtBytes);
                texture.Apply();
                texture.name = finf.Name;

                return texture;
            }
            catch (Exception ex)
            {
                Debug.LogError($"LoadTexture() Error:{Environment.NewLine}Error: Could not load DDS texture{Environment.NewLine}Exception: {ex}");
                return new Texture2D(8, 8);
            }
        }

        // TGALoader by https://gist.github.com/mikezila/10557162
        static Texture2D LoadTGA(Stream TGAStream)
        {

            using (BinaryReader r = new BinaryReader(TGAStream))
            {
                r.BaseStream.Seek(12, SeekOrigin.Begin);

                short width = r.ReadInt16();
                short height = r.ReadInt16();
                int bitDepth = r.ReadByte();
                r.BaseStream.Seek(1, SeekOrigin.Current);

                Texture2D tex = new Texture2D(width, height);
                Color32[] pulledColors = new Color32[width * height];

                if (bitDepth == 32)
                {
                    for (int i = 0; i < width * height; i++)
                    {
                        byte red = r.ReadByte();
                        byte green = r.ReadByte();
                        byte blue = r.ReadByte();
                        byte alpha = r.ReadByte();

                        pulledColors[i] = new Color32(blue, green, red, alpha);
                    }
                }
                else if (bitDepth == 24)
                {
                    for (int i = 0; i < width * height; i++)
                    {
                        byte red = r.ReadByte();
                        byte green = r.ReadByte();
                        byte blue = r.ReadByte();

                        pulledColors[i] = new Color32(blue, green, red, 1);
                    }
                }
                else
                {
                    throw new Exception($"LoadTexture() Error: TGA texture is not 32 or 24 bit depth.{Environment.NewLine}");
                }

                tex.SetPixels32(pulledColors);
                tex.Apply();
                return tex;

            }
        }
    }
}