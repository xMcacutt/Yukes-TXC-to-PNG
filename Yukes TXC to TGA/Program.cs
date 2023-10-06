
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using DamienG.Security.Cryptography;
using SixLabors.ImageSharp;
using System;
using System.Drawing.Imaging;
using SixLabors.ImageSharp.Formats;

namespace Yukes_TXC_to_TGA
{
    public class Program
    {
        static string _path;
        static bool _run = true;

        public static void Main(string[] args)
        {
            if(args.Length == 1)
            {
                if (File.Exists(args[0]))
                {
                    Convert(args[0]);
                    return;
                }
            }

            while (_run)
            {
                Convert(Console.ReadLine());
            }
        }

        public static void Convert(string path)
        {
            var f = File.OpenRead(path);

            if(!Enumerable.SequenceEqual(ReadBytes(f, 4), new byte[] { 0x52, 0x54, 0x58, 0x33 }))
            {
                Console.WriteLine("File is not txc. RTX3 Magic not present. Ending conversion.");
                return;
            }
            TXC txc = new();
            f.Seek(0x1C, SeekOrigin.Begin);
            uint bppMarker = BitConverter.ToUInt32(ReadBytes(f, 4), 0);
            switch (bppMarker)
            {
                case 0x14: 
                    txc.BitDepth = 4; 
                    break;
                case 0x13: 
                    txc.BitDepth = 8; 
                    break;
                default:
                    Console.WriteLine($"Unrecognised bitdepth marker {bppMarker:X}. Ending conversion.");
                    return;
            }
            txc.Width = BitConverter.ToUInt16(ReadBytes(f, 2), 0);
            txc.Height = BitConverter.ToUInt16(ReadBytes(f, 2), 0);
            f.Seek(0x18, SeekOrigin.Current);
            txc.PaletteOffset = (int)BitConverter.ToUInt32(ReadBytes(f, 4), 0) + 0x8;
            f.Seek(txc.PaletteOffset, SeekOrigin.Begin);
            txc.Palette = ReadBytes(f, (int)(f.Length - txc.PaletteOffset));
            f.Seek(0x40, SeekOrigin.Begin);
            txc.Data = ReadBytes(f, (int)(f.Length - txc.Palette.Length - 0x40));
            f.Close();
            
            txc.RGBAData = ComposeRGBA(txc.Data, txc.Palette, txc.BitDepth);

            Image<Rgba32> image = Image.LoadPixelData<Rgba32>(txc.RGBAData, txc.Width, txc.Height);
            image.Save(Path.GetFileNameWithoutExtension(path) + ".png", new SixLabors.ImageSharp.Formats.Png.PngEncoder());
            return;

        }

        public static byte[] ReadBytes(FileStream f, int len)
        {
            byte[] buffer = new byte[len];
            f.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        public static byte[] ComposeRGBA(byte[] data, byte[] palette, int depth)
        {
            using MemoryStream output = new MemoryStream();
            foreach(byte b in data)
            {
                if(depth == 4)
                {
                    int i1 = (b & 0xF0) >> 4;
                    int i2 = b & 0x0F;
                    output.Write(new byte[] { palette[i1 * 4], palette[i1 * 4 + 1], palette[i1 * 4 + 2], (byte)(palette[i1 * 4 + 3] * 255 / 128) });
                    output.Write(new byte[] { palette[i2 * 4], palette[i2 * 4 + 1], palette[i2 * 4 + 2], (byte)(palette[i2 * 4 + 3] * 255 / 128) });
                }
                if(depth == 8)
                {
                    output.Write(new byte[] { palette[b * 4], palette[b * 4 + 1], palette[b * 4 + 2], (byte)(palette[b * 4 + 3] * 255/128) });
                }
            }
            return output.ToArray();
        }
    }
}