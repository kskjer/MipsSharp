using ELFSharp.ELF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MipsSharp.Binutils.Archive;
using ELFSharp.ELF.Sections;
using ELFSharp.ELF.Segments;

namespace MipsSharp
{
    public static class ArchiveFileExtensions
    {
        public static IELF ToElf(this IArchiveFile archive)
        {
            var fn = Path.GetTempFileName();
            File.WriteAllBytes(fn, archive.Data.ToArray());

            return new ElfProxy(
                () => File.Delete(fn),
                ELFReader.Load(fn)
            );
        }

        private class ElfProxy : IELF
        {
            private readonly Action _cleanupAction;
            private readonly IELF _inner;

            public ElfProxy(Action cleanup, IELF inner)
            {
                _cleanupAction = cleanup;
                _inner = inner;
            }

            public Class Class
            {
                get
                {
                    return _inner.Class;
                }
            }

            public Endianess Endianess
            {
                get
                {
                    return _inner.Endianess;
                }
            }

            public bool HasSectionHeader
            {
                get
                {
                    return _inner.HasSectionHeader;
                }
            }

            public bool HasSectionsStringTable
            {
                get
                {
                    return _inner.HasSectionsStringTable;
                }
            }

            public bool HasSegmentHeader
            {
                get
                {
                    return _inner.HasSegmentHeader;
                }
            }

            public Machine Machine
            {
                get
                {
                    return _inner.Machine;
                }
            }

            public IEnumerable<ISection> Sections
            {
                get
                {
                    return _inner.Sections;
                }
            }

            public IStringTable SectionsStringTable
            {
                get
                {
                    return _inner.SectionsStringTable;
                }
            }

            public IEnumerable<ISegment> Segments
            {
                get
                {
                    return _inner.Segments;
                }
            }

            public FileType Type
            {
                get
                {
                    return _inner.Type;
                }
            }

            private bool _disposed = false;

            public void Dispose()
            {
                if (_disposed)
                    return;

                _inner.Dispose();
                _cleanupAction();
                _disposed = true;
            }

            public ISection GetSection(int index)
            {
                return _inner.GetSection(index);
            }

            public ISection GetSection(string name)
            {
                return _inner.GetSection(name);
            }

            public IEnumerable<T> GetSections<T>() where T : ISection
            {
                return _inner.GetSections<T>();
            }

            public bool TryGetSection(int index, out ISection section)
            {
                return _inner.TryGetSection(index, out section);
            }

            public bool TryGetSection(string name, out ISection section)
            {
                return _inner.TryGetSection(name, out section);
            }
        }
    }
}
