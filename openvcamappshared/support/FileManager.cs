using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace openvcamapp.shared.support
{
    public class FileManager
    {
        public string FileName { get; set; }        
        public string TempFolder { get; set; }
        public int FileMaxSizeMb { get; set; }
        public List<string> FileChunks { get; set; }

        public FileManager()
        {
            FileChunks = new List<string>();
        }

        public bool DivideFile()
        {
            bool result = false;            

            int file_chunk_size = FileMaxSizeMb * (1024 * 1024);

            const int BUFFER_READ_SIZE = 1024;
            byte[] buffer_stream_file = new byte[BUFFER_READ_SIZE];

            using (FileStream stream_file = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int file_chunks_total = 0;
                if (stream_file.Length < file_chunk_size)
                {
                    file_chunks_total = 1;
                }
                else
                {
                    float file_chunck_precision = ((float)stream_file.Length / (float)file_chunk_size);
                    file_chunks_total = (int)Math.Ceiling(file_chunck_precision);
                }

                int file_chunks_quantity = 0;

                while (stream_file.Position < stream_file.Length)
                {
                    string file_chunck_name = String.Format("{0}.part_{1}.{2}", Path.GetFileName(FileName), (file_chunks_quantity + 1).ToString(), file_chunks_total.ToString());
                    file_chunck_name = Path.Combine(TempFolder, file_chunck_name);
                    FileChunks.Add(file_chunck_name);
                    using (FileStream file_chunk = new FileStream(file_chunck_name, FileMode.Create))
                    {
                        int rest_bytes = file_chunk_size;
                        int read_bytes = 0;
                        while (rest_bytes > 0 && (read_bytes = stream_file.Read(buffer_stream_file, 0, Math.Min(rest_bytes, BUFFER_READ_SIZE))) > 0)
                        {
                            file_chunk.Write(buffer_stream_file, 0, read_bytes);
                            rest_bytes -= read_bytes;
                        }
                    }

                    file_chunks_quantity++;
                }

            }
            return result;
        }
    }
}
