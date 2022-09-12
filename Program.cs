// Виноградов Сергей Васильевич, 1984, Мытищи
// https://github.com/VinnySmallUtilities/sdel
// 2022 год


using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace sdel
{
    class MainClass
    {
        class Progress
        {
            public long rewritedSize     = 0;
            public long SizeToRewrite    = 0;

            public long rewritedCnt      = 0;
            public long cntToRewrite     = 0;

            public int  showProgressFlag = 0;
            public int  slowDownFlag     = 0;

            public DateTime lastMessage = DateTime.MinValue;


            /// <summary>Создаёт объект, отображающий прогресс выполнения</summary>
            /// <param name="SizeToRewrite">Общий размер файлов для перезаписи</param>
            /// <param name="cntToRewrite">Количество файлов для перезаписи</param>
            /// <param name="showProgressFlag">Флаг прогресса. 1 - отображать прогресс выполнения в консоли</param>
            public Progress(long SizeToRewrite = 0, long cntToRewrite = 0, int showProgressFlag = 1)
            {
                this.SizeToRewrite    = SizeToRewrite;
                this.cntToRewrite     = cntToRewrite;
                this.showProgressFlag = showProgressFlag;
            }

            public float progress
            {
                get
                {
                    var cntProgress  = rewritedCnt  / (float) cntToRewrite;
                    var sizeProgress = rewritedSize / (float) SizeToRewrite;

                    return (cntProgress + sizeProgress) * 50f;
                }
            }

            public string progressStr => progress.ToString("F2") + "%    ";

            public uint IntervalToMessageInSeconds = 1;
            public void showMessage()
            {
                if (showProgressFlag == 0)
                    return;

                var now = DateTime.Now;
                if ((now - lastMessage).TotalSeconds < IntervalToMessageInSeconds)
                    return;

                lastMessage = now;

                Console.CursorLeft = 0;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(progressStr);
                Console.ResetColor();
            }
        }
        
        // const int BufferSize = 16 * 1024 * 1024;
        const int BufferSize = 16 * 1024 * 1024;

        public static int Main(string[] args)
        {
            #if DEBUG
            // args = new string[] { "v", "/home/g2/g2/.wine/" };
            // args = new string[] { "z3", "/inRam/1.txt" };
            // args = new string[] { "vvslpr", "/inRam/1/" };
            // args = new string[] { "-", "/inRam/Renpy/" };
            // args = new string[] { "vvslpr", "/Arcs/toErase" };
            #endif

            if (args.Length < 2)
            {
                Console.Error.WriteLine("sdel dir");
                Console.WriteLine("Examples:");
                Console.WriteLine("sdel - /home/user/.wine");
                Console.WriteLine("sdel v /home/user/.wine");
                Console.WriteLine("flag 'v' switches to verbosive mode");
                Console.WriteLine("flag 'vv' switches to twice verbosive mode");
                Console.WriteLine("flag 'z' switches to 0x00 pattern");
                Console.WriteLine("flag 'z2' switches to twice rewriting. 0x55AA and 0x00 patterns");
                Console.WriteLine("flag 'z3' switches to three rewriting. 0xCCCC, 0x6666, 0x00 patterns");
                Console.WriteLine("flag 'pr' show progress");
                Console.WriteLine("flag 'sl' get slow down progress (small disk usage)");
                Console.WriteLine("Example:");
                Console.WriteLine("sdel vvz2pr /home/user/.wine");
                return 101;
            }


            var path  = args[1];
            var flags = args[0];

            var verbose = flags.Contains("v") ? 1 : 0;
            if (verbose > 0)
            {
                Console.WriteLine("Verbosive mode");
            }
            if (flags.Contains("vv"))
            {
                Console.WriteLine("Verbosive mode twiced");
                verbose = 2;
            }

            Progress progress = new Progress(showProgressFlag: 0);

            var zFlag = flags.Contains("z") ? 1 : 0;
            if (flags.Contains("z2"))
                zFlag = 2;
            else
            if (flags.Contains("z3"))
                zFlag = 3;

            var isDirectory = false;
            if (Directory.Exists(path))
            {
                isDirectory = true;
                Console.WriteLine($"Directory to deletion: \"{path}\"");
            }
            else
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"File not exists:\n\"{path}\"");
                return 102;
            }
            else
            {
                Console.WriteLine($"File to deletion: \"{path}\"");
            }

            List<byte[]> bt = new List<byte[]>(4);

            if (zFlag > 1)
            {
                if (zFlag >= 3)
                {
                    ArrayInitialization(bt, 0xCC);      // Это первое перезатирание при тройном перезатирании
                }

                ArrayInitialization(bt, 0x66);
            }

            var bt1 = new byte[BufferSize];
            var A   = new byte[] { 0x55, 0xAA };
            for (int i = 0; i < bt1.Length; i++)
            {
                if (zFlag == 0)
                    bt1[i] = A[i & 1];
                else
                    bt1[i] = 0;  // Флаг z установлен - последнее перезатирание - 0
            }
            bt.Add(bt1);

            if (!isDirectory)
            {
                var fi = new FileInfo(path);
                if (flags.Contains("pr"))
                {
                    progress = new Progress(SizeToRewrite: fi.Length, cntToRewrite: 1);
                }

                if (flags.Contains("sl"))
                    progress.slowDownFlag = 1;

                deleteFile(fi, bt, true, true, progress: progress, verbose: verbose);

                if (File.Exists(path))
                    Console.WriteLine($"Program ended. Deletion failed for file {path}");
                else
                    Console.WriteLine($"Program ended. Deletion successfull ended for file {path}");

                return 0;
            }

            if (verbose >= 1)
            {
                Console.WriteLine("Prepare a list of files to data sanitization");
            }

            var di   = new DirectoryInfo(path);
            var list = di.EnumerateFiles("*", SearchOption.AllDirectories);

            if (flags.Contains("pr"))
            {
                progress    = new Progress();
                foreach (var file in list)
                {
                    progress.cntToRewrite  += 1;
                    progress.SizeToRewrite += file.Length;
                }

                if (zFlag >= 2)
                    progress.SizeToRewrite *= zFlag;
                if (zFlag >= 4)
                    throw new NotImplementedException();

                var dirList = di.EnumerateDirectories("*", SearchOption.AllDirectories);
                foreach (var file in list)
                {
                    progress.cntToRewrite  += 1;
                }
            }

            if (flags.Contains("sl"))
                progress.slowDownFlag = 1;

            foreach (var file in list)
            {
                deleteFile(file, bt, true, progress: progress, verbose: verbose);
            }

            di.Refresh();
            var checkList = di.GetFiles();
            if (checkList.Length > 0)
            {
                Console.Error.WriteLine("Files is not deleted. Some files wich not has been deleted:");
                var cnt = 0;
                foreach (var fileInfo in checkList)
                {
                    Console.Error.WriteLine(fileInfo);
                    cnt++;
                    if (cnt > 16)
                        break;
                }

                Console.Error.WriteLine();
                Console.Error.WriteLine("Files is not deleted");
                return 11;
            }

            deleteDir(di, progress: progress, verbose: verbose);
            
            di.Refresh();
            if (di.Exists)
            {
                Console.WriteLine("Deletion failed");
                return 12;
            }

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine($"Program ended. Deletion successfull ended for directory {path}");
            Console.WriteLine();

            return 0;
        }

        /// <summary>Инициализирует массив одним и тем же значением на весь массив</summary>
        /// <param name="bt">Список для добавления массива</param>
        /// <param name="pattern">Шаблон заполнения</param>
        private static void ArrayInitialization(List<byte[]> bt, byte pattern)
        {
            var bt0 = new byte[16 * 1024 * 1024];
            for (int i = 0; i < bt0.Length; i++)
            {
                bt0[i] = pattern;
            }
            bt.Add(bt0);
        }

        private static void deleteDir(DirectoryInfo dir, Progress progress = null, int verbose = 0)
        {
            var oldDirName = dir.FullName;

            if (verbose > 0)
            Console.WriteLine($"Try to delete directory \"{dir.FullName}\"");

            var newFileName = dir.FullName;

            var fn = dir.Name;
            var sb = new StringBuilder(fn.Length);
            for (char ci = ' '; ci <= 'z'; ci++)
            {
                if (ci == '*' || ci == '?' || ci == Path.DirectorySeparatorChar || ci == Path.AltDirectorySeparatorChar)
                    continue;

                sb.Clear();
                sb.Append(ci, fn.Length);
                fn = sb.ToString();
                sb.Clear();

                newFileName = Path.Combine(dir.Parent.FullName, fn);
                if (File.Exists(newFileName) || Directory.Exists(newFileName))
                {
                    continue;
                }

                dir.MoveTo(newFileName);
                break;
            }

            var dirList = dir.GetDirectories();
            foreach (var di in dirList)
            {
                deleteDir(di, verbose: verbose);
            }

            Directory.Delete(newFileName);
            if (Directory.Exists(newFileName) || Directory.Exists(oldDirName))
                Console.WriteLine($"Fail to delete directory \"{oldDirName}\"");
            else
            if (verbose >= 2)
                Console.WriteLine($"Directory deletion successfull ended: \"{oldDirName}\"");

            progress.rewritedCnt++;
            progress.showMessage();
        }

        private static void deleteFile(FileInfo file, List<byte[]> bt, bool rename = true, bool onlyOne = false, Progress progress = null, int verbose = 0)
        {
            var oldFileName = file.FullName;

            if (verbose > 0)
            Console.WriteLine($"Try to delete file \"{oldFileName}\"");

            try
            {
                DateTime now, dt = DateTime.MinValue;
                TimeSpan ts;
                using (var fs = file.OpenWrite())
                {
                    long offset = 0;
                    foreach (var bt0 in bt)
                    {
                        offset = 0; fs.Seek(0, SeekOrigin.Begin);

                        while (offset < file.Length)
                        {
                            if (progress.slowDownFlag > 0)
                                dt = DateTime.Now;

                            var cnt = file.Length - offset;
                            if (cnt > bt0.Length)
                                cnt = bt0.Length;

                            fs.Write(bt0, 0, (int) cnt);
                            offset += cnt;

                            fs.Flush();
                            progress.rewritedSize += cnt;
                            progress.showMessage();

                            if (progress.slowDownFlag > 0)
                            {
                                now = DateTime.Now;
                                ts  = now - dt;

                                Thread.Sleep((int) ts.TotalMilliseconds);
                            }
                        }
                    }

                    foreach (var bt0 in bt)
                    {
                        // Расширение файла является необязательной операцией. Может не удастся, если диск уже заполнен
                        try
                        {
                            fs.Seek(offset, SeekOrigin.Begin);
    
                            var c = offset & 65535;
                            if (c != 0)
                            {
                                c = 65536 - c;
                                fs.Write(bt0, 0, (int) c);
                                fs.Flush();
                            }
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine($"Could not expand the file {oldFileName}. Error: {e.Message}");
                        }
                    }
                }
            }
            catch (System.UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Access denied for file \"{oldFileName}\"");
                return;
            }

            var newFileName = oldFileName;
            var fn = file.Name;
            var sb = new StringBuilder(fn.Length);

            if (rename)
            {
                for (char ci = ' '; ci <= 'z'; ci++)
                {
                    if (ci == '*' || ci == '?' || ci == Path.DirectorySeparatorChar || ci == Path.AltDirectorySeparatorChar)
                        continue;

                    sb.Clear();
                    sb.Append(ci, fn.Length);
                    fn = sb.ToString();
                    sb.Clear();

                    newFileName = Path.Combine(file.DirectoryName, fn);
                    if (File.Exists(newFileName) || Directory.Exists(newFileName))
                    {
                        if (onlyOne)
                        {
                            if (ci == 'z')
                                return;

                            continue;
                        }

                        deleteFile(new FileInfo(newFileName), bt, false);
                    }
    
                    file.MoveTo(newFileName);
                    break;
                }
            }

            File.Open(newFileName, FileMode.Truncate).Close();
            File.Delete(newFileName);

            progress.rewritedCnt++;
            progress.showMessage();

            if (File.Exists(newFileName) || File.Exists(oldFileName))
                Console.WriteLine($"Fail to delete file \"{oldFileName}\"");
            else
            if (verbose >= 2)
                Console.WriteLine($"File deletion successfull ended: \"{oldFileName}\"");
        }
    }
}
