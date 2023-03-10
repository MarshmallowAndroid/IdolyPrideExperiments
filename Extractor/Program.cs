using Extractor.Octo.Data;
using Extractor.Octo.Proto;
using Extractor.Solis.Common;
using ProtoBuf;
using System.Reflection;
using System.Text;

namespace Extractor
{
    internal class Program
    {
        private static readonly string aesKey = "zkfuuwgc4eoxlaew";

        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine($"Usage: {Assembly.GetExecutingAssembly().GetName().Name}.exe <octo dir> <appid> <version> [output dir]");
                return;
            }

            string outputDir = "";
            if (args.Length == 4)
                outputDir = args[3];

            string cachePath = Path.Combine(args[0], "pdb", args[1], args[2], "octocacheevai");

            AesCrypt crypt = new(aesKey);
            SecureFile sf = new(cachePath, crypt);
            MemoryStream dbStream = new(sf.Load());

            ProtoReader.State reader = ProtoReader.State.Create(dbStream, null);
            Database db = ReadDatabase(reader);

            Console.Write("Extract " + db.AssetBundleList.Count + " asset bundles? (Y/N) ");
            ConsoleKeyInfo key = Console.ReadKey();
            if (key.Key == ConsoleKey.Y) ExtractAssetBundles(args[0], db.AssetBundleList, outputDir);
        }

        private static Database ReadDatabase(ProtoReader.State reader)
        {
            Database database = new();

            int field = reader.ReadFieldHeader();

            SubItemToken token;
            bool fieldHeaderRead;

            do
            {
                if (field < 1) return database;

                switch (field)
                {
                    case 1:
                        database.Revision = reader.ReadInt32();
                        break;
                    case 2:
                        do
                        {
                            token = reader.StartSubItem();
                            database.AssetBundleList.Add(ReadData(reader));
                            reader.EndSubItem(token);
                            fieldHeaderRead = reader.TryReadFieldHeader(reader.FieldNumber);
                        } while (fieldHeaderRead);
                        break;
                    case 3:
                        do
                        {
                            token = reader.StartSubItem();
                            database.TagName.Add(reader.ReadString());
                            reader.EndSubItem(token);
                            fieldHeaderRead = reader.TryReadFieldHeader(reader.FieldNumber);
                        } while (fieldHeaderRead);
                        break;
                    case 4:
                        do
                        {
                            token = reader.StartSubItem();
                            database.ResourceList.Add(ReadData(reader));
                            reader.EndSubItem(token);
                            fieldHeaderRead = reader.TryReadFieldHeader(reader.FieldNumber);
                        } while (fieldHeaderRead);
                        break;
                    case 5:
                        database.UrlFormat = reader.ReadString();
                        break;
                    default:
                        reader.AppendExtensionData(database);
                        break;
                }

                try
                {
                    field = reader.ReadFieldHeader();
                }
                catch (Exception)
                {
                    return database;
                }
            } while (true);
        }

        private static Data ReadData(ProtoReader.State reader)
        {
            Data data = new();

            List<int> intList;
            WireType wireType;
            SubItemToken token;
            bool hasSubValue;

            int field = reader.ReadFieldHeader();
            if (field > 0)
            {
                do
                {
                    switch (field)
                    {
                        case 1:
                            data.Id = reader.ReadInt32();
                            break;
                        case 2:
                            data.FilePath = reader.ReadString();
                            break;
                        case 3:
                            data.Name = reader.ReadString();
                            break;
                        case 4:
                            data.Size = reader.ReadInt32();
                            break;
                        case 5:
                            data.Crc = reader.ReadUInt32();
                            break;
                        case 6:
                            data.Priority = reader.ReadInt32();
                            break;
                        case 7:
                            intList = data.TagId;
                            wireType = reader.WireType;

                            if (wireType == WireType.String)
                            {
                                token = reader.StartSubItem();
                                while (true)
                                {
                                    hasSubValue = reader.HasSubValue(wireType);
                                    if (!hasSubValue) break;
                                    intList.Add(reader.ReadInt32());
                                }
                                reader.EndSubItem(token);
                            }
                            else
                            {
                                bool fieldHeaderRead;
                                do
                                {
                                    intList.Add(reader.ReadInt32());
                                    fieldHeaderRead = reader.TryReadFieldHeader(reader.FieldNumber);
                                } while (fieldHeaderRead);
                            }
                            break;
                        case 8:
                            List<int> list = data.Dependencies;
                            wireType = reader.WireType;

                            if (wireType == WireType.String)
                            {
                                token = reader.StartSubItem();
                                while (true)
                                {
                                    hasSubValue = reader.HasSubValue(wireType);
                                    if (!hasSubValue) break;
                                    list.Add(reader.ReadInt32());
                                }
                                reader.EndSubItem(token);
                            }
                            else
                            {
                                bool fieldHeaderRead;
                                do
                                {
                                    list.Add(reader.ReadInt32());
                                    fieldHeaderRead = reader.TryReadFieldHeader(reader.FieldNumber);
                                } while (fieldHeaderRead);
                            }
                            break;
                        case 9:
                            data.DataState = (Data.State)reader.ReadInt32();
                            break;
                        case 10:
                            data.Md5 = reader.ReadString();
                            break;
                        case 11:
                            data.ObjectName = reader.ReadString();
                            break;
                        case 12:
                            data.Generation = reader.ReadUInt64();
                            break;
                        case 13:
                            data.UploadVersionId = reader.ReadInt32();
                            break;
                        default:
                            reader.AppendExtensionData(data);
                            break;
                    }
                    field = reader.ReadFieldHeader();
                } while (field > 0);

            }

            return data;
        }

        private static void ExtractAssetBundles(string octoDir, List<Data> assetBundleList, string outputDir)
        {
            Console.Clear();
            Console.CursorVisible = false;
            ProgressBar pb = new(100, (Console.WindowWidth / 2) - 50, (Console.WindowHeight / 2) - 3);

            Directory.CreateDirectory(outputDir);
            string[] files = Directory.GetFiles(octoDir, "*", SearchOption.AllDirectories);

            Dictionary<string, string> fileDict = new();
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                if (fileName.Equals(".meta")) continue;
                if (!File.Exists(file)) continue;
                fileDict.Add(fileName, file);
            }

            int fileIndex = 1;
            int fileCount = fileDict.Count;
            foreach (var item in assetBundleList)
            {
                pb.UpdateProgress("Extracting " + item.Name, fileIndex, fileCount);

                if (!fileDict.ContainsKey(item.Md5)) continue;
                string filePath = fileDict[item.Md5];
                if (!File.Exists(filePath)) continue;
                FileStream abFs = File.OpenRead(filePath);

                Stream stream;

                if (MaskedHeaderStreamUtility.IsEncryptedStream(abFs))
                    stream = new MaskedHeaderStream(abFs, 256, item.Name);
                else
                    stream = abFs;

                FileStream dAbFs = File.Create(Path.Combine(outputDir, item.Name));
                stream.CopyTo(dAbFs);

                fileIndex++;
            }

            pb.UpdateProgress("Extracted " + fileCount + " asset bundles.", fileIndex, fileCount);
        }
    }

    class ProgressBar
    {
        private readonly int x;
        private readonly int y;

        public ProgressBar(int width, int x, int y)
        {
            this.x = x;
            this.y = y;
            Width = width;
        }

        public int Width { get; }

        public void UpdateProgress(string title, int value, int max)
        {
            float percentage = (float)value / max;
            int progress = (int)(percentage * Width);

            WriteAt(x, y, Pad(Pad("", (Width / 2) - (title.Length / 2)) + title, Width));

            StringBuilder buffer = new();
            for (int i = 0; i < Width; i++)
            {
                if (i <= progress) buffer.Append('█');
                else buffer.Append('░');
            }
            WriteAt(x, y + 2, buffer.ToString());
        }

        public void Clear()
        {
            WriteAt(x, y, Pad("", Width));
            StringBuilder buffer = new();
            buffer.Append(' ', Width);
            WriteAt(x, y + 1, buffer.ToString());
        }

        private static string Pad(string value, int width)
        {
            StringBuilder buffer = new();
            buffer.Append(value);
            buffer.Append(' ', width - value.Length);
            return buffer.ToString();
        }

        private static void WriteAt(int x, int y, object value)
        {
            int prevX = Console.CursorLeft;
            int prevY = Console.CursorTop;

            Console.CursorLeft = x;
            Console.CursorTop = y;
            Console.Write(value);
            Console.CursorLeft = prevX;
            Console.CursorTop = prevY;
        }
    }
}