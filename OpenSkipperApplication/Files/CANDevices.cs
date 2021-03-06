﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using CANStreams;
using CANHandler;
using System.Reflection;

namespace CANDevices
{
    // Class for single CAN device description
    public class CANDevice
    {
        public string StreamName { get; set; }
        public int Source { get; set; }
        // Device information
        public UInt64 ID { get; set; }
        public UInt32 UniqueNumber { get; set; }
        public UInt32 DeviceFunction { get; set; }
        public UInt32 DeviceClass { get; set; }
        public UInt32 ManufacturerCode { get; set; }
        public int IndustryGroup { get; set; }
        // Product information
        private byte[] PiData=null;
        public string ModelSerialCode { get; set; }
        public UInt32 ProductCode { get; set; }
        public string ModelID { get; set; }
        public string SwCode { get; set; }
        public string ModelVersion { get; set; }
        public int LoadEquivalency { get; set; }
        public UInt16 N2kVersion { get; set; }
        public int CertificationLevel { get; set; }
        private byte[] CiData=null;
        // Configuration information
        public string InstallationDescription1 { get; set; }
        public string InstallationDescription2 { get; set; }
        public string Manufacturer { get; set; }

        public readonly object Lock = new object();

        public CANDevice(int source, string streamName)
        {
            Clear();
            Source = source;
            StreamName = streamName;
        }

        private void Clear()
        {
            ID = 0;
            Source = 0;
            DeviceFunction = 0;
            DeviceClass = 0;
            ModelSerialCode = "n/a";
            ModelID = "n/a";
            SwCode = "n/a";
            ModelVersion = "n/a";
            InstallationDescription1 = "n/a";
            InstallationDescription2 = "n/a";
            Manufacturer = "n/a";
        }

        public override string ToString()
        {
            string str = "";
            lock (Lock)
            {
                str = "Source=" + Source
                    // Device information
                    + "\nID=" + ID.ToString("X16")
                    + "\n-- Device information --"
                    + "\nUnique number=" + UniqueNumber
                    + "\nDevice function=" + DeviceClasses.FunctionAsString(DeviceClass,DeviceFunction)
                    + "\nDevice class=" + DeviceClasses.AsString(DeviceClass)
                    + "\nManufacturer code=" + ManufacturerCodeStr
                    + "\nIndustry group=" + IndustryGroup
                    // Product information
                    + "\n"
                    + "\n-- Product information --"
                    + "\nModel serial code=" + ModelSerialCode
                    + "\nProduct code=" + ProductCode
                    + "\nModel ID=" + ModelID
                    + "\nSw code=" + SwCode
                    + "\nModel version=" + ModelVersion
                    + "\nLoad equivalency=" + LoadEquivalency + " (" + LoadEquivalency*50+" mA)"
                    + "\nN2k version=" + N2kVersion
                    + "\nCertification level=" + CertificationLevel
                    // Configuration information
                    + "\n"
                    + "\n-- Configuration information --"
                    + "\nInstallation description1=" + InstallationDescription1
                    + "\nInstallation description2=" + InstallationDescription2
                    + "\nManufacturer=" + Manufacturer
                    ;
            }

            return str;
        }

        public string ManufacturerCodeStr { 
            get { 
                return Manufacturers.AsString(ManufacturerCode); 
            } 
        }
        private bool SetDeviceInformation(N2kFrame n2kFrame)
        {
            bool changed = false;

            lock (Lock)
            {
                UInt64 _ID = BitConverter.ToUInt64(n2kFrame.Data, 0);

                if (ID != _ID) 
                { 
                    ID = _ID;

                    // We pass here field description and use NMEA2000 definition
                    UInt32 UnicAndMCode = BitConverter.ToUInt32(n2kFrame.Data, 0);
                    UniqueNumber = UnicAndMCode & 0x1FFFFF;
                    ManufacturerCode = (UnicAndMCode >> 21) & 0x7ff;
                    DeviceFunction = n2kFrame.Data[5];
                    DeviceClass = (UInt32)((n2kFrame.Data[6] >> 1) & 0x7f);
                    IndustryGroup = (n2kFrame.Data[7] >> 4) & 0x7;

                    changed = true;
                }
            }

            return changed;
        }
        private bool SetProductInformation(N2kFrame n2kFrame)
        {
            bool changed = false;

            lock (Lock)
            {
                if (!EqualBytesLongUnrolled(PiData, n2kFrame.Data))
                {
                    // We pass here field description and use NMEA2000 definition
                    N2kVersion = BitConverter.ToUInt16(n2kFrame.Data, 0);
                    ProductCode = BitConverter.ToUInt16(n2kFrame.Data, 2);
                    ModelID = N2kASCIIField.ReadString(n2kFrame.Data, 4, 32);
                    SwCode = N2kASCIIField.ReadString(n2kFrame.Data, 36, 32);
                    ModelVersion = N2kASCIIField.ReadString(n2kFrame.Data, 68, 32);
                    ModelSerialCode = N2kASCIIField.ReadString(n2kFrame.Data, 100, 32);
                    CertificationLevel = n2kFrame.Data[132];
                    LoadEquivalency = n2kFrame.Data[133];
                    PiData = n2kFrame.Data.ToArray();

                    changed = true;
                }
            }

            return changed;
        }
        private bool SetConfigurationInformation(N2kFrame n2kFrame)
        {
            bool changed = false;

            lock (Lock)
            {
                if (!EqualBytesLongUnrolled(CiData, n2kFrame.Data))
                {
                    // We pass here field description and use NMEA2000 definition
                    int ByteOffset=0;
                    int ByteLength=0;
                    InstallationDescription1 = N2kStringField.ReadString(n2kFrame.Data, ByteOffset, ref ByteLength);
                    ByteOffset += ByteLength; ByteLength = 0;
                    InstallationDescription2 = N2kStringField.ReadString(n2kFrame.Data, ByteOffset, ref ByteLength);
                    ByteOffset += ByteLength; ByteLength = 0;
                    Manufacturer = N2kStringField.ReadString(n2kFrame.Data, ByteOffset, ref ByteLength);
                    CiData = n2kFrame.Data.ToArray();

                    changed = true;
                }
            }

            return changed;
        }
        public bool UpdateDevice(N2kFrame n2kFrame)
        {
            bool changed = false;

            switch (n2kFrame.Header.PGN)
            {
                case 60928:
                    changed=SetDeviceInformation(n2kFrame);
                    break;
                case 126996:
                    changed=SetProductInformation(n2kFrame);
                    break;
                case 126998:
                    changed = SetConfigurationInformation(n2kFrame);
                    break;
            }

            return changed;
        }

        // Copiend from http://stackoverflow.com/questions/43289/comparing-two-byte-arrays-in-net
        static unsafe bool EqualBytesLongUnrolled(byte[] data1, byte[] data2)
        {
            if (data1 == data2)
                return true;
            if (data1==null || data2==null)
                return false;
            if (data1.Length != data2.Length)
                return false;

            fixed (byte* bytes1 = data1, bytes2 = data2)
            {
                int len = data1.Length;
                int rem = len % (sizeof(long) * 16);
                long* b1 = (long*)bytes1;
                long* b2 = (long*)bytes2;
                long* e1 = (long*)(bytes1 + len - rem);

                while (b1 < e1)
                {
                    if (*(b1) != *(b2) || *(b1 + 1) != *(b2 + 1) ||
                        *(b1 + 2) != *(b2 + 2) || *(b1 + 3) != *(b2 + 3) ||
                        *(b1 + 4) != *(b2 + 4) || *(b1 + 5) != *(b2 + 5) ||
                        *(b1 + 6) != *(b2 + 6) || *(b1 + 7) != *(b2 + 7) ||
                        *(b1 + 8) != *(b2 + 8) || *(b1 + 9) != *(b2 + 9) ||
                        *(b1 + 10) != *(b2 + 10) || *(b1 + 11) != *(b2 + 11) ||
                        *(b1 + 12) != *(b2 + 12) || *(b1 + 13) != *(b2 + 13) ||
                        *(b1 + 14) != *(b2 + 14) || *(b1 + 15) != *(b2 + 15))
                        return false;
                    b1 += 16;
                    b2 += 16;
                }

                for (int i = 0; i < rem; i++)
                    if (data1[len - 1 - i] != data2[len - 1 - i])
                        return false;

                return true;
            }
        }
    }

    public static class CANDeviceList
    {
        private static Dictionary<int, CANDevice> _Devices;
        public static event Action DeviceListChange;
        public static event Action SourceChange; // Event caused by address claiming
        public static Dictionary<int, CANDevice> Devices { get { return _Devices; } }

        public static readonly object Lock = new object();

//        public static ManufacturerCollection Manufacturers;

        // Constructor
        static CANDeviceList()
        {
            _Devices = new Dictionary<int, CANDevice> { };
            StreamManager.NewStream += new Action<CANStreamer>(Connect);
//            Manufacturers=ManufacturerCollection.LoadFromFile("");
        }
        public static CANDevice FindDeviceByRule(string searchRule)
        {
            CANDevice dev=null;
            int Source;
            UInt64 ID; 

            try {
                if ( int.TryParse(searchRule, out Source) )
                {
                    _Devices.TryGetValue(Source, out dev);
                } 
                else 
                {
                    if ( searchRule.Substring(0, 3) == "ID:" )
                    {
                        ID = Convert.ToUInt64(searchRule.Substring(3), 16);
                        dev = FindDeviceByID(ID);
                    }
                }
            } catch
            {
                dev = null;
            }

            return dev;
        }

        private static int FindDeviceKeyByID(UInt64 ID)
        {
            int Key = -1;
            lock (Lock)
            {
                foreach (var item in _Devices)
                {
                    if (item.Value.ID == ID)
                    {
                        Key = item.Key;
                        break;
                    }
                }
            }

            return Key;
        }
        private static CANDevice FindDeviceByID(UInt64 ID)
        {
            CANDevice dev = null;
            lock (Lock)
            {
                int Key = FindDeviceKeyByID(ID);
                if (Key != -1) { dev = _Devices[Key]; }
            }

            return dev;
        }

        private static CANDevice UpdateAddDevice(N2kFrame n2kFrame, string streamName)
        {
            CANDevice dev=null;
            int Source;

            if (Int32.TryParse(n2kFrame.Header.Source, out Source))
            {
                lock (Lock)
                {
                    // First try to find device with source
                    _Devices.TryGetValue(Source, out dev);

                    if (dev != null) { // If we found device, just update it
                        UpdateDevice(dev, n2kFrame);
                    }
                    else
                    {
                        // If device does not exist, check is it on different source.
                        // Source may change due to address claiming.
                        if (n2kFrame.Header.PGN == 60928)
                        {
                            UInt64 ID = BitConverter.ToUInt64(n2kFrame.Data, 0);
                            int Key = FindDeviceKeyByID(ID);
                            if (Key != -1) // We found device, so register it with new key.
                            {
                                dev = _Devices[Key];
                                _Devices.Remove(Key);
                                _Devices.Remove(Source);
                                _Devices.Add(Source, dev);
                                lock (dev.Lock)
                                {
                                    dev.Source = Source;
                                }
                                // Notify that device address has been changed
                                var e = SourceChange; if (e != null) e();
                                e = DeviceListChange; if (e != null) e();
                            }
                            else
                            {
                                dev = AddDevice(n2kFrame, streamName);
                                var e = SourceChange; if (e != null) e();
                            }
                        }
                        else
                        {
                            // We did not find the device, but we can not add it, since message was
                            // not ISO Address claim PGN 60928
                        }
                    } // device was null
                }

            }
            return dev;
        }
        private static CANDevice AddDevice(N2kFrame n2kFrame, string streamName)
        {
            CANDevice dev = null;
            int Source;
            if (Int32.TryParse(n2kFrame.Header.Source, out Source))
            {
                dev = new CANDevice(Source, streamName);
                lock (Lock)
                {
                    dev.Source = Source;
                    _Devices.Add(Source, dev);
                }
                UpdateDevice(dev, n2kFrame);
            }

            return dev;
        }
        private static void UpdateDevice(CANDevice dev, N2kFrame n2kFrame)
        {
            var e = DeviceListChange;
            if (dev.UpdateDevice(n2kFrame) && e != null) e();
        }
        public static void OnDeviceDataReceieved(object sender, FrameReceivedEventArgs e)
        {
            N2kFrame n2kFrame = (N2kFrame)e.ReceivedFrame;
            CANStreamer stream = sender as CANStreamer;

            CANDevice dev = UpdateAddDevice(n2kFrame, stream.Name);
        }

        // Connects to the stream
        public static void Connect(CANStreamer stream)
        {
            stream.PGNReceived[60928] += OnDeviceDataReceieved;
            stream.PGNReceived[126996] += OnDeviceDataReceieved;
            stream.PGNReceived[126998] += OnDeviceDataReceieved;
        }

        public static void Initialize(){

        }

    }

    public static class Manufacturers {
        public static ManufacturerCollection _Manufacturers;

        static Manufacturers()
        {
            _Manufacturers = ManufacturerCollection.LoadFromFile();
        }

        public static string AsString(UInt32 key)
        {
            Manufacturer m;
            string str;
            if (_Manufacturers.ManufacturerByCode.TryGetValue(key, out m))
            {
                str = key.ToString() + " (" + m.Name + ")";
            }
            else
            {
                str = key.ToString() + " (unknown)";
            }
            return str;
        }

    }

    public class Manufacturer
    {
        public string Name { get; set; }
        public UInt32 Code { get; set; }
    }

    public class ManufacturerCollection
    {
        public const string DefDefnFile = "Manufacturers.N2kDfn.xml";
        private Manufacturer[] _Manufacturers;

        [XmlIgnore]
        // The file we loaded the definitions from
        public string FileName { get; set; }
        [XmlIgnore]
        public FileTypeEnum FileType { get; set; }
        [XmlIgnore]
        public Dictionary<UInt32, Manufacturer> ManufacturerByCode;

        public ManufacturerCollection()
        {
            ManufacturerByCode = new Dictionary<UInt32, Manufacturer> { };
            Manufacturers = new Manufacturer[0] { };
            FileName = "";
        }

        public Manufacturer[] Manufacturers
        {
            get
            {
                return _Manufacturers;
            }
            set
            {
                _Manufacturers = value ?? new Manufacturer[0];
                BuildInternalStructures();
            }
        }

        // Serialization
        private static XmlFileSerializer<ManufacturerCollection> XmlFileSerializer = new XmlFileSerializer<ManufacturerCollection>();

        // File IO
        public static ManufacturerCollection LoadFromFile()
        {
            string fileName = DefDefnFile;
            if (fileName == "") return LoadInternal();

            ManufacturerCollection manufCol = XmlFileSerializer.Deserialize(fileName);
            if (manufCol != null)
            {
                manufCol.FileName = fileName;
                manufCol.FileType = FileTypeEnum.NativeXMLFile;
                return manufCol;
            }

            return LoadInternal();
        }
        public static ManufacturerCollection LoadInternal()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            Stream stream = asm.GetManifestResourceStream("OpenSkipperApplication.Resources.Manufacturers.N2kDfn.xml");

            ManufacturerCollection newDefns = XmlFileSerializer.Deserialize(stream);
            newDefns.FileType = FileTypeEnum.Internal; // No filename set, instead we set type to internal
            return newDefns;
        }

        // Internal structs
        private void BuildInternalStructures()
        {
            ManufacturerByCode.Clear();
            foreach (Manufacturer m in Manufacturers)
            {
                if (!ManufacturerByCode.ContainsKey(m.Code))
                {
                    ManufacturerByCode[m.Code] = m;
                }
            }
        }
    }

    public static class DeviceClasses
    {
        public static DeviceClassCollection _DeviceClasses;

        static DeviceClasses()
        {
            _DeviceClasses = DeviceClassCollection.LoadFromFile();
        }

        public static string AsString(UInt32 classCode)
        {
            DeviceClass c;
            string str;

            if (_DeviceClasses.DeviceClassByCode.TryGetValue(classCode, out c))
            {
                str = c.ToString();
            }
            else
            {
                str = classCode.ToString() + " (unknown)";
            }
            return str;
        }

        public static string FunctionAsString(UInt32 classCode, UInt32 functionCode)
        {
            DeviceClass c;
            DeviceFunction f;
            string str;

            if (_DeviceClasses.DeviceClassByCode.TryGetValue(classCode, out c) &&
                c.DeviceFunctionByCode.TryGetValue(functionCode, out f) )
            {
                str = f.ToString();
            }
            else
            {
                str = functionCode.ToString() + " (unknown)";
            }
            return str;
        }

    }

    public class DeviceFunction
    {
        public string Name { get; set; }
        public UInt32 Code { get; set; }
        public override string ToString() { return Code + " (" + Name + ")"; }
    }

    public class DeviceClass
    {
        private DeviceFunction[] _DeviceFunctions;
        [XmlIgnore]
        public Dictionary<UInt32, DeviceFunction> DeviceFunctionByCode;

        public string Name { get; set; }
        public UInt32 Code { get; set; }
        public override string ToString() { return Code + " (" + Name + ")"; }

        public DeviceClass()
        {
            DeviceFunctionByCode = new Dictionary<UInt32, DeviceFunction> { };
            _DeviceFunctions = new DeviceFunction[0] { };
        }

        public DeviceFunction[] DeviceFunctions
        {
            get
            {
                return _DeviceFunctions;
            }
            set
            {
                _DeviceFunctions = value ?? new DeviceFunction[0];
                BuildInternalStructures();
            }
        }

        // Internal structs
        private void BuildInternalStructures()
        {
            DeviceFunctionByCode.Clear();
            foreach (DeviceFunction c in DeviceFunctions)
            {
                if (!DeviceFunctionByCode.ContainsKey(c.Code))
                {
                    DeviceFunctionByCode[c.Code] = c;
                }
            }
        }
    }

    public class DeviceClassCollection
    {
        public const string DefDefnFile = "ClassAndFunction.N2kDfn.xml";
        private DeviceClass[] _DeviceClasses;

        [XmlIgnore]
        // The file we loaded the definitions from
        public string FileName { get; set; }
        [XmlIgnore]
        public FileTypeEnum FileType { get; set; }
        [XmlIgnore]
        public Dictionary<UInt32, DeviceClass> DeviceClassByCode;

        public DeviceClassCollection()
        {
            DeviceClassByCode = new Dictionary<UInt32, DeviceClass> { };
            DeviceClasses = new DeviceClass[0] { };
            FileName = "";
        }

        public DeviceClass[] DeviceClasses
        {
            get
            {
                return _DeviceClasses;
            }
            set
            {
                _DeviceClasses = value ?? new DeviceClass[0];
                BuildInternalStructures();
            }
        }

        // Serialization
        private static XmlFileSerializer<DeviceClassCollection> XmlFileSerializer = new XmlFileSerializer<DeviceClassCollection>();

        // File IO
        public static DeviceClassCollection LoadFromFile()
        {
            string fileName = DefDefnFile;
            if (fileName == "") return LoadInternal();

            DeviceClassCollection classCol = XmlFileSerializer.Deserialize(fileName);
            if (classCol != null)
            {
                classCol.FileName = fileName;
                classCol.FileType = FileTypeEnum.NativeXMLFile;
                return classCol;
            }

            return LoadInternal();
        }
        public static DeviceClassCollection LoadInternal()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            Stream stream = asm.GetManifestResourceStream("OpenSkipperApplication.Resources.ClassAndFunction.N2kDfn.xml");

            DeviceClassCollection newDefns = XmlFileSerializer.Deserialize(stream);
            newDefns.FileType = FileTypeEnum.Internal; // No filename set, instead we set type to internal
            return newDefns;
        }

        // Internal structs
        private void BuildInternalStructures()
        {
            DeviceClassByCode.Clear();
            foreach (DeviceClass c in DeviceClasses)
            {
                if (!DeviceClassByCode.ContainsKey(c.Code))
                {
                    DeviceClassByCode[c.Code] = c;
                }
            }
        }
    }

}
