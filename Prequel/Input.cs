namespace Prequel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// An input, either inline on the command line or from a file
    /// </summary>
    public class Input
    {
        public string Path { get; set; }

        private Stream stream;

        public Stream Stream
        {
            get
            {
                if (stream == null)
                {
                    stream = File.OpenRead(Path);
                }

                return stream;
            }

            private set
            {
                stream = value;
            }
        }

        public static Input FromString(string inlineInput)
        {
            return new Input() { Path = "<inline>", Stream = new MemoryStream(Encoding.UTF8.GetBytes(inlineInput)) }; // correct encoding? 
        }

        public static Input FromFile(string path)
        {
            return new Input() { Path = path }; // defer opening the file until we want to read it
        }
    }
}
