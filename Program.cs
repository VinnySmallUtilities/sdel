// Виноградов Сергей Васильевич, 1984, Мытищи
// https://github.com/VinnySmallUtilities/sdel
// 2022 год


using System;
using System.IO;
using System.Text;

namespace sdel
{
    class MainClass
    {
        public static int Main(string[] args)
        {
            // args = new string[] { "v", "/home/g2/g2/.wine/" };
            // args = new string[] { "-", "/inRam/1.txt" };
            // args = new string[] { "-", "/inRam/1/" };
            // args = new string[] { "-", "/inRam/Renpy/" };

            if (args.Length < 1)
            {
                Console.Error.WriteLine("sdel dir");
                Console.WriteLine("Examples:");
                Console.WriteLine("sdel - /home/user/.wine");
                Console.WriteLine("sdel v /home/user/.wine");
                Console.WriteLine("flag 'v' switches to verbosive mode");
                Console.WriteLine("flag 'vv' switches to twice verbosive mode");
                Console.WriteLine("flag 'z' switches to 0x00 pattern");
                Console.WriteLine("Example:");
                Console.WriteLine("sdel vvz /home/user/.wine");
                // Console.WriteLine("flag 'zz' switches to twice rewriting. 0x55AA and 0x00 pattern");
                return 101;
            }


            var bt    = new byte[16*1024*1024];
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
            
            var zFlag = flags.Contains("z") ? 1 : 0;

            var isDirectory = false;
            if (Directory.Exists(path))
            {
                isDirectory = true;
                Console.WriteLine($"Directory to deletion: \"{path}\"");
            }
            else
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"Directory or file not exists:\n\"{path}\"");
                return 102;
            }
            else
            {
                Console.WriteLine($"File to deletion: \"{path}\"");
            }

            var A = new byte[] { 0x55, 0xAA };
            for (int i = 0; i < bt.Length; i++)
            {
                if (zFlag == 0)
                    bt[i] = A[i & 1];
                else
                    bt[i] = 0;  // Флаг z установлен
            }

            if (!isDirectory)
            {
                deleteFile(new FileInfo(path), bt, true, true, verbose: verbose);
                return 0;
            }

            var di   = new DirectoryInfo(path);
            var list = di.EnumerateFiles("*", SearchOption.AllDirectories);

            foreach (var file in list)
            {
                deleteFile(file, bt, true, verbose: verbose);
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

            deleteDir(di, verbose: verbose);
            
            di.Refresh();
            if (di.Exists)
            {
                Console.WriteLine("Deletion failed");
                return 12;
            }

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Deletion successfull ended");
            Console.WriteLine();

            return 0;
        }

        private static void deleteDir(DirectoryInfo dir, int verbose = 0)
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
        }

        private static void deleteFile(FileInfo file, byte[] bt, bool rename = true, bool onlyOne = false, int verbose = 0)
        {
            var oldFileName = file.FullName;

            if (verbose > 0)
            Console.WriteLine($"Try to delete file \"{oldFileName}\"");

            try
            {
                using (var fs = file.OpenWrite())
                {
                    long offset = 0;
                    while (offset < file.Length)
                    {
                        var cnt = file.Length - offset;
                        if (cnt > bt.Length)
                            cnt = bt.Length;

                        fs.Write(bt, 0, (int) cnt);
                        offset += cnt;

                        fs.Flush();
                    }

                    var c = offset & 65535;
                    if (c != 0)
                    {
                        c = 65536 - c;
                        fs.Write(bt, 0, (int) c);
                        fs.Flush();
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

            if (File.Exists(newFileName) || File.Exists(oldFileName))
                Console.WriteLine($"Fail to delete file \"{oldFileName}\"");
            else
            if (verbose >= 2)
                Console.WriteLine($"File deletion successfull ended: \"{oldFileName}\"");
        }
    }
}
