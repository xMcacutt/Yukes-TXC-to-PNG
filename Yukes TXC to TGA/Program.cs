
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using DamienG.Security.Cryptography;
using System.Drawing;
using SixLabors.ImageSharp.Formats.Bmp;
using System;
using System.Drawing.Imaging;

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
            foreach (byte b in txc.Palette) Console.WriteLine(b.ToString("X"));
            var oF = File.Create(Path.GetFileNameWithoutExtension(path) + ".png");

            //MAGIC
            oF.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
            oF.Write(new byte[] { 0x0D, 0x0A, 0x1A, 0x0A });
            Crc32 crc32 = new();

            //HEADER
            var chunkData = Encoding.ASCII.GetBytes("IHDR")
                .Concat(BEBitConv.GetBytes(txc.Width - 1))
                .Concat(BEBitConv.GetBytes(txc.Height))
                .Concat(new byte[] { (byte)txc.BitDepth, 0x3, 0x0, 0x0, 0x0 })
                .ToArray();
            var crcBytes = crc32.ComputeHash(chunkData);
            oF.Write(BEBitConv.GetBytes(chunkData.Length - 4)
                .Concat(chunkData)
                .Concat(crcBytes).ToArray());

            //PALETTE
            chunkData = Encoding.ASCII.GetBytes("PLTE");
            List<byte> alphaValues = new List<byte>();
            for (int i = 0; i < Math.Pow(2, txc.BitDepth); i++)
            {
                byte[] bytes = new byte[3];
                bytes[0] = txc.Palette[i * 4];
                bytes[1] = txc.Palette[i * 4 + 1];
                bytes[2] = txc.Palette[i * 4 + 2];
                chunkData = chunkData.Concat(bytes).ToArray();
                alphaValues.Add(txc.Palette[(i * 4) + 3]);
            }
            crcBytes = crc32.ComputeHash(chunkData);
            oF.Write(BEBitConv.GetBytes(chunkData.Length - 4)
                .Concat(chunkData)
                .Concat(crcBytes).ToArray());

            //ALPHA
            chunkData = Encoding.ASCII.GetBytes("tRNS");
            var alphaArray = new byte[1];
            foreach(byte alpha in alphaValues)
            {
                alphaArray[0] = (byte)(alpha * 255/128);
                chunkData = chunkData.Concat(alphaArray).ToArray();
            }
            crcBytes = crc32.ComputeHash(chunkData);
            oF.Write(BEBitConv.GetBytes(chunkData.Length - 4)
                .Concat(chunkData)
                .Concat(crcBytes).ToArray());

            //DATA
            chunkData = Encoding.ASCII.GetBytes("IDAT");
            var compressedData = DeflateCompress(txc.Data);
            chunkData = chunkData.Concat(compressedData).ToArray();
            crcBytes = crc32.ComputeHash(chunkData);
            oF.Write(BEBitConv.GetBytes(chunkData.Length - 4)
                .Concat(chunkData)
                .Concat(crcBytes).ToArray());

            //END
            chunkData = Encoding.ASCII.GetBytes("IEND");
            crcBytes = crc32.ComputeHash(chunkData);
            oF.Write(BEBitConv.GetBytes(chunkData.Length - 4)
                .Concat(chunkData)
                .Concat(crcBytes).ToArray());

            oF.Close();
            Console.ReadLine();
            DeflateUncompress(compressedData);
            Console.ReadLine();
            return;

        }

        public static byte[] ReadBytes(FileStream f, int len)
        {
            byte[] buffer = new byte[len];
            f.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        public static byte[] DeflateCompress(byte[] uncompressedBytes)
        {
            // Create a MemoryStream to hold the compressed data
            using MemoryStream compressedStream = new MemoryStream();

            // Create a DeflateStream that writes to the MemoryStream
            using (ZLibStream stream = new(compressedStream, CompressionMode.Compress, true))
            {
                try
                {
                    // Write your uncompressed data to the DeflateStream
                    stream.Write(uncompressedBytes, 0, uncompressedBytes.Length);
                    // Flush the data to ensure it's properly compressed
                    stream.Flush();
                }
                catch (Exception ex)
                {
                    // Handle exceptions, e.g., log or throw
                    Console.WriteLine($"Compression error: {ex.Message}");
                    // You can choose to rethrow the exception here if needed
                }
            } // This will automatically close the DeflateStream

            // Get the compressed data as a byte array
            return compressedStream.ToArray();
        }

        public static byte[] DeflateUncompress(byte[] compressedBytes)
        {
            using MemoryStream uncompressedStream = new();
            using (MemoryStream compressedStream = new MemoryStream(compressedBytes))
            using (ZLibStream deflateStream = new(compressedStream, CompressionMode.Decompress))
            {
                try
                {
                    byte[] buffer = new byte[1024]; // You can adjust the buffer size as needed.
                    int bytesRead;
                    while ((bytesRead = deflateStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        uncompressedStream.Write(buffer, 0, bytesRead);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Decompression error: {ex.Message}");
                }
            }
            return uncompressedStream.ToArray();
        }
    }
}