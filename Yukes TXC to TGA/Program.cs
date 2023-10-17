
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using DamienG.Security.Cryptography;
using SixLabors.ImageSharp;
using System;
using System.Drawing.Imaging;
using SixLabors.ImageSharp.Formats;
using ImageMagick;
using System.IO;

namespace Yukes_TXC_to_PNG
{
    public class Program
    {
        static string _path;
        static bool _run = true;

        public static void Main(string[] args)
        {
            foreach(string arg in args)
            {
                if (File.Exists(arg))
                {
                    Convert(arg);        
                }
            }
            if (args.Length != 0) return;

            while (_run)
            {
                Console.Clear();
                Console.WriteLine("TXC Converter for The DOG Island");

                string input = "";
                while (!string.Equals(input, "y", StringComparison.CurrentCultureIgnoreCase) && !string.Equals(input, "n", StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.WriteLine("Would you like to convert all TXCs?   Y / N");
                    input = Console.ReadLine();
                }
                if (string.Equals(input, "y", StringComparison.CurrentCultureIgnoreCase))
                {
                    input = "";
                    while (!string.Equals(input, "y", StringComparison.CurrentCultureIgnoreCase) && !string.Equals(input, "n", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Console.WriteLine("Move files when complete?   Y / N");
                        input = Console.ReadLine();
                    }
                    bool move = string.Equals(input, "y", StringComparison.CurrentCultureIgnoreCase);
                    
                    Console.WriteLine("Please enter path to root directory");
                    _path = Console.ReadLine().Replace("\"", "");
                    while (!Directory.Exists(_path))
                    {
                        Console.WriteLine("Path was invalid");
                        Console.WriteLine("Please enter path to root directory");
                        _path = Console.ReadLine().Replace("\"", " ");
                    }
                    string output = "";
                    if (move)
                    {
                        Console.WriteLine("Please enter path to output root directory");
                        output = Console.ReadLine().Replace("\"", "");
                        while (!Directory.Exists(_path))
                        {
                            Console.WriteLine("Path was invalid");
                            Console.WriteLine("Please enter path to output root directory");
                            output = Console.ReadLine().Replace("\"", " ");
                        }
                    }
                    Console.WriteLine("Converting...");
                    ConvertAll(_path, output);
                    Console.WriteLine("Conversion completed successfully");
                    Console.ReadLine();
                }
                else
                {
                    Console.WriteLine("Please enter path to TXC");
                    _path = Console.ReadLine().Replace("\"", "");
                    while (!File.Exists(_path) || !_path.EndsWith(".txc", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Console.WriteLine("Path was invalid");
                        Console.WriteLine("Please enter path to TXC");
                        _path = Console.ReadLine().Replace("\"", " ");
                    }
                    Console.WriteLine("Converting...");
                    Convert(_path);
                    Console.WriteLine("Conversion completed successfully");
                    Console.ReadLine();
                }
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
            f.Seek(0x20, SeekOrigin.Begin);
            txc.Width = BitConverter.ToUInt16(ReadBytes(f, 2), 0);
            txc.Height = BitConverter.ToUInt16(ReadBytes(f, 2), 0);
            f.Seek(0x16, SeekOrigin.Current);
            txc.BitDepth = (int)Math.Log2(BitConverter.ToInt16(ReadBytes(f, 2), 0));
            txc.PaletteOffset = (int)BitConverter.ToUInt32(ReadBytes(f, 4), 0) + 0x8;
            f.Seek(txc.PaletteOffset, SeekOrigin.Begin);
            txc.Palette = ReadBytes(f, (int)(f.Length - txc.PaletteOffset));
            f.Seek(0x40, SeekOrigin.Begin);
            txc.Data = ReadBytes(f, (int)(f.Length - txc.Palette.Length - 0x40));
            f.Close();
            
            txc.Palette = PS2ShiftPalette(txc.Palette);
            txc.RGBAData = ComposeRGBA(txc.Data, txc.Palette, txc.BitDepth);

            Image<Rgba32> image = Image.LoadPixelData<Rgba32>(txc.RGBAData, txc.Width, txc.Height);
            image.Save(Path.ChangeExtension(path, "png"), new SixLabors.ImageSharp.Formats.Png.PngEncoder());
            return;

        }

        public static void ConvertAll(string inDir, string outDir)
        {
            foreach (var file in Directory.GetFiles(inDir, "*.txc", SearchOption.AllDirectories))
            {
                var f = File.OpenRead(file);

                if (!Enumerable.SequenceEqual(ReadBytes(f, 4), new byte[] { 0x52, 0x54, 0x58, 0x33 }))
                {
                    Console.WriteLine("File is not txc. RTX3 Magic not present. Ending conversion.");
                    return;
                }
                TXC txc = new();
                f.Seek(0x20, SeekOrigin.Begin);
                txc.Width = BitConverter.ToUInt16(ReadBytes(f, 2), 0);
                txc.Height = BitConverter.ToUInt16(ReadBytes(f, 2), 0);
                f.Seek(0x16, SeekOrigin.Current);
                txc.BitDepth = (int)Math.Log2(BitConverter.ToInt16(ReadBytes(f, 2), 0));
                txc.PaletteOffset = (int)BitConverter.ToUInt32(ReadBytes(f, 4), 0) + 0x8;
                f.Seek(txc.PaletteOffset, SeekOrigin.Begin);
                txc.Palette = ReadBytes(f, (int)(f.Length - txc.PaletteOffset));
                f.Seek(0x40, SeekOrigin.Begin);
                txc.Data = ReadBytes(f, (int)(f.Length - txc.Palette.Length - 0x40));
                f.Close();

                txc.Palette = PS2ShiftPalette(txc.Palette);
                txc.RGBAData = ComposeRGBA(txc.Data, txc.Palette, txc.BitDepth);

                Image<Rgba32> image = Image.LoadPixelData<Rgba32>(txc.RGBAData, txc.Width, txc.Height);

                string relativePath = Path.GetRelativePath(inDir, file);
                string outputPath = Path.Combine(outDir, relativePath);

                // Create the necessary directories if they don't exist
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                image.Save(Path.ChangeExtension(outputPath, "png"), new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                return;
            }
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
                    int i2 = (b & 0xF0) >> 4;
                    int i1 = b & 0x0F;
                    output.Write(new byte[] { palette[i1 * 4], palette[i1 * 4 + 1], palette[i1 * 4 + 2], (byte)(palette[i1 * 4 + 3] * 255f / 128f) });
                    output.Write(new byte[] { palette[i2 * 4], palette[i2 * 4 + 1], palette[i2 * 4 + 2], (byte)(palette[i2 * 4 + 3] * 255f / 128f) });
                }
                if(depth == 8)
                {
                    output.Write(new byte[] { palette[b * 4], palette[b * 4 + 1], palette[b * 4 + 2], (byte)(palette[b * 4 + 3] * 255f/128f) });
                }
            }
            return output.ToArray();
        }

        public static byte[] PS2ShiftPalette(byte[] palette)
        {
            if (palette.Length < 128) return palette;
            using MemoryStream output = new MemoryStream();
            for (int iChunk = 0; iChunk < palette.Length / 128; iChunk++)
            {
                byte[][] groups = new byte[4][];
                for(int iGroup = 0; iGroup < 4; iGroup++)
                {
                    groups[iGroup] = palette.Skip(iChunk * 128 + iGroup * 32).Take(32).ToArray();
                }
                output.Write(groups[0]);
                output.Write(groups[2]);
                output.Write(groups[1]);
                output.Write(groups[3]);
            }
            return output.ToArray();
        }

    }
}