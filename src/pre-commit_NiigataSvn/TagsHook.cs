using SharpSvn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SvnHook.PreCommit
{
    /// <summary>
    /// tags 配下を操作するためのフックスクリプト。
    /// </summary>
    public class TagsHook
    {
        public const string NodeName = @"tags";

        // tags へ許可される新規ディレクトリパス
        private static readonly Regex AcceptNewDirectoryRegex = new Regex(@"(^.*/tags/v[0-9]+\.[0-9]+$)|(^.*/tags/v[0-9]+\.[0-9]+/(dev|prod)$)", RegexOptions.Compiled);
        private static readonly Regex AcceptCopyFromPathDirectoryRegex = new Regex(@"(^.*/tags/v[0-9]+\.[0-9]+/[0-9]{11}_[^/]+$)|(^.*/tags/v[0-9]+\.[0-9]+/(dev|prod)/[0-9]{11}_[^/]+$)", RegexOptions.Compiled);

        /// <summary>
        /// tags ディレクトリ配下
        /// </summary>
        /// <param name="ci">SvnChangeInfoEventArgs オブジェクト。</param>
        /// <returns>True:有効, False:無効。</returns>
        public static bool Validate(SvnChangeInfoEventArgs ci)
        {
            string newDirectoryMaxPath = "";
            string copyFromPathMaxPath = "";

            // tags が存在するパスのみ処理
            foreach (SvnChangeItem item in ci.ChangedPaths.Where(s => s.Path.IndexOf(NodeName) >= 0))
            {
                // tags ノードでない場合は処理対象外
                if (!IsTagsNode(item))
                {
                    continue;
                }

                // 追加操作以外は拒否
                if (!IsAddAction(item))
                {
                    Console.WriteLine("----------------------------------------------------------");
                    Console.WriteLine("tags配下は追加以外の操作はできません。");
                    Console.WriteLine("----------------------------------------------------------");
                    return false;
                }

                // 新規作成される中間ディレクトリ
                newDirectoryMaxPath = GetNewDirectoryMaxPath(item, newDirectoryMaxPath);

                // tags 作成先のディレクトリ
                copyFromPathMaxPath = GetCopyFromPathMaxPath(item, copyFromPathMaxPath);

                // 新規作成される中間ディレクトリが許可されない形式なら拒否
                if (newDirectoryMaxPath != "" && !AcceptNewDirectoryRegex.IsMatch(newDirectoryMaxPath))
                {
                    PutNotCreateMessage();
                    return false;
                }

                // コピー先ディレクトリが許可されない形式なら拒否
                if (copyFromPathMaxPath != "" && !AcceptCopyFromPathDirectoryRegex.IsMatch(copyFromPathMaxPath))
                {
                    PutNotCreateMessage();
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 操作がADDかどうかを取得する。
        /// </summary>
        /// <param name="item">SvnChangeItem オブジェクト。</param>
        /// <returns>True:ADD, False:ADD以外。</returns>
        private static bool IsAddAction(SvnChangeItem item)
        {
            if (item.Action == SvnChangeAction.Add)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// tags ノードかどうかを取得する。
        /// </summary>
        /// <param name="item">SvnChangeItem オブジェクト。</param>
        /// <returns>True:tags ノード, False:tags ノードでない。</returns>
        private static bool IsTagsNode(SvnChangeItem item)
        {
            var tagsPos = item.Path.IndexOf(NodeName);

            // tags の位置よりtrunk, branchesが上位ディレクトリにある場合
            var paths = item.Path.Substring(0, tagsPos).Split('/');
            if (paths.Contains(TrunkHook.NodeName) || paths.Contains(BranchesHook.NodeName))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 新規作成される中間ディレクトリの最大パスを取得する。
        /// </summary>
        /// <param name="item">SvnChangeItem オブジェクト。</param>
        /// <param name="currentMaxPath">現在の最大パス。</param>
        /// <returns>最大パス。</returns>
        private static string GetNewDirectoryMaxPath(SvnChangeItem item, string currentMaxPath)
        {
            var result = currentMaxPath;

            // tags 作成先のディレクトリの場合は何もしない
            if (!string.IsNullOrEmpty(item.CopyFromPath))
            {
                return result;
            }

            if (item.NodeKind == SvnNodeKind.Directory && result.Length < item.Path.Length)
            {
                result = item.Path;
            }
            if (item.NodeKind == SvnNodeKind.File)
            {
                var directory = item.Path.Substring(0, item.Path.LastIndexOf('/'));
                if (result.Length < directory.Length)
                {
                    result = directory;
                }
            }
            return result;
        }

        /// <summary>
        /// tags 作成先の最大パスを取得する。
        /// </summary>
        /// <param name="item">SvnChangeItem オブジェクト。</param>
        /// <param name="currentMaxPath">現在の最大パス。</param>
        /// <returns>最大パス。</returns>
        private static string GetCopyFromPathMaxPath(SvnChangeItem item, string currentMaxPath)
        {
            var result = currentMaxPath;

            // 新規作成される中間ディレクトリの場合は何もしない
            if (string.IsNullOrEmpty(item.CopyFromPath))
            {
                return result;
            }

            if (item.NodeKind == SvnNodeKind.Directory && result.Length < item.Path.Length)
            {
                result = item.Path;
            }
            if (item.NodeKind == SvnNodeKind.File)
            {
                var directory = item.Path.Substring(0, item.Path.LastIndexOf('/'));
                if (result.Length < directory.Length)
                {
                    result = directory;
                }
            }
            return result;
        }

        /// <summary>
        /// 作成不可時のコンソールメッセージを出力する。
        /// </summary>
        private static void PutNotCreateMessage()
        {
            Console.WriteLine("----------------------------------------------------------");
            Console.WriteLine("タグ内容を変更することはできません。");
            Console.WriteLine("タグの作成は以下の形式にいずれかでなければなりません。");
            Console.WriteLine("/tags/v1.0/20180403001_タグ内容");
            Console.WriteLine("/tags/v1.0/prod|dev/20180403001_タグ内容");
            Console.WriteLine("/プロジェクト名/tags/v1.0/20180403001_タグ内容");
            Console.WriteLine("/プロジェクト名/tags/v1.0/prod|dev/20180403001_タグ内容");
            Console.WriteLine("----------------------------------------------------------");
        }
    }
}
