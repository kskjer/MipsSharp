using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MipsSharp.Binutils
{
    public class Archive
    {
        private readonly byte[] _data;
        private readonly byte[] _magic = Encoding.ASCII.GetBytes("!<arch>\n");

        private string ReadString(IReadOnlyList<byte> input, int offset, int size)
        {
            var buffer = new byte[size];

            for (var i = 0; i < size; i++)
                buffer[i] = input[offset + i];

            return Encoding.ASCII.GetString(buffer);
        }

        private class Symbol
        {
            public string Name { get; set; }
            public int Offset { get; set; }
        }

        private IReadOnlyDictionary<string, int> LoadSymbols(IReadOnlyList<byte> contents)
        {
            var numSymbols = (int)Utilities.ReadU32(contents, 0);
            var offsets = Enumerable.Range(0, numSymbols)
                .Select(i => (int)Utilities.ReadU32(contents, 4 + i * 4));
            var strings = contents.Skip(4 + numSymbols * 4).GetGroupsOfBytesSeparatedBy(0)
                .Select(g => Encoding.ASCII.GetString(g.ToArray()))
                .ToList();

            return strings
                .Zip(offsets, (a, b) => new { a, b })
                .ToDictionary(a => a.a, a => a.b);
        }

        private IReadOnlyDictionary<int, string> ReadFilenames(IReadOnlyList<byte> contents)
        {
            var offset = 0;

            return contents
                .GetGroupsOfBytesSeparatedBy(0x0a)
                .Select(g =>
                {
                    var rval = new { str = Encoding.ASCII.GetString(g.ToArray()).TrimEnd('/'), offset };

                    offset += g.Count + 1;

                    return rval;
                })
                .Where(g => !string.IsNullOrWhiteSpace(g.str))
                .ToDictionary(a => a.offset, a => a.str);
        }

        private IEnumerable<IArchiveFile> GetFiles(IReadOnlyList<byte> input, int startPos)
        {
            var pos = startPos;
            IReadOnlyDictionary<string, int> syms = null;
            IReadOnlyDictionary<int, string> filenames = null;

            do
            {
                var identifier = ReadString(input, pos, 16);
                pos += identifier.Length;
                var mtime = ReadString(input, pos, 12);
                pos += mtime.Length;
                var ownerId = ReadString(input, pos, 6);
                pos += ownerId.Length;
                var groupId = ReadString(input, pos, 6);
                pos += groupId.Length;
                var fileMode = ReadString(input, pos, 8);
                pos += fileMode.Length;
                var fileSize = ReadString(input, pos, 10);
                pos += fileSize.Length;
                var endingChars = ReadString(input, pos, 2);
                pos += endingChars.Length;

                identifier = identifier.Trim();
                mtime = mtime.Trim();
                ownerId = ownerId.Trim();
                groupId = groupId.Trim();
                fileMode = fileMode.Trim();
                fileSize = fileSize.Trim();

                var contents = input.GetSegment(pos, int.Parse(fileSize));
                pos += int.Parse(fileSize);

                // Symbol table
                if( identifier == "/" )
                {
                    syms = LoadSymbols(contents);
                }
                // File table
                else if( identifier == "//" )
                {
                    filenames = ReadFilenames(contents);
                }
                else
                {
                    // Check for long filename
                    try
                    {
                        if (identifier.StartsWith("/"))
                            identifier = filenames[int.Parse(identifier.Substring(1))];
                    }
                    catch
                    {
                        Console.Error.WriteLine("Warning, couldn't find filename {0}", identifier = identifier.Substring(1));
                    }

                    yield return new ArchiveFile
                    {
                        Data = contents,
                        FileMode = int.Parse(fileMode),
                        FileSize = int.Parse(fileSize),
                        Filename = identifier.TrimEnd('/'),
                        GroupId = int.Parse(groupId),
                        OwnerId = int.Parse(ownerId),
                        Timestamp = int.Parse(mtime)
                    };

                    if (pos % 2 > 0)
                        pos++;
                }
            }
            while (pos < input.Count - startPos);
        }

        public Archive(string path)
        {
            _data = File.ReadAllBytes(path);

            var magicOk = _data
                .Take(_magic.Length)
                .SequenceEqual(_magic);

            if (!magicOk)
                throw new ArgumentException("Not an AR archive");

            Files = GetFiles(_data, _magic.Length).ToList();
        }

        public IReadOnlyList<IArchiveFile> Files { get; }

        private class ArchiveFile : IArchiveFile
        {
            public string Filename { get; set; }
            public int Timestamp { get; set; }
            public int OwnerId { get; set; }
            public int GroupId { get; set; }
            public int FileMode { get; set; }
            public int FileSize { get; set; }
            public IReadOnlyList<byte> Data { get; set; }

            public override string ToString() =>
                string.Format("{0} ({1:0.00} kB, {2})", Filename, FileSize / 1024.0, Utilities.UnixTimeStampToDateTime(Timestamp).ToString("d"));
        }


        public interface IArchiveFile
        {
            IReadOnlyList<byte> Data { get; }
            int FileMode { get; }
            string Filename { get; }
            int FileSize { get; }
            int GroupId { get; }
            int OwnerId { get; }
            int Timestamp { get; }
        }
    }
}
