using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Extractor.Octo.Proto;
using ProtoBuf;

namespace Extractor.Octo.Data
{
    public class SecureProtobufDatabase<T> : SecureSerializableDatabase<T> where T : class, new()
    {
        public SecureProtobufDatabase(string path, AesCrypt crypt) : base(path, crypt)
        {
        }

        protected override T Deserialize(byte[] bytes)
        {
            //using MemoryStream memoryStream = new(bytes);

            //var reader = ProtoReader.State.Create(memoryStream, null);

            //if (typeof(T) == typeof(Database))
            //    return ReadDatabase(reader);
            //else
            //    return null;


            ////FileStream fs = File.Create("to_deserialize");
            ////memoryStream.CopyTo(fs);
            ////memoryStream.Position = 0;
            ////return Serializer.Deserialize<T>(memoryStream);

            return null;
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

        private static Proto.Data ReadData(ProtoReader.State reader)
        {
            Proto.Data data = new();

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
                            data.DataState = (Proto.Data.State)reader.ReadInt32();
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
