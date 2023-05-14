// Виноградов Сергей Васильевич, 1984, Мытищи
// https://github.com/VinnySmallUtilities/sdel
// 2022-2023 год


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace sdel
{
    class MainClass
    {
        class Progress
        {
            public long rewritedSize      = 0;
            public long SizeToRewrite     = 0;

            public long rewritedCnt       = 0;
            public long cntToRewrite      = 0;
                                                                /// <summary>Выводить прогресс (pr)</summary>
            public int  showProgressFlag  = 0;                  /// <summary>Замедлять работу программы, вставляя паузы (sl)</summary>
            public int  slowDownFlag      = 0;                  /// <summary>Вместо перезатирания существующих файлов создать большой файл для перезатирания пустого пространства на диске (cr)</summary>
            public int  createDirectories = 0;
                                                                /// <summary>Создание файла с последующим удалением без перезатирания (crs)</summary>
            public int  createWithSimpleDeleting = 0;           /// <summary>Создавать только директории, не создавая большого файла (crf)</summary>
            public int  createDirectoriesOnly    = 0;
                                                                /// <summary>Не удалять директории (только файлы, оставить структуру папок нетронутой)</summary>
            public int  doNotDeleteDirectories   = 0;

            public DateTime lastMessage  = DateTime.MinValue;
            public DateTime creationTime = DateTime.Now;


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

                    if (cntToRewrite == 0)
                        cntProgress = 1f;
                    if (SizeToRewrite == 0)
                        sizeProgress = 1f;

                    return (cntProgress + sizeProgress) * 50f;
                }
            }

            public string progressStr => progress.ToString("F2") + "%    ";

            public uint IntervalToMessageInSeconds = 1;

            public void showMessage(string endOfMsg = "", bool notPrintProgress = false, bool forced = false)
            {
                if (showProgressFlag == 0)
                    return;

                var now = DateTime.Now;
                if (!forced && (now - lastMessage).TotalSeconds < IntervalToMessageInSeconds)
                    return;

                lastMessage = now;

                Console.CursorLeft = 0;
                Console.ForegroundColor = ConsoleColor.Green;
                if (notPrintProgress)
                    Console.Write(endOfMsg);
                else
                    Console.Write(progressStr + " " + endOfMsg);
                Console.ResetColor();
                Console.CursorVisible = false;
            }
            
            public string getMessageForEntireTimeOfSanitization
            {
                get
                {
                    var ts = DateTime.Now - creationTime;
                    return ts.ToString();
                }
            }
        }
        
        const int BufferSize         = 16 * 1024 * 1024;
        const int MinTimeToSleepInMs = 50;

        public static int Main(string[] args)
        {
#if DEBUG
            // args = new string[] { "v", "/home/g2/g2/.wine/" };
            // args = new string[] { "z3", "/inRam/1.txt" };
            // args = new string[] { "vvslpr", "/inRam/1/" };
            // args = new string[] { "-", "/inRam/Renpy/" };
            // args = new string[] { "vvprsl", "/Arcs/toErase" };
            // args = new string[] { "vvpr", "/Arcs/toErase" };
            // args = new string[] { "vv_pr_crds", "/inRam/1" };
            // args = new string[] { "vv_pr_crf", "/inRam/1" };
            // args = new string[] { "'v pr crf'", "/home/vinny/_toErase" };
            // args = new string[] { "'v pr crds'", "/inRam/rcd/_toErase" };
            // args = new string[] { "'v pr crs'", "/media/vinny/0A36-9B56/System Volume Information/_toErase" };
            // args = new string[] { "'v pr crs'" };
            // args = new string[] { ":prv" };
#endif

            int returnCode = -1;
            try
            {
                returnCode = Main_OneArgument(args);

                // if (returnCode != 0)
                if (returnCode == 101 || returnCode == 102)
                    return returnCode;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("ERROR: " + ex.Message + "\n" + ex.StackTrace + "\n\n\n");
                Console.ResetColor();
            }


            // sdel prv * добавляет новые параметры в конец
            if (args.LongLength > 2)
            for (long i = 2; i < args.LongLength; i++)
            {
                var line = args[i];
                Console.WriteLine("\n--------------------------------\n");
                Console.WriteLine("Try to re-executing with additional files");

                try
                {
                    var rc = Main_OneArgument(    new string[] {  args[0], line  }    );
    
                    if (rc != 0 && returnCode == 0)
                        returnCode = rc;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine("ERROR: " + ex.Message + "\n" + ex.StackTrace + "\n\n\n");
                    Console.ResetColor();
                }
            }

            if (returnCode != 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Error occured");
                Console.ResetColor();
            }

            return returnCode;
        }

        public static int Main_OneArgument(string[] args)
        // public static int Main(string[] args)
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;

            Console.CursorVisible = true;
            if (args.Length == 1 && args[0].Contains(":"))
            {
                var stdIn = Console.OpenStandardInput();
                using var stdR = new StreamReader(stdIn);
                var ec = ExecuteSdels(args[0], stdR);

                Console.CursorVisible = true;
                return ec;
            }

            bool isFirstFileError = false;
            if (args.Length > 0)
            {
                isFirstFileError = File.Exists(args[0]) || Directory.Exists(args[0]);
            }

            if (args.Length < 2 || isFirstFileError)
            {
                Console.WriteLine("sdel version: 2023 may 13.2");
                Console.Error.WriteLine("sdel 'flags' dir");
                Console.WriteLine("Examples:");
                Console.WriteLine("sdel - /home/user/.wine");
                Console.WriteLine("sdel v /home/user/.wine");
                Console.WriteLine("flag 'v' switches to verbosive mode");
                Console.WriteLine("flag 'vv' switches to twice verbosive mode");
                Console.WriteLine("flag 'z' switches to 0x00 pattern");
                Console.WriteLine("flag 'z2' switches to twice rewriting. 0x55AA and 0x00 patterns");
                Console.WriteLine("flag 'z3' switches to three rewriting. 0xCCCC, 0x6666, 0x00 patterns");
                Console.WriteLine("flag 'pr' show progress");
                Console.WriteLine("flag 'sl' get slow down progress (pauses when using the disk)");
                Console.WriteLine("flag 'cr' set to creation mode. A not existence file must be created as big as possible");
                Console.WriteLine("flag 'crd' set to creation mode with create a many count of directories");
                Console.WriteLine("flag 'crds' or 'crs' set to the creation mode with a one time to write at the creation file moment");
                Console.WriteLine("flag 'crf' set to the creation mode for create directories only");
                Console.WriteLine("use ':' to use with conveyor. Example: ls -1 | sdel 'v:-'");
                Console.WriteLine("ndd - do not delete directories");
                Console.WriteLine("Example:");
                Console.WriteLine("sdel vvz2pr /home/user/.wine");
                Console.WriteLine("sdel vv_z2_pr /home/user/.wine");
                Console.WriteLine("sdel 'vv z2 pr' /home/user/.wine");
                Console.WriteLine("sdel \"vv z2 pr\" /home/user/.wine");
                Console.WriteLine("for SSD and flash");
                Console.WriteLine("sdel 'crd pr v sl' ~/_toErase");
                Console.WriteLine("for magnetic disks");
                Console.WriteLine("sdel 'crds pr v sl' ~/_toErase");
                Console.WriteLine("for file");
                Console.WriteLine("sdel 'pr v' ~/_toErase");
                Console.WriteLine("for directory");
                Console.WriteLine("sdel 'pr v sl' ~/_toErase");
                Console.WriteLine("for delete all logs in the Linux /var/log");
                Console.WriteLine("sudo sdel ndd /var/log");

                return 101;
            }

            Progress progress = new Progress(showProgressFlag: 0);

            var path0 = args[1];
            var path  = Path.GetFullPath(path0); // new FileInfo(path0).FullName;
            var flags = args[0];
            int verbose = GetVerboseFlag(flags);

            var zFlag = flags.Contains("z") ? 1 : 0;
            if (flags.Contains("z2"))
            {
                zFlag = 2;

                if (verbose > 0)
                    Console.WriteLine("z2: double rewrite: 0x55AA and 0x0000");
            }
            else
            if (flags.Contains("z3"))
            {
                zFlag = 3;

                if (verbose > 0)
                    Console.WriteLine("z3: triple rewrite: 0xCCCC 0x6666 0x00");
            }

            var creationMode = flags.Contains("cr");
            if (creationMode)
            {
                if (verbose > 0)
                    Console.WriteLine("cr* - creation mode");

                if (flags.Contains("sl"))
                {
                    progress.slowDownFlag = 1;
                    Console.WriteLine("sl - slow down");
                }

                if (flags.Contains("pr"))
                    progress.showProgressFlag = 1;
                if (flags.Contains("crd") || flags.Contains("crsd"))
                    progress.createDirectories = 1;
                if (flags.Contains("crs") || flags.Contains("crds") || flags.Contains("crsd"))
                {
                    progress.createWithSimpleDeleting = 1;
                    Console.WriteLine("cr?s - creation mode without the file wiping (only create and fill)");
                }
                if (flags.Contains("crf"))
                {
                    progress.createDirectories = 1;
                    progress.createDirectoriesOnly = 1;
                    if (verbose > 0)
                        Console.WriteLine($"crf - Only directories created, no have big file");
                }

                if (!createFile(path, progress: progress, verbose: verbose))
                {
                    Console.CursorVisible = true;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"File creation failed with name '{path}'");
                    Console.ResetColor();
                    return 111;
                }

                if (progress.createWithSimpleDeleting > 0)
                {
                    if (verbose > 0)
                        Console.WriteLine($"Usually directory deleting (without additional rewriting)");

                    Directory.Delete(path, true);
                    Console.WriteLine($"Program ended with time {progress.getMessageForEntireTimeOfSanitization}. Deletion successfull ended for directory '{path}'");

                    Console.CursorVisible = true;
                    return 0;
                }
            }

            if (path0.StartsWith("'") || path0.StartsWith("\""))
            {
                if (!Directory.Exists(path) && !File.Exists(path))
                {
                    path0 = path0.Substring(1, path0.Length - 2);
                    path  = Path .GetFullPath(path0);
                }
            }

            var isDirectory = false;
            if (Directory.Exists(path))
            {
                isDirectory = true;
                Console.WriteLine($"Directory to deletion: \"{path}\"");
            }
            else
            if (!File.Exists(path))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Error.WriteLine($"File not exists:\n\"{path}\"");
                Console.ResetColor();

                Console.CursorVisible = true;
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
            var A = new byte[] { 0x55, 0xAA };
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
                    var dt = progress.creationTime;
                    progress = new Progress(SizeToRewrite: fi.Length, cntToRewrite: 1);

                    progress.creationTime = dt;
                }

                if (flags.Contains("sl"))
                {
                    progress.slowDownFlag = 1;
                    Console.WriteLine("sl - slow down");
                }

                deleteFile(fi, bt, progress: progress, true, true, verbose: verbose);

                if (File.Exists(path))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Program ended with time {progress.getMessageForEntireTimeOfSanitization}. Deletion failed for file '{path}'");
                    Console.ResetColor();

                    Console.CursorVisible = true;
                    return 15;
                }

                Console.WriteLine($"Program ended with time {progress.getMessageForEntireTimeOfSanitization}. Deletion successfull ended for file '{path}'");

                Console.CursorVisible = true;
                return 0;
            }

            if (verbose >= 1)
            {
                Console.WriteLine("Prepare a list of files to data sanitization");
            }

            if (flags.Contains("sl"))
            {
                progress.slowDownFlag = 1;
                Console.WriteLine("sl - slow down");
            }

            if (flags.Contains("ndd"))
            {
                progress.doNotDeleteDirectories = 1;
                Console.WriteLine("ndd - do not delete directories");
            }

            var di = new DirectoryInfo(path);

			try
			{
				if (flags.Contains("pr"))
				{
					// var list = di.GetFiles("*", SearchOption.AllDirectories);
					var list = di.EnumerateFiles("*", SearchOption.AllDirectories);

					var dt = progress.creationTime;
					progress = new Progress();
					progress.creationTime = dt;

					foreach (var file in list)
					{
						try
						{
							progress.cntToRewrite += 1;
							progress.SizeToRewrite += file.Length;
						}
						catch
						{}
					}

					if (zFlag >= 2)
						progress.SizeToRewrite *= zFlag;
					if (zFlag >= 4)
						throw new NotImplementedException();

					var dirList = di.EnumerateDirectories("*", SearchOption.AllDirectories);
					foreach (var file in list)
					{
						progress.cntToRewrite += 1;
					}
				}
			}
			catch (Exception e)
			{
				Console.Error.WriteLine($"Error for calculate progress for '{di.FullName}'\n{e.Message}\n{e.StackTrace}");
			}

			DeleteFilesFromDir(di, bt: bt, progress: progress, verbose: verbose);

            di.Refresh();
            var checkList = di.GetFiles();
            if (checkList.LongLength > 0)
            {
                Console.CursorVisible = true;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Files is not deleted. Some files wich not has been deleted:");
                Console.ResetColor();

                var cnt = 0;
                foreach (var fileInfo in checkList)
                {
                    Console.Error.WriteLine(fileInfo);
                    cnt++;
                    if (cnt > 16)
                    {
                        Console.Error.WriteLine($"and others: {cnt} files in total");
                        break;
                    }
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine();
                Console.Error.WriteLine("Files is not deleted");
                Console.ResetColor();

                return 11;
            }

            try
            {
                deleteDir(di, progress: progress, verbose: verbose);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Deletion failed for directory '{di.FullName}' with exception\n{e.Message}\n{e.StackTrace}\n\n");
            }

            Console.CursorVisible = true;
            di.Refresh();
            var checkCount = checkList.LongLength;
            if (di.Exists)
            {
                if (progress.doNotDeleteDirectories > 0)
                {}
                else
                {
                    // Проверяем содержимое всех директорий, включая поддиректории - их не должно быть
                    // checkCount = di.GetFileSystemInfos().LongLength;
                    checkCount = 1;
                }

                if (checkCount > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Deletion failed for directory '{path}'. Program ended with time {progress.getMessageForEntireTimeOfSanitization}.");
                    Console.ResetColor();
                    return 12;
                }
            }

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine($"Program ended with time {progress.getMessageForEntireTimeOfSanitization}. Deletion successfull ended for directory '{path}'");
            Console.WriteLine();

            return 0;
        }

		// Такая рекурсия нужна, потому что часть файлов могут быть недоступны по каким-то причинам для удаления и мы должны удалить хотя бы то, что можем
		static void DeleteFilesFromDir(DirectoryInfo di, List<byte[]> bt, Progress progress, int verbose = 0)
		{
			// Удаляем файлы в директории
			for (int @try = 0; @try < 2; @try++)
			{
				try
				{
					var list = di.EnumerateFiles("*", SearchOption.TopDirectoryOnly);

					foreach (var file in list)
					{
						try
						{
							file.Refresh();

							if (file.Exists)
								deleteFile(file, bt, progress: progress, true, verbose: verbose);
							else
							if (verbose >= 2)
								Console.WriteLine($"File not exists'{file.FullName}' (removed from another program?)");
						}
						catch (UnauthorizedAccessException e)
						{
							Console.Error.WriteLine($"Error (PD) for file '{file.FullName}'\n{e.Message}\n{e.StackTrace}");
						}
						catch (Exception e)
						{
							Console.Error.WriteLine($"Error (1) for file '{file.FullName}'\n{e.Message}\n{e.StackTrace}");
						}
					}
				}
				catch (Exception e)
				{
					Console.Error.WriteLine($"Error (1) for directory '{di.FullName}'\n{e.Message}\n{e.StackTrace}");
				}
			}

			// Удаляем файлы в поддиректориях
			for (int @try = 0; @try < 2; @try++)
			{
				try
				{
					var list = di.EnumerateDirectories("*", SearchOption.TopDirectoryOnly);

					foreach (var dir in list)
					{
						try
						{
							if (dir.LinkTarget is null)
							DeleteFilesFromDir(dir, bt: bt, progress: progress, verbose: verbose);
						}
						catch (Exception e)
						{
							Console.Error.WriteLine($"Error (1) for subdirectory '{dir.FullName}'\n{e.Message}\n{e.StackTrace}");
						}
					}
				}
				catch (Exception e)
				{
					Console.Error.WriteLine($"Error (2) for directory '{di.FullName}'\n{e.Message}\n{e.StackTrace}");
				}
			}
		}

        private static int GetVerboseFlag(string flags)
        {
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

            return verbose;
        }

        public static int ExecuteSdels(string flagsOfAll, StreamReader stdR)
        {
            int rc    = 0;
            var foa   = flagsOfAll.Split(  new char[] { ':' },   StringSplitOptions.None  );
            var flags = foa[0];

            int verbose = GetVerboseFlag(flags);
            if (verbose > 0)
                Console.WriteLine("Wait for std.input file names");

            do
            {
                var line = stdR.ReadLine();
                if (line == null)
                    break;

                // var sdelName = typeof(MainClass)!.Assembly!.Location;
                var sdelName = System.AppContext.BaseDirectory!;
                    sdelName = Path.Combine(sdelName, "sdel");
                var cmdLine = $"'{foa[1]}' '{line}'";
                if (verbose > 0)
                    Console.WriteLine($"Start {sdelName} {cmdLine}");

                var psi = new ProcessStartInfo(sdelName, cmdLine);

                psi.UseShellExecute = false;
                psi.CreateNoWindow  = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError  = true;
                using (var pi = Process.Start(psi))
                {
                    if (pi == null)
                    {
                        rc = 1;
                        continue;
                    }

                    pi.WaitForExit();
                    
                    if (pi.ExitCode != 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        rc = pi.ExitCode;
                    }

                    Console.Error.WriteLine(pi.StandardError.ReadToEnd());
                    Console.WriteLine(pi.StandardOutput.ReadToEnd());

                    Console.ResetColor();
                }
            }
            while (true);

            if (rc != 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Error occured");
                Console.ResetColor();
            }

            return rc;
        }

        private static bool createFile(string path, Progress progress, int verbose)
        {
            var bt1 = new byte[BufferSize];
            // var A   = new byte[] { 0x92, 0x49 };
            for (int i = 0; i < bt1.Length; i++)
            {
                bt1[i] = 0;
            }

            // if (verbose > 0)
            Console.WriteLine($"Try to create directory {path}");
            
            if (Directory.Exists(path) || File.Exists(path))
            {
                Console.WriteLine($"Directory creation failed for path {path} (directory or file already exists). Program terminated");

                return false;
            }
            
            var dir = new DirectoryInfo(path);
            dir.Create();

            var mainFileName = "0123456789012345";
            var mainFile     = new FileInfo(Path.Combine(dir.FullName, mainFileName));
            
            DateTime now, dt = DateTime.MinValue;
            TimeSpan ts;

            FileStream? fs = null;
            if (progress.createDirectoriesOnly <= 0)
            try
            {
                try
                {
                    fs = new FileStream(mainFile.FullName, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.WriteThrough);
                }
                catch
                {
                    fs = new FileStream(mainFile.FullName, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1 << 16, FileOptions.None);
                }

                if (progress.slowDownFlag > 0)
                    dt = DateTime.Now;

                long offset = 0;
                fs.Seek(0, SeekOrigin.Begin);
                int  lenToWrite = bt1.Length;

                var cntOneBytes = 0;
                while (cntOneBytes < 16)
                {
                    var cnt = lenToWrite;

                    try
                    {
                        fs.Write(bt1, 0, (int) cnt);
                        offset += cnt;

                        fs.Flush();

                        var sz = offset/1024/1024;
                        progress.showMessage($"Creation file, Mb: {sz.ToString("#,#")}", true);
                    }
                    catch
                    {
                        if (lenToWrite == 1)
                        {
                            cntOneBytes++;
                            
                            if (cntOneBytes == 1)
                            {
                                var sz = offset/1024/1024;
                                if (sz > 0)
                                    progress.showMessage($"Creation file, Mb: {sz.ToString("#,#,#")}", true, forced: true);
                                else
                                    progress.showMessage($"Creation file, bytes: {offset.ToString("#,#,#")}", true, forced: true);

                                Console.WriteLine();    // Это чтобы был виден прогресс, чтобы его не перезатереть нижеследующим сообщением
                            }

                            progress.showMessage($"for creation: try to expand file with 1 bytes, count of tries: {cntOneBytes}", true);
                            Thread.Sleep(500);      // Вдруг ещё место сейчас освободится? Чуть ждём.

                            dt = DateTime.Now;
                        }

                        lenToWrite >>= 1;
                        if (lenToWrite < 1)
                            lenToWrite = 1;
                    }

                    if (progress.slowDownFlag > 0)
                    {
                        now = DateTime.Now;
                        ts  = now - dt;

                        if (ts.TotalMilliseconds <= MinTimeToSleepInMs)
                            continue;

                        Thread.Sleep((int) ts.TotalMilliseconds);
                        dt = DateTime.Now;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + "\n\n" + e.StackTrace);
            }
            finally
            {
                try
                {
                    fs?.Close();
                }
                catch
                { }
            }
            

            // label1:

            Console.WriteLine();

            var sb  = new StringBuilder();
            int len = 1024;
            var rnd = new Random();
            var cc  = 0;

            dt  = DateTime.MinValue;

            if (progress.slowDownFlag > 0)
                dt = DateTime.Now;

            int ms;

            List<DirectoryInfo> diList = new List<DirectoryInfo>();
            diList.Add(dir);
            var index  = 0;
            var lastcc = index;

            if (progress.createDirectories > 0)
            while (len > 0)
            {
                dir = diList[index];

                if (progress.slowDownFlag > 0)
                {
                    now = DateTime.Now;
                    ms = (int) (now - dt).TotalMilliseconds;
                    if (ms >= MinTimeToSleepInMs)
                    {
                        Thread.Sleep(ms);
                        dt = DateTime.Now;
                    }
                }

                sb.Clear();
                progress.showMessage($"(try to create a big count of directories, count of tries: {cc.ToString("#,#,#")}, {lastcc})", true);

                for (int i = 0; i < len; i++)
                {
                    var n = rnd.Next(0, 10);
                    var c = (char) (n + '0');
                    sb.Append(c);
                }

                var sbName = sb.ToString();

                do
                {
                    try
                    {
                        var newDir = dir.CreateSubdirectory(sbName);
                        cc++;
                        lastcc = index;

                        // Защита от того, что мы запомним слишком много директорий
                        if (diList.Count < 1_000_000)
                            diList.Add(newDir);

                        break;
                    }
                    catch
                    {
                        if (index <= 1)
                            len--;
                        else
                            len >>= 1;

                        sbName = sbName.Substring(startIndex: 0, length: len);

                        if (sbName.Length <= 0 || len <= 0)
                        {
                            // Выходим, если не создано ни одной директории в главной директории
                            if (cc == 0)
                            {
                                len = 0;
                                break;
                            }

                            // Будем создавать директории в субдиректориях: на всякий случай. Они обычно не создаются, но бывают исключения
                            index++;
                            if (index >= diList.Count)
                            {
                                len = 0;
                                break;
                            }

                            if (index - lastcc > 16)
                            {
                                len = 0;
                                break;
                            }

                            len = 1024;
                            break;
                        }
                    }
                }
                while (true);
            }

            progress.showMessage($"(try to create a big count of directories, count of tries: {cc.ToString("#,#,#")}, {lastcc})", true, forced: true);

            Thread.Sleep(500);
            Console.WriteLine();        // Перевод строки после progress.showMessage
            return true;
        }

        /// <summary>Инициализирует массив одним и тем же значением на весь массив</summary>
        /// <param name="bt">Список для добавления массива</param>
        /// <param name="pattern">Шаблон заполнения</param>
        private static void ArrayInitialization(List<byte[]> bt, byte pattern)
        {
            var bt0 = new byte[BufferSize];
            for (int i = 0; i < bt0.Length; i++)
            {
                bt0[i] = pattern;
            }
            bt.Add(bt0);
        }

        private static void deleteDir(DirectoryInfo dir, Progress progress, int verbose = 0)
        {
            if (progress.doNotDeleteDirectories > 0)
                return;

            var oldDirName = dir.FullName;

            if (verbose > 0)
            Console.WriteLine($"Try to delete directory \"{dir.FullName}\"");

			// Также пропускаем ссылки, не проходя по ним. То есть мы сейчас не будем перечислять директорию, которая является ссылкой
			if (dir.LinkTarget is null)
			{
				var dirList = dir.GetDirectories();

				foreach (var di in dirList)
				{
					try
					{
						deleteDir(di, progress: progress, verbose: verbose);
					}
					catch (UnauthorizedAccessException e)
					{
						Console.Error.WriteLine($"Error for dir {di.FullName}\n{e.Message}\n{e.StackTrace}");
					}
				}
			}

            var newFileName = dir.FullName;

            var fn = dir.Name;
            var sb = new StringBuilder(fn.Length);
            for (char ci = ' '; ci <= 'z'; ci++)
            {
                try
                {
                    if (ci == '*' || ci == '?' || ci == Path.DirectorySeparatorChar || ci == Path.AltDirectorySeparatorChar)
                        continue;
    
                    sb.Clear();
                    sb.Append(ci, fn.Length);
                    fn = sb.ToString();
                    sb.Clear();

                    newFileName = Path.Combine(dir!.Parent!.FullName!, fn);
                    if (File.Exists(newFileName) || Directory.Exists(newFileName))
                    {
                        continue;
                    }

                    dir!.MoveTo(newFileName);
                    break;
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Error for dir {dir.FullName} with newFileName '{newFileName}'\n{e.Message}\n{e.StackTrace}");
                }
            }

			if (dir.LinkTarget is null)
			{
            	Directory.Delete(newFileName);
			}
			else
			{
				File.Delete(newFileName);
			}

            if (Directory.Exists(newFileName) || Directory.Exists(oldDirName))
                Console.WriteLine($"Fail to delete directory \"{oldDirName}\"");
            else
            if (verbose >= 2)
                Console.WriteLine($"Directory deletion successfull ended: \"{oldDirName}\"");

            progress.rewritedCnt++;
            progress.showMessage();
        }

        private static void deleteFile(FileInfo file, List<byte[]> bt, Progress progress, bool rename = true, bool onlyOne = false, int verbose = 0)
        {
            var oldFileName = file.FullName;

            if (verbose > 0)
			{
				if (file.LinkTarget is null)
				{
					Console.WriteLine($"Try to delete file \"{oldFileName}\"");
				}
				else
					Console.WriteLine($"The wiping has been skipped for link with name \"{oldFileName}\"");
			}

			FileStream? fs = null;

			var faMode = FileAccess.Write | FileAccess.Read;
			if (file.LinkTarget is null)
            try
            {
                DateTime now, dt = DateTime.MinValue;
                TimeSpan ts;

				try
				{
					fs = new FileStream(file.FullName, FileMode.Open, faMode, FileShare.None, 1, FileOptions.WriteThrough);
				}
				catch
				{
					faMode = FileAccess.Write;
					if (verbose > 1)
					Console.WriteLine($"Can not open file with 'rw' mode: \"{oldFileName}\"\nWill try 'w' mode");

					try
					{
						fs = new FileStream(file.FullName, FileMode.Open, faMode, FileShare.None, 1, FileOptions.WriteThrough);
					}
					catch (Exception e)
					{
						if (verbose > 1)
						Console.WriteLine($"Can not open file with 'w' mode: \"{oldFileName}\"\n{e.Message}");
					}
				}

				if (fs != null && !fs.CanSeek)
				{
					Console.WriteLine($"File not support seeking and skipped (may be pipe or another service file): \"{oldFileName}\"");
					fs.Close();
					fs = null;
				}


				// Есть файлы типа pipe, они зависают при попытке открыть их только на запись, но на чтение+запись открываются нормально
				// prwx------ 1 root        root        0 фев 17 12:27 /tmp/clr-debug-pipe-813-1941-in|
				if (fs != null)
                using (fs)
                {
                    if (progress.slowDownFlag > 0)
                        dt = DateTime.Now;

                    long offset = 0;
                    foreach (var bt0 in bt)
                    {
                        offset = 0; fs.Seek(0, SeekOrigin.Begin);

                        while (offset < file.Length)
                        {
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

                                var ms = (int) ts.TotalMilliseconds;
                                if (ms <= MinTimeToSleepInMs)
                                    //continue;
                                    ms = MinTimeToSleepInMs;

                                Thread.Sleep(ms);
                                dt = DateTime.Now;
                            }
                        }
                    }

                    foreach (var bt0 in bt)
                    {
                        // Расширение файла является необязательной операцией. Может не удастся, если диск уже заполнен
                        try
                        {
                            fs.Seek(offset, SeekOrigin.Begin);

                            // Выравниваем на границу 64 кб
                            // Возможно, стоит выравнивать на границу 1 Мб
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
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Error.WriteLine($"Could not expand the file {oldFileName}. Error: {e.Message}");
                            Console.ResetColor();
                        }
                    }
                }
            }
            catch (System.UnauthorizedAccessException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Access denied for file \"{oldFileName}\"");
                Console.ResetColor();

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

                    try
                    {
                        sb.Clear();
                        sb.Append(ci, fn.Length);
                        fn = sb.ToString();
                        sb.Clear();

                        newFileName = Path.Combine(file.DirectoryName!, fn);
                        if (newFileName == oldFileName)
                            continue;

                        if (File.Exists(newFileName) || Directory.Exists(newFileName))
                        {
                            if (onlyOne)
                            {
                                if (ci == 'z')
                                    return;

                                continue;
                            }

                            // Кто сказал, что newFileName можно удалять?
                            // В целом, его можно удалять, т.к. мы удаляем целую директорию - флаг onlyOne
                            // deleteFile(new FileInfo(newFileName), bt, progress: progress, false);
                            // Однако, есть проблема.
                            // Есть мы не можем удалить текущий файл, то не сможем удалить и этот:
                            // ведь у него одинаковая длина и он также будет переименован в аналогичные имена
                            continue;
                        }

                        // Похоже, FileInfo.MoveTo перемещает файл путём копирования, а затем - удаления. Это не то, что нам надо
                        // file.MoveTo(newFileName, false);
                        // Так что будем использовать системные команды
                        MoveFile(oldFileName, newFileName);

                        break;
                    }
                    catch (Exception)
                    {
                        if (ci == 'z')
                            throw;
                    }
                }
            }

			try
			{
				// Обрезаем файл только в том случае, если это не ссылка
				if (fs != null)
				{
                    // FileAccess.Write нужен тогда, когда у нас есть доступ на запись, но не на чтение
	            	File.Open(newFileName, FileMode.Truncate, FileAccess.Write).Close();
				}

				File.Delete(newFileName);		// Если не получилось обрезать файл, не будем и удалять его, чтобы его можно было дальше обрезать каким-то другим способом и было понятно, что удаление не закончилось успехом
			}
			catch (Exception e)
			{
				//if (verbose > 0)
				Console.WriteLine($"Fail to truncate file \"{oldFileName}\" with error: '{e.Message}'");
			}

            progress.rewritedCnt++;
            progress.showMessage();

            if (File.Exists(newFileName) || File.Exists(oldFileName))
                Console.WriteLine($"Fail to delete file \"{oldFileName}\"");
            else
            if (verbose >= 2)
                Console.WriteLine($"File deletion successfull ended: \"{oldFileName}\"");
        }

        static string mvCommandName  = "mv";
        static string mvCommandName2 = "move";
        public static void MoveFile(string oldFileName, string newFileName)
        {
            // System.OperatingSystem.IsLinux

            if (System.OperatingSystem.IsWindows())
                mvCommandName = "move";

            try
            {
                var psi = new ProcessStartInfo(mvCommandName, $"\"{oldFileName}\" \"{newFileName}\"");

                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError  = true;
                using (var pi = Process.Start(psi))
                {
                    // Этого никогда не бывает, кажется
                    if (pi == null)
                    {
                        // Console.WriteLine("pi == null");
                        /*if (mvCommandName != mvCommandName2)
                        {
                            mvCommandName = mvCommandName2;
                            MoveFile(oldFileName, newFileName);
                            return;
                        }
                        else */throw new Exception("pi == null");
                    }

                    pi.WaitForExit();

                    if (pi.ExitCode != 0)
                    {
                        throw new Exception($"Error occured with rename the file; {mvCommandName} unsuccessfull. " + pi.StandardError.ReadToEnd() + "\n" + pi.StandardOutput.ReadToEnd());
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception e)
            {
                Console.Error.WriteLine(e.Message);
                /*if (mvCommandName != mvCommandName2)
                {
                    mvCommandName = mvCommandName2;
                    MoveFile(oldFileName, newFileName);
                    return;
                }
                else*/
                    try
                    {
                        var psi = new ProcessStartInfo("whoami");

                        psi.UseShellExecute = false;
                        psi.CreateNoWindow  = true;
                        psi.RedirectStandardOutput = true;
                        psi.RedirectStandardError  = true;
                        using (var pi = Process.Start(psi))
                        {
                            pi?.WaitForExit();
                            Console.WriteLine(pi?.StandardOutput?.ReadToEnd());
                        }
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine(ex2.Message);
                    }

                    throw new Exception("Error occured with rename the file; 'mv' and 'move' system commands has been tried, but its unsuccessfull");
            }
        }
    }
}
