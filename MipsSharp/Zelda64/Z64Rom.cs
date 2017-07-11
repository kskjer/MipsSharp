using MipsSharp.Nintendo64;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MipsSharp.Zelda64
{
    class Z64Rom
    {
        public string FilePath => _rom.FilePath;

        private readonly Rom _rom;

        public Z64Rom(Rom rom)
        {
            _rom = rom;

            var identity = (UInt64)_rom.Header.Crc[0] << 32 | _rom.Header.Crc[1];
            var useOffsets = true;

            if (!_tableOffsets.ContainsKey(identity))
                useOffsets = false;

            TableOffsets offsets;

            if( useOffsets)
                offsets = _tableOffsets[identity];
            else
            {
                offsets = new TableOffsets
                {
                    FileTable = FindFileTable(rom.Data)
                };

                if (offsets.FileTable == 0)
                    throw new ArgumentException("File table not found. Is this a valid Zelda 64 ROM?");
            }

            var fileTablePointers = _rom.GetSegment(offsets.FileTable)
                .ToWordGroups(4)
                .TakeWhile(g => !g.All(w => w == 0))
                .ToList();


            var nameTable =
                useOffsets
                ? _rom.GetSegment(offsets.NameTable)
                .ExtractStrings(fileTablePointers.Count)
                .ToList()
                : new List<string>();

            Files = fileTablePointers
                .Where(f => f[3] != 0xFFFFFFFF && f[2] != 0xFFFFFFFF)
                .Select((f, i) => new File(
                    f[0], f[1], f[2], f[3], 
                    nameTable.Count > i ? nameTable[i] : null, 
                    null, 
                    _rom
                ))
                .ToList();

            CodeFile = Files.Cast<File>().FirstOrDefault(f => f.ContainsString("Yoshitaka Yasumoto"));

            IEnumerable<uint[]> overlaySource;

            if (useOffsets)
            {
                overlaySource = offsets.OverlayTables
                    .SelectMany(o => _rom.GetSegment(o.StartAddress, o.RecordCount * o.RecordSize)
                        .ToWordGroups(o.RecordSize / sizeof(UInt32))
                    );
            }
            else
            {
                var ovls = Files.Where(f => f.Type == FileType.Overlay)
                    .Select(o => (UInt64)o.VirtualStart << 32 | o.VirtualEnd)
                    .ToList();

                overlaySource = CodeFile.FindTuples(ovls)
                    .SelectMany(t => Utilities.ReadU32(CodeFile.Contents, t.Value + 12) >= 0x80800000
                    ?
                    CodeFile.Contents.GetSegment(t.Value, 32)
                    .ToWordGroups(7)
                    :
                    // Some overlays in Majora's Mask have the vma start and end addreses
                    // before the virtual file address pair. Here, we compensate for this
                    // by checking if the vma end is < 0x80800000 (overlay address space).
                    // If it is, we prepend the preceding 8 bytes with the rest of the record.
                    CodeFile.Contents.GetSegment(t.Value - 8, 8)
                    .Concat(CodeFile.Contents.GetSegment(t.Value, 24))
                    .ToWordGroups(7));
            }

            Overlays = overlaySource
                //_rom.GetSegment(offsets.EffectOverlayTable, (int)offsets.EffectOverlayCount * (int)OverlayEntry.EffectRecordSize)
                .Select(r => OverlayEntry.FromWords(r, Files.FirstOrDefault(f => f.VirtualStart == r[0])))
                .Where(r => r.VmaStart >= 0x80800000 && r.VmaEnd != 0)
                .OrderBy(o => o.VmaStart)
                .ToList();
        }

        // 00000000 00001060 00000000 00000000 00001060
        /// <summary>
        /// Searches the provided byte array for the Zelda 64 file table.
        /// </summary>
        /// <param name="romData"></param>
        /// <returns></returns>
        public static int FindFileTable(IReadOnlyList<byte> romData)
        {
            for(var i = 0; i <= romData.Count - 16; i += 16)
            {
                if(Utilities.ReadU64(romData, i)      == 0x1060 && 
                   Utilities.ReadU64(romData, i + 8)  == 0      && 
                   Utilities.ReadU32(romData, i + 16) == 0x1060)
                {
                    return i;
                }
            }

            return 0;
        }

        public IZ64File CodeFile { get; }
        public IReadOnlyList<IZ64File> Files { get; }
        public IReadOnlyList<IOverlayEntry> Overlays { get; }

        public interface IZ64File
        {
            FileType Type { get; }
            string Name { get; }
            UInt32 VirtualStart { get; }
            UInt32 VirtualEnd { get; }
            UInt32 PhysicalStart { get; }
            UInt32 PhysicalEnd { get; }
            UInt32 Size { get; }
            UInt32 PhysicalSize { get; }
            bool IsCompressed { get; }
            IReadOnlyList<byte> Contents { get; }
            /// <summary>
            /// If compressed, provides the data in compressed form.
            /// </summary>
            IReadOnlyList<byte> RawContents { get; }
            bool ContainsString(string search);
            int FindDword(UInt64 value);
            IReadOnlyDictionary<UInt64, int> FindTuples(IReadOnlyList<UInt64> tuples);
        }


        public interface IOverlayEntry
        {
            UInt32 VirtualEnd { get; }
            UInt32 VirtualStart { get; }
            UInt32 VmaEnd { get; }
            UInt32 VmaStart { get; }
            IZ64File File { get; }
        }

        private class OverlayEntry : IOverlayEntry
        {
            public const uint EffectRecordSize = 28;
            public const uint MainRecordSize = 0x30;

            // Overlay entries are 28 bytes each
            public UInt32 VirtualStart { get; private set; }
            public UInt32 VirtualEnd { get; private set; }
            public UInt32 VmaStart { get; private set; }
            public UInt32 VmaEnd { get; private set; }

            // Always zero u32
            public UInt32 Unknown1 { get; private set; }

            public UInt32 PointerInsideOverlay { get; private set; }

            public UInt32 Unknown2 { get; private set; }

            public UInt32 VmaSize => VmaEnd - VmaStart;

            public IZ64File File { get; private set; }

            private OverlayEntry() { }

            public static OverlayEntry FromWords(UInt32[] words, IZ64File file)
            {
                return new OverlayEntry
                { 
                    VirtualStart         = words[0],
                    VirtualEnd           = words[1],
                    VmaStart             = words[2],
                    VmaEnd               = words[3],
                    Unknown1             = words[4],
                    PointerInsideOverlay = words[5],
                    Unknown2             = words[6],
                    File                 = file
                };
            }

            public override string ToString() =>
                string.Format("{0:X8} - {1:X8} {2} ({3:0.00} kB)", VmaStart, VmaEnd, File?.Name ?? "", VmaSize / 1024.0f);
        }

        private class File : IZ64File
        {
            public static FileType DetectFromName(string name) =>
                name.StartsWith("ovl_")    ? FileType.Overlay :
                name.StartsWith("object_") ? FileType.Object  :
                name.EndsWith("_scene")    ? FileType.Scene   :
                name.Contains("_room_")    ? FileType.Room    : FileType.Unknown;

            public uint PhysicalSize =>
                IsCompressed
                ? PhysicalEnd - PhysicalStart
                : Size;

            public uint Size => 
                VirtualEnd - VirtualStart;

            public bool IsCompressed =>
                PhysicalEnd != 0;

            public string Name { get; }
            public FileType Type { get; }

            public uint PhysicalEnd { get; }
            public uint PhysicalStart { get; }

            public uint VirtualEnd { get; }
            public uint VirtualStart { get; }

            public IReadOnlyList<byte> Contents =>
                !IsCompressed
                ? RawContents
                : (_decompressed ?? (_decompressed = Yaz0.Decompress(_rawContents)));

            public IReadOnlyList<byte> RawContents => 
                _rawContents;

            private readonly IReadOnlyList<byte> _rawContents;
            private IReadOnlyList<byte> _decompressed;


            private static bool ArraySequenceEquals(IReadOnlyList<byte> input, int offset, IReadOnlyList<byte> search)
            {
                for (var i = 0; i < search.Count && i + offset < input.Count; i++)
                    if (input[offset + i] != search[i])
                        return false;

                return true;
            }

            public bool ContainsString(string search)
            {
                var bytes = Encoding.ASCII.GetBytes(search);

                for (var i = 0; i < Contents.Count; i++)
                    if (ArraySequenceEquals(Contents, i, bytes))
                        return true;

                return false;
            }

            public IReadOnlyDictionary<UInt64, int> FindTuples(IReadOnlyList<UInt64> tuples)
            {
                var lookup = tuples.ToDictionary(t => t, t => 0);
                var found = 0;

                for(var i = 0; i <= Contents.Count - 8 && found < tuples.Count; i += 4)
                {
                    UInt64 wrd;

                    if (lookup.ContainsKey(wrd = Utilities.ReadU64(Contents, i)))
                    {
                        lookup[wrd] = i;
                        found++;
                    }
                }

                return lookup;
            }

            public int FindDword(UInt64 value)
            {
                for (var i = 0; i <= Contents.Count - 8; i += 4)
                    if (Utilities.ReadU64(Contents, i) == value)
                        return i;

                return -1;
            }


            public File(uint vstart, uint vend, uint pstart, uint pend, string name, FileType? type, Rom rom)
            {
                VirtualStart = vstart;
                VirtualEnd = vend;
                PhysicalStart = pstart;
                PhysicalEnd = pend;
                Name = name;

                _rawContents = rom.GetSegment((int)PhysicalStart, (int)PhysicalSize);

                Type = type ?? (name != null ? DetectFromName(name) : DetectByContent());
            }

            private FileType DetectByContent()
            {
                return Overlay.Detect(Contents) ? FileType.Overlay : FileType.Unknown;
            }

            public override string ToString() =>
                string.Format("{0:X8} - {1:X8} {2} ({3:0.00} kB)", VirtualStart, VirtualEnd, Name, Size / 1024.0);
        }

        private class TableOffsets
        {
            public int FileTable { get; set; }
            public int NameTable { get; set; }
            public IEnumerable<OverlayListEntry> OverlayTables { get; set; }
        }

        private class OverlayListEntry
        {
            public int StartAddress { get; set; }
            public int RecordSize { get; set; }
            public int RecordCount { get; set; }
        }

        private readonly static IReadOnlyDictionary<UInt64, TableOffsets> _tableOffsets =
            new Dictionary<ulong, TableOffsets>()
            {
                // Master Quest debug ROM
                {
                    0x917D18F669BC5453,
                    new TableOffsets
                    {
                        FileTable = 0x12f70,
                        NameTable = 0xbe80,
                        OverlayTables = new []
                        {
                            new OverlayListEntry { StartAddress = 0xb8cb50, RecordCount = 37, RecordSize = 28 },
                            new OverlayListEntry { StartAddress = 0xb96a04, RecordCount = 5, RecordSize = 0x30 },
                            new OverlayListEntry { StartAddress = 0xba4344, RecordCount = 2, RecordSize = 28 },
                            new OverlayListEntry { StartAddress = 0xb9729c, RecordCount = 1, RecordSize = 28 },
                            new OverlayListEntry { StartAddress = 0xb8d480, RecordCount = 469, RecordSize = 32 } // b8d480 - b90f20 }
                        }
                    }
                }
            };
    }
}
