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
    /// branches 配下を操作するためのフックスクリプト。
    /// </summary>
    public class BranchesHook
    {
        public const string NodeName = @"branches";

        // brahcnes へ許可される新規ディレクトリパス
        private static readonly Regex AcceptCopyFromPathDirectoryRegex = new Regex(@"^.*/branches/v[0-9]+\.[0-9]+\.x/[0-9]{11}_[^/]+$", RegexOptions.Compiled);

        /// <summary>
        /// branches ディレクトリ配下
        /// </summary>
        /// <param name="ci">SvnChangeInfoEventArgs オブジェクト。</param>
        /// <returns>True:有効, False:無効。</returns>
        public static bool Validate(SvnChangeInfoEventArgs ci)
        {
            string copyFromPathMaxPath = "";

            // branches が存在するパスのみ処理
            foreach (SvnChangeItem item in ci.ChangedPaths.Where(s => s.Path.IndexOf(NodeName) >= 0))
            {
                // branches ノードでない場合は処理対象外
                if (!IsBranchesNode(item))
                {
                    continue;
                }

                // branches 作成先のディレクトリ
                copyFromPathMaxPath = GetCopyFromPathMaxPath(item, copyFromPathMaxPath);

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
        /// branches ノードかどうかを取得する。
        /// </summary>
        /// <param name="item">SvnChangeItem オブジェクト。</param>
        /// <returns>True:branches ノード, False:branches ノードでない。</returns>
        private static bool IsBranchesNode(SvnChangeItem item)
        {
            var branchesPos = item.Path.IndexOf(NodeName);

            // branches の位置よりtrunk, tagsが上位ディレクトリにある場合
            var paths = item.Path.Substring(0, branchesPos).Split('/');
            if (paths.Contains(TrunkHook.NodeName) || paths.Contains(TagsHook.NodeName))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// branches 作成先の最大パスを取得する。
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
            Console.WriteLine("ブランチの作成は以下の形式にいずれかでなければなりません。");
            Console.WriteLine("/branches/v1.0.x/20180403001_ブランチ内容");
            Console.WriteLine("/プロジェクト名/branches/v1.0.x/20180403001_ブランチ内容");
            Console.WriteLine("----------------------------------------------------------");
        }
    }
}
