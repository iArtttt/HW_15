﻿using System.Diagnostics;

namespace HW_15
{
    class Generator
    {
        private readonly string _path;
        private readonly long _size;

        private readonly Random _random = new Random();

        public Generator(string path, long size = 1L* 1024  * 1024 * 1024)
        {
            _path = path;
            _size = size;
        }

        public async Task Do()
        {
            var bufferSize = (int)(_size * 0.1);
            var task = Task.CompletedTask;
            var buffers = GetBuffers(bufferSize);

            await using var stream = File.OpenWrite(_path);

            var progress = _size;
            while (progress > 0)
            {
                buffers.MoveNext();
                var buffer = buffers.Current;

                _random.NextBytes(buffer);

                await task;
                task = stream.WriteAsync(buffer, 0, buffer.Length);

                progress -= bufferSize;
            }
            await task;
        }

        private IEnumerator<byte[]> GetBuffers(int length)
        {
            var buffer1 = new byte[length];
            var buffer2 = new byte[length];

            while (true)
            {
                yield return buffer1;
                yield return buffer2;
            }
        }
    }

    class Parser
    {
        private readonly string _path;

        public Parser(string path)
        {
            _path = path;
        }

        public async Task Parse()
        {
            const int size = 512 * 1024 * 1024;

            var buffer = new byte[size];
            var bufferTemp = new byte[size];
            var bufferResult = new byte[size];

            var readTask = Task.CompletedTask;
            var writeTask = Task.CompletedTask;

            await using var stream = File.OpenRead(_path);
            await using var resultStream = File.OpenWrite("../../../../result.bin");

            Stopwatch stopwatch = Stopwatch.StartNew();
            
            if (stream.Position < stream.Length)
            {
                readTask = stream.ReadAsync(buffer, 0, buffer.Length);
                var count = 0;


                while (stream.Position < stream.Length)
                {
                    await readTask;
                    
                    count = bufferTemp[0] == default ? buffer.Count() : bufferTemp.Count();
               
                    readTask = buffer[0] == default ? 
                        stream.ReadAsync(buffer, 0, buffer.Length) : 
                        stream.ReadAsync(bufferTemp, 0, bufferTemp.Length);

                    int resultCount = 0;
                    
                    if (buffer != null)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            var ch = (char)buffer[i];
                            if (char.IsAscii(ch))
                            {
                                bufferResult[i] = buffer[i];
                                resultCount++;
                            }
                        }
                        buffer[0] = default;
                    }
                    else
                    {
                        for (int i = 0; i < count; i++)
                        {
                            var ch = (char)bufferTemp[i];
                            if (char.IsAscii(ch))
                            {
                                bufferResult[i] = bufferTemp[i];
                                resultCount++;
                            }
                        }
                        bufferTemp[0] = default;
                    }


                    await writeTask;
                    writeTask = resultStream.WriteAsync(bufferResult, 0, resultCount);
                }
                await writeTask;
                stopwatch.Stop();
                Console.WriteLine(stopwatch.Elapsed);
            }
        }
    }
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var gen = new Generator("../../../../data.bin");
            var genTask = gen.Do();
            var parser = new Parser("../../../../data.bin");
            
            await genTask;
            await parser.Parse();
            // ===============================
            
            //foreach (var line in await File.ReadAllLinesAsync("../../../../result.bin"))
            //{
            //    await Console.Out.WriteLineAsync(line);
            //}
            await foreach (var line in File.ReadLinesAsync("../../../../result.bin"))
            {
                await Console.Out.WriteLineAsync(line);
            }
        }
    }
}