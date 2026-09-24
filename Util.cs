using System;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MyZipper
{

    public class FileSearcher
    {
        /// <summary>
        /// 指定された深さ（階層）にあるファイルの一覧を取得します。
        /// </summary>
        /// <param name="rootPath">探索を開始するルートディレクトリのパス</param>
        /// <param name="targetDepth">対象とする深さ（1以上。ルート直下は1）</param>
        /// <returns>条件に一致するファイルのパスリスト</returns>
        public static List<string> GetFilesAtDepth(string rootPath, int targetDepth)
        {
            if (targetDepth < 1)
            {
                throw new ArgumentException("深さは1以上の値を指定してください。", nameof(targetDepth));
            }

            if (!Directory.Exists(rootPath))
            {
                throw new DirectoryNotFoundException($"指定されたルートパスが見つかりません: {rootPath}");
            }

            // ルートパスを標準化（末尾の区切り文字を削除）
            string normalizedRoot = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // ルートパス自体の区切り文字の数をカウント
            int rootSeparatorCount = CountSeparators(normalizedRoot);

            // 指定された深さに対応する区切り文字の数を計算
            // 例: ルートが「C:\Root」（区切り1）で深さ2なら、「C:\Root\Dir1\file.txt」（区切り3）を探す
            int targetSeparatorCount = rootSeparatorCount + targetDepth;

            var resultFiles = new List<string>();

            try
            {
                // 全てのファイルを列挙（SearchOption.AllDirectories でサブディレクトリも含める）
                var allFiles = Directory.EnumerateFiles(normalizedRoot, "*", SearchOption.AllDirectories);

                foreach (var file in allFiles)
                {
                    // ファイルパスの区切り文字数をカウント
                    int currentSeparators = CountSeparators(file);

                    if (currentSeparators == targetSeparatorCount)
                    {
                        resultFiles.Add(file);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // アクセス権限のないフォルダをスキップする場合のハンドリング
                Console.WriteLine("アクセス権限のないディレクトリが含まれています。");
            }

            return resultFiles;
        }

        /// <summary>
        /// パス文字列に含まれるディレクトリ区切り文字（\ または /）の数をカウントします。
        /// </summary>
        private static int CountSeparators(string path)
        {
            return path.Count(c => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar);
        }
    }

    public class DirectoryLister
    {
        /// <summary>
        /// 指定された階層（深さ）にあるサブディレクトリのパス一覧を取得します。
        /// </summary>
        /// <param name="rootPath">基点となるディレクトリのパス</param>
        /// <param name="depth">階層（1: 直下, 2: 孫ディレクトリ...）</param>
        /// <returns>該当するディレクトリのパスリスト</returns>
        public static List<string> GetDirectoriesAtDepth(string rootPath, int depth)
        {
            // ガード節：無効な値のチェック
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            {
                throw new DirectoryNotFoundException($"指定されたパスが見つかりません: {rootPath}");
            }

            if (depth < 1)
            {
                throw new ArgumentException("数値は1以上を指定してください。", nameof(depth));
            }

            // 現在の階層のディレクトリを追跡するためのリスト
            List<string> currentLevelDirs = new List<string> { rootPath };

            // 指定された深さ（depth）に達するまで、1階層ずつ深くしていく
            for (int i = 0; i < depth; i++)
            {
                List<string> nextLevelDirs = new List<string>();

                foreach (var dir in currentLevelDirs)
                {
                    try
                    {
                        // 現在のディレクトリ直下にあるサブディレクトリを取得
                        var subDirs = Directory.GetDirectories(dir);
                        nextLevelDirs.AddRange(subDirs);
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        // アクセス権限がないディレクトリはスキップ
                        Log.E($"{e}");
                    }
                    catch (Exception e)
                    {
                        // その他のエラー（パスが長すぎるなど）も実務を考慮してスキップ
                        Log.E($"{e}");
                    }
                }

                // 次の階層のディレクトリがなくなったら、これ以上深く潜れないので空のリストを返す
                if (nextLevelDirs.Count == 0)
                {
                    return new List<string>();
                }

                currentLevelDirs = nextLevelDirs;
            }

            return currentLevelDirs;
        }
    }

    public class Util
    {
        /// <summary>
        /// 指定されたファイルパスが存在する場合、存在しないユニークなファイルパス（連番付き）に変換して返却します。
        /// 存在しない場合は、元のパスをそのまま返します。
        /// </summary>
        /// <param name="originalPath">チェック対象のオリジナルファイルパス</param>
        /// <returns>存在しないことが保証されたファイルパス</returns>
        public static string GetUniqueFilePath(string originalPath)
        {
            // 引数チェック
            if (string.IsNullOrWhiteSpace(originalPath))
            {
                throw new ArgumentException("ファイルパスが空です。", nameof(originalPath));
            }

            // ファイルが既に存在しなければ、元のパスをそのまま返す
            if (!File.Exists(originalPath))
            {
                return originalPath;
            }

            // パスを「ディレクトリ」「ファイル名（拡張子なし）」「拡張子」に分解
            string directory = Path.GetDirectoryName(originalPath) ?? string.Empty;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
            string extension = Path.GetExtension(originalPath); // 例: ".txt"

            int count = 1;
            string uniquePath = originalPath;

            // 存在しないパスが見つかるまでループ
            while (File.Exists(uniquePath))
            {
                // 新しいファイル名を生成 (例: "sample (1).txt")
                string newFileName = $"{fileNameWithoutExtension} ({count}){extension}";

                // ディレクトリパスと結合
                uniquePath = Path.Combine(directory, newFileName);

                count++;
            }

            return uniquePath;
        }

        public static List<string> GetImageFiles(string targetPath, bool alldir)
        {
            string[] extensions = { ".jpg", ".jpeg", ".png", ".gif" };

            try
            {
                SearchOption searchOption;
                if (alldir)
                {
                    searchOption = SearchOption.AllDirectories;
                }
                else
                {
                    searchOption = SearchOption.TopDirectoryOnly;
                }

                /* .net 7-
                    var options = new EnumerationOptions
                    {
                        IgnoreInaccessible = true, // アクセス権のないフォルダをスキップ
                        RecurseSubdirectories = true // サブディレクトリも検索
                    };
                */
                var imageFiles = Directory.EnumerateFiles(
                    targetPath,
                    "*.*",
                    searchOption)
                    .Where(file => extensions.Contains(Path.GetExtension(file).ToLower()));

                return new List<string>(imageFiles);
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("アクセス権限のないフォルダが含まれています。");
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("指定されたパスが見つかりません。");
            }

            return null;
        }

        public static string FormatFileSize(long bytes)
        {
            var unit = 1024;
            if (bytes < unit) { return $"{bytes} B"; }

            var exp = (int)(Math.Log(bytes) / Math.Log(unit));
            return $"{bytes / Math.Pow(unit, exp):F2} {("KMGTPE")[exp - 1]}B";
        }

        static public bool IsDirectory(string path)
        {
            return File.GetAttributes(path).HasFlag(FileAttributes.Directory);
        }

        static public string[] GetDirectories(string path)
        {
            return Directory.GetDirectories(path);
        }

        static public (string, string) SplitPath(string path)
        {
            var dn = Path.GetDirectoryName(path);
            var fn = Path.GetFileName(path);
            return (dn, fn);
        }

        static public string GetParentDir(string path)
        {
            var dirname = Path.GetDirectoryName(path);
            return Path.GetFileName(dirname);
        }

        static public (string, string) DivPathDirAndFile(string path)
        {
            var dirname = Util.GetParentDir(path);
            var fn = Path.GetFileNameWithoutExtension(path);

            Log.I($"'{path}' => '{dirname}' | '{fn}'");

            return (dirname, fn);
        }

        static public string GetEntryName(string path)
        {
            var dirname = Path.GetDirectoryName(path);
            dirname = Path.GetFileName(dirname);
            var fn = Path.GetFileNameWithoutExtension(path);

            return dirname + "-" + fn;
        }

        static public string GetTitle(string path)
        {
            var dirname = Path.GetDirectoryName(path);
            dirname = Path.GetFileName(dirname);
            var fn = Path.GetFileNameWithoutExtension(path);

            return Path.Combine(dirname, fn);
        }

        static public string GetExt(string path)
        {
            return Path.GetExtension(path);
        }

        static public string GetZipPath(string path, string append_word)
        {
            var fn = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);
            var dirname = Path.GetDirectoryName(path);

            var length = 10;
            if (append_word.Length > length * 2 + 1)
            {
                append_word = append_word.Substring(0, length) + "～" + append_word.Substring(append_word.Length - length, length);
            }

            Log.D($"'{path}' => '{dirname}' | '{fn}' | '{append_word}' + '{ext}'");

            return Path.Combine(dirname, fn + append_word + ext);
        }

        static public string GetZipPath(string path, int cnt, int totalNo, string append_word = "")
        {
            var dirname = Path.GetDirectoryName(path);
            
            var fn = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);

            var type = "D"; // 補間タイプ ("D"=10進数)
            var digit = 3;  // 桁数
            if (totalNo > 999)
            {
                digit = 4;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append(type);
            sb.Append(digit);
            var number = cnt.ToString(sb.ToString());

            var filename = $"{fn}[{number}]{append_word}";
            var result = Path.Combine(dirname, filename + ext);
            return result;
        }

        static public string AppendPostfixToFilename(string origName, string appdStr)
        {
            var dirname = Path.GetDirectoryName(origName);
            var fn = Path.GetFileNameWithoutExtension(origName);
            var ext = Path.GetExtension(origName);

            return Path.Combine(dirname, fn + appdStr + ext);
        }

        static public string GetAspectRatioStr10_16(int width, int height)
        {
            if (width > height)
            {
                double ratio = (double)height / (double)width;
                var str = string.Format("16:{0}", (int)Math.Truncate(16 / ratio));
                return str;
            }
            else
            {
                double ratio = (double)height / (double)width;
                var str = string.Format("{0}:16", (int)Math.Truncate(ratio * 16));
                return str;
            }
        }

        /// <summary>
        /// 文字列が指定された文字数以上の表示、省略記号を指定文字数内に収めて短縮します。
        /// </summary>
        /// <param name="input">対象の文字列</param>
        /// <param name="n">この文字数以上の場合に短縮する（しきい値）</param>
        /// <param name="m">短縮後のトータル文字数（省略記号を含む）</param>
        /// <param name="ellipsis">省略記号（デフォルトは "..."）</param>
        public static string TruncateString(string input, int n, int m, string ellipsis = "～")
        {
            // 引数のバリデーション（例外処理）
            if (input == null) return null;
            if (m < ellipsis.Length)
            {
                throw new ArgumentException($"短縮後の文字数(m)は、省略記号の長さ({ellipsis.Length})以上で指定してください。");
            }

            // 文字列が n 文字未満ならそのまま返す
            if (input.Length < n)
            {
                return input;
            }

            // 文字列がすでに m 文字以下の場合はそのまま返す（無限ループやバグ防止）
            if (input.Length <= m)
            {
                return input;
            }

            // 省略記号を除いた、残せる文字数を計算
            int availableLength = m - ellipsis.Length;

            // 前半部分と後半部分に何文字ずつ割り振るかを計算
            int startLength = availableLength / 2;
            int endLength = availableLength - startLength; // 奇数の場合は後半が1文字多くなる

            // 文字列を切り出して結合
            string startPart = input.Substring(0, startLength);
            string endPart = input.Substring(input.Length - endLength);

            return startPart + ellipsis + endPart;
        }
    }

    public class Log
    {
        public static bool Quiet;
#if DEBUG
        public static bool Dbg = true;
#else
        public static bool Dbg = false;
#endif
        public static bool Verbose = false;

        static Log()
        {
        }

        static public void E(string s, params Object[] args)
        {
            LogOut("[E] ", s, args);
        }

        static public void W(string s, params Object[] args)
        {
            LogOut("[W] ", s, args);
        }

        static public void D(string str,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (Dbg)
            {
                
                Console.Error.WriteLine($"[D] {str}");
            }
            var d = false;
            if (d)
            {
                Console.Error.WriteLine($"[{DateTime.Now}] [{memberName}() '{filePath}'({lineNumber})] {str}");
            }
        }

        static public void I(string s, params Object[] args)
        {
            LogOut("[I] ", s, args);
        }

        static public void V(string s, params Object[] args)
        {
            if (Verbose)
            {
                LogOut("[V] ", s, args);
            }
        }

        static private void LogOut(string prefix, string s, params Object[] args)
        {
            Console.Error.Write(prefix);
            Console.Error.WriteLine(s, args);
        }

        static public void LogOutNoCRLF(string s)
        {
            Console.Error.Write(s);
        }

        static public void LogOut(string s)
        {
            Console.Error.WriteLine(s);
        }
    }

    public class Zip
    {
        static public void CreateEntryFromFile(ZipArchive archive, string rootpath, string infilepath)
        {
            var subdir = infilepath.Replace(rootpath + Path.DirectorySeparatorChar, "");//tekitou

            Log.D($"rootpath=  '{rootpath}'"); 
            //Log.D($"infilepath='{infilepath}'");
            Log.D($"subdir=    '{subdir}'");
            archive.CreateEntryFromFile(infilepath, subdir);
        }
    }

    public class StringHelper
    {
        /// <summary>
        /// 文字列から絵文字（Unicode絵文字記号）を除去します
        /// </summary>
        public static string RemoveEmoji(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // 絵文字、シンボル、その他の記号を対象とする正規表現
            // \p{Cs} はサロゲートペア（絵文字の主要な構成要素）をカバーします
            // \p{So} はその他の記号（Symbol Other）をカバーします
            return Regex.Replace(text, @"\p{Cs}|\p{So}|\p{Cn}", "");
        }
        /*
            \p{Cs} (Surrogate): 絵文字の多くは2つの16ビット値（サロゲートペア）で構成されています。これを除去の対象にします。
            \p{So} (Symbol, Other): サロゲートペアを使わない古い絵文字や、トランプのマークなどの記号をカバーします。
            \p{Cn} (Unassigned): まだ文字が割り当てられていないコードポイントですが、新しい絵文字がここに含まれることがあるため念のため含めています。
         */
            }
}
