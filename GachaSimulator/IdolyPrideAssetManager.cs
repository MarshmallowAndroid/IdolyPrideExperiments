using AssetStudio;
using Fmod5Sharp;
using Fmod5Sharp.FmodTypes;
using GachaSimulator.Octo.Data;
using GachaSimulator.Octo.Proto;
using GachaSimulator.Solis.Common;
using NAudio.Vorbis;
using NAudio.Wave;
using ProtoBuf;
using System.Collections.Specialized;

namespace GachaSimulator
{
    internal class IdolyPrideAssetManager
    {
        private static readonly string aesKey = "zkfuuwgc4eoxlaew";
        private static readonly string unityVersion = "2020.3.18f1";

        private readonly Database? db;
        private readonly string filesDirectory;

        public IdolyPrideAssetManager()
        {
            filesDirectory = Directory.GetCurrentDirectory();
        }

        public IdolyPrideAssetManager(string octoDirectory)
        {
            string appIdDir = Directory.GetDirectories(Path.Combine(octoDirectory, "pdb")).Last();
            string versionDir = Directory.GetDirectories(appIdDir).Last();
            filesDirectory = Path.Combine(octoDirectory, "v1");

            if (appIdDir == "" || versionDir == "")
                throw new DirectoryNotFoundException("Cache directory not found.");

            string cachePath = Path.Combine(versionDir, "octocacheevai");

            AesCrypt crypt = new(aesKey);
            SecureFile sf = new(cachePath, crypt);
            MemoryStream dbStream = new(sf.Load());

            ProtoReader.State reader = ProtoReader.State.Create(dbStream, null);
            db = ReadDatabase(reader);

            if (db == null)
                throw new Exception("Failed to read database.");
        }

        public LoopWaveStream LoadSoundFragment(string name)
        {
            byte[] soundData = LoadSoundData(name, out MonoBehaviour? soundFragmentData);

            if (soundFragmentData == null)
                throw new Exception(nameof(soundFragmentData) + " was null.");

            OrderedDictionary monoBehaviourType = soundFragmentData.ToType();
            OrderedDictionary fragments = (OrderedDictionary)((List<object>)monoBehaviourType["_fragments"]!)[0]!;

            float startTime = (float)fragments["_startTime"]!;
            float endTime = (float)fragments["_endTime"]!;
            bool startFromInitial = (byte)fragments["_startFromInitial"]! > 0;

            LoopWaveStream ret = new(new VorbisWaveReader(new MemoryStream(soundData)), startTime, endTime) { Loop = true };
            if (!startFromInitial) ret.CurrentTime = TimeSpan.FromSeconds(startTime);

            if (ret == null)
                throw new Exception("Failed to create LoopWaveStream.");

            return ret;
        }

        public WaveStream LoadSound(string name)
        {
            byte[] soundData = LoadSoundData(name, out MonoBehaviour? _);
            return new WaveFileReader(new MemoryStream(soundData));
        }

        private byte[] LoadSoundData(string name, out MonoBehaviour? soundFragmentData)
        {
            string fileName;

            if (db != null)
                fileName = db.AssetBundleList.First(ab => ab.Name == name).Md5;
            else
                fileName = name;

            string filePath = Directory.GetFiles(filesDirectory, fileName, SearchOption.AllDirectories).FirstOrDefault() ?? "";

            if (filePath == "")
                throw new Exception(name + " not found.");

            FileReader fileReader = new(filePath, new MaskedHeaderStream(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), 256, name));
            BundleFile bundleFile = new(fileReader);

            SerializedFile? assetFile = null;
            BinaryReader? resourceFile = null;
            foreach (var file in bundleFile.fileList)
            {
                FileReader subFileReader = new(Path.Combine(Path.GetDirectoryName(fileReader.FullPath) ?? "", file.fileName), file.stream);
                if (subFileReader.FileType == FileType.AssetsFile)
                {
                    SerializedFile serializedFile = new(subFileReader, null);
                    serializedFile.SetVersion(unityVersion);
                    assetFile = serializedFile;
                }
                else if (subFileReader.FileType == FileType.ResourceFile)
                {
                    resourceFile = subFileReader;
                }
            }

            if (assetFile == null || resourceFile == null)
                throw new Exception("Failed to load asset.");

            soundFragmentData = null;
            AudioClip? audioClip = null;
            foreach (var assetObject in assetFile.m_Objects)
            {
                ObjectReader objectReader = new(assetFile.reader, assetFile, assetObject);

                if (objectReader.type == ClassIDType.MonoBehaviour)
                    soundFragmentData = new(objectReader);
                else if (objectReader.type == ClassIDType.AudioClip)
                    audioClip = new(objectReader);
            }

            if (audioClip == null)
                throw new Exception("Failed to find AudioClip data.");

            FmodSoundBank soundBank = FsbLoader.LoadFsbFromByteArray(resourceFile.ReadBytes((int)audioClip.m_Size));
            soundBank.Samples[0].RebuildAsStandardFileFormat(out byte[]? soundData, out string? extension);

            if (soundData == null)
                throw new Exception("Failed to load audio data.");

            return soundData;
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
    }
}
