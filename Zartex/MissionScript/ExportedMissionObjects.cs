using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using DSCript;
using DSCript.Spooling;

namespace Zartex
{
    public class ExportedMissionObjects : SpoolableResource<SpoolableBuffer>
    {
        public List<MissionObject> Objects { get; set; }

        public static Type GetObjectTypeById(int typeId)
        {
            switch (typeId)
            {
            case 1: return typeof(VehicleObject);
            case 2: return typeof(CharacterObject);
            case 3: return typeof(VolumeObject);
            case 4: return typeof(AreaObject);
            case 5: return typeof(PathObject);
            case 6: return typeof(ObjectiveIconObject);
            case 7: return typeof(CameraObject);
            case 8: return typeof(PropObject);
            case 9: return typeof(SwitchObject);
            case 10: return typeof(CollectableObject);
            case 11: return typeof(AnimPropObject);
            case 12: return typeof(MarkerObject);
            }

            return null;
        }

        public static string GetObjectNameById(int typeId)
        {
            switch (typeId)
            {
            case 1: return "Vehicle";
            case 2: return "Character";
            case 3: return "Volume";
            case 4: return "Area";
            case 5: return "Path";
            case 6: return "ObjectiveIcon";
            case 7: return "Camera";
            case 8: return "Prop";
            case 9: return "Switch";
            case 10: return "Collectable";
            case 11: return "AnimProp";
            case 12: return "Marker";
            }

            return typeId.ToString();
        }

        protected static MissionObject CreateObject(int typeId)
        {
            var type = GetObjectTypeById(typeId);

            if (type == null)
                throw new InvalidOperationException($"Couldn't create mission object -- invalid type '{typeId}'!");

            return Activator.CreateInstance(type) as MissionObject;
        }

        public MissionObject this[int index]
        {
            get { return Objects[index]; }
            set { Objects[index] = value; }
        }

        protected override void Load()
        {
            using (var f = Spooler.GetMemoryStream())
            {
                var count = f.ReadInt32();

                Objects = new List<MissionObject>(count);

                for (int i = 0; i < count; i++)
                {
                    var typeId = f.ReadInt32();

                    DSC.Log($"Mission object {i} @ {f.Position:X8} (type:{typeId})");

                    try
                    {
                        var obj = CreateObject(typeId);
                        obj.Load(f);

                        Objects.Add(obj);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"Failed to load mission object {i} / {count} of type {typeId}!", e);
                    }
                }
            }
        }

        protected override void Save()
        {
            var count = Objects?.Count ?? 0;

            using (var ms = new MemoryStream(4096))
            {
                ms.Write(count);

                if (Objects != null)
                {
                    foreach (var obj in Objects)
                        obj.Save(ms);
                }

                var size = (int)ms.Position;
                ms.SetLength(size);

                var buffer = ms.ToArray();

                Spooler.SetBuffer(buffer);
            }
        }
    }
}