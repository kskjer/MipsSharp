using ELFSharp.ELF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ELFSharp.ELF.Sections;
using ELFSharp.ELF.Segments;
using MipsSharp.Extensions;

namespace MipsSharp.Binutils
{
    public class Elf
    {
        /// <summary>
        /// Wrapper IELF object that is based off a temporary file which
        /// is deleted when the object is disposed.
        /// </summary>
        private class ElfWrapper : IELF
        {
            private readonly IELF _inner;
            private readonly string _tempFilePath;

            public ElfWrapper(string tempFilePath)
            {
                _tempFilePath = tempFilePath;
                _inner = ELFReader.Load(_tempFilePath);
            }

            public void Dispose()
            {
                _inner.Dispose();

                try
                {
                    File.Delete(_tempFilePath);
                }
                catch { }
            }

            public Endianess Endianess => _inner.Endianess;
            public Class Class => _inner.Class;
            public FileType Type => _inner.Type;
            public Machine Machine => _inner.Machine;
            public bool HasSegmentHeader => _inner.HasSegmentHeader;
            public bool HasSectionHeader => _inner.HasSectionHeader;
            public bool HasSectionsStringTable => _inner.HasSectionsStringTable;
            public IEnumerable<ISegment> Segments => _inner.Segments;
            public IStringTable SectionsStringTable => _inner.SectionsStringTable;
            public IEnumerable<ISection> Sections => _inner.Sections;

            public ISection GetSection(string name)
            {
                return _inner.GetSection(name);
            }

            public ISection GetSection(int index)
            {
                return _inner.GetSection(index);
            }

            public IEnumerable<T> GetSections<T>() where T : ISection
            {
                return _inner.GetSections<T>();
            }

            public bool TryGetSection(string name, out ISection section)
            {
                return _inner.TryGetSection(name, out section);
            }

            public bool TryGetSection(int index, out ISection section)
            {
                return _inner.TryGetSection(index, out section);
            }
        }

        public static IELF LoadFromBytes(IReadOnlyList<byte> fileContents)
        {
            var tempPath = Path.GetTempFileName();

            File.WriteAllBytes(tempPath, fileContents.ToArray());

            return new ElfWrapper(tempPath);
        }
    }
}
