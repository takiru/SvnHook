using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpSvn;
using System.Text.RegularExpressions;

namespace SvnHook.PreCommit
{
    /// <summary>
    /// pre-commitの制御を行います。
    /// </summary>
    class PreCommit
    {
        private SvnHookArguments hookArgs;
        private const string Trunk = @"trunk";
        private const string Branches = @"branches";
        private const string Tags = @"tags";

        private readonly Regex TagUriRegex = new Regex(@"(^.*/tags/v[0-9]+\.[0-9]+$)|(^.*/tags/v[0-9]+\.[0-9]+/[0-9]{11}_[^/]+$)", RegexOptions.Compiled);
        private readonly Regex BranchesUriRegex = new Regex(@"(^.*/branches/v[0-9]+\.[0-9]+\.x$)|(^.*/branches/v[0-9]+\.[0-9]+\.x/[0-9]{11}_[^/]+$)", RegexOptions.Compiled);

        /// <summary>
        /// pre-commitのロードを行います。
        /// </summary>
        /// <param name="args">呼出パラメーター。</param>
        /// <returns>true:成功, false:失敗。</returns>
        public bool Load(string[] args)
        {
            if (!SvnHookArguments.ParseHookArguments(args, SvnHookType.PreCommit, false, out hookArgs))
            {
                Console.Error.WriteLine("パラメーターが不正です。");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 検証を実施します。
        /// </summary>
        /// <returns>true:コミット可, false:コミット不可。</returns>
        public bool Validate()
        {
            using (SvnLookClient cl = new SvnLookClient())
            {
                SvnChangeInfoEventArgs ci;
                cl.GetChangeInfo(hookArgs.LookOrigin, out ci);

                // ログメッセージに関する検証
                if (!ValidateOfLogMessage(ci))
                {
                    return false;
                }

                // tagsに関する検証
                if (!ValidateOfTagsNode(ci))
                {
                    return false;
                }

                // branchesに関する検証
                if (!ValidateOfBranchesNode(ci))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// ログメッセージに関する検証を行う。
        /// </summary>
        /// <param name="ci">SvnChangeInfoEventArgs オブジェクト。</param>
        /// <returns>true:コミット可, false:コミット不可。</returns>
        private bool ValidateOfLogMessage(SvnChangeInfoEventArgs ci)
        {
            // 未入力は拒否する
            if (string.IsNullOrEmpty(ci.LogMessage.Trim()))
            {
                Console.WriteLine("ログメッセージが未入力です。");
                return false;
            }
            return true;
        }

        /// <summary>
        /// tagsに関する検証を行う。
        /// </summary>
        /// <param name="ci">SvnChangeInfoEventArgs オブジェクト。</param>
        /// <returns>true:コミット可, false:コミット不可。</returns>
        private bool ValidateOfTagsNode(SvnChangeInfoEventArgs ci)
        {
            foreach (SvnChangeItem item in ci.ChangedPaths.Where(s => s.Path.IndexOf(Tags) >= 0))
            {
                var tagsPos = item.Path.IndexOf(Tags);

                // tagsの位置が以下でない場合は対象外
                // ・リポジトリ名/tags
                // ・リポジトリ名/プロジェクト名/tags
                // tagsより前にtrunkやbranchesがあるなら、それはtagsではない
                var paths = item.Path.Substring(0, tagsPos).Split('/');
                if (paths.Contains(Trunk) || paths.Contains(Branches))
                {
                    continue;
                }
                var pathLevel = paths.Length;
                if (pathLevel != 2 && pathLevel != 3)
                {
                    continue;
                }

                // ADD以外は許可しない
                if (item.Action != SvnChangeAction.Add)
                {
                    Console.WriteLine("tags配下は追加以外の操作はできません。");
                    return false;
                }

                // tagsをチェックアウトしてコミットした時(タグを切った時は制御させない)
                if (string.IsNullOrWhiteSpace(item.CopyFromPath))
                {
                    /// 以下構成に対する変更が行われたら許可しない。
                    /// ・リポジトリ名/tags/バージョン/タグ名/ファイル・フォルダ名
                    /// ・リポジトリ名/プロジェクト名/tags/バージョン/タグ名/ファイル・フォルダ名
                    if (item.Path.Substring(tagsPos).Split('/').Length > 2)
                    {
                        Console.WriteLine("tagsへの変更は行えません。");
                        return false;
                    }
                }

                // 許可されるパスは以下のみ
                // ・/リポジトリ名/tags/v3.0
                // ・/リポジトリ名/tags/v3.0/20180403001_タグ内容
                // ・/リポジトリ名/プロジェクト名/tags/v3.0
                // ・/リポジトリ名/プロジェクト名/tags/v3.0/20180403001_タグ内容
                if (!TagUriRegex.IsMatch(item.Path))
                {
                    Console.WriteLine("タグの作成は以下の形式のいずれかでなければなりません。");
                    Console.WriteLine("/リポジトリ/tags/v1.0/20180403001_タグ名");
                    Console.WriteLine("/リポジトリ/プロジェクト/tags/v1.0/20180403001_タグ名");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// branchesに関する検証を行う。
        /// </summary>
        /// <param name="ci">SvnChangeInfoEventArgs オブジェクト。</param>
        /// <returns>true:コミット可, false:コミット不可。</returns>
        private bool ValidateOfBranchesNode(SvnChangeInfoEventArgs ci)
        {
            foreach (SvnChangeItem item in ci.ChangedPaths.Where(s => s.Path.IndexOf(Branches) >= 0))
            {
                var branchesPos = item.Path.IndexOf(Branches);

                // branchesの位置が以下でない場合は対象外
                // ・リポジトリ名/branches
                // ・リポジトリ名/プロジェクト名/branches
                // tagsより前にtrunkがあるなら、それはbranchesではない
                var paths = item.Path.Substring(0, branchesPos).Split('/');
                if (paths.Contains(Trunk))
                {
                    continue;
                }
                var pathLevel = paths.Length;
                if (pathLevel != 2 && pathLevel != 3)
                {
                    continue;
                }

                // branchesをチェックアウトしてコミットした時(ブランチを切った時は制御させない)は常にOK
                if (string.IsNullOrWhiteSpace(item.CopyFromPath))
                {
                    continue;
                }

                // 許可されるパスは以下のみ
                // ・/リポジトリ名/branches/v3.0.x
                // ・/リポジトリ名/branches/v3.0.x/20180403001_タグ内容
                // ・/リポジトリ名/プロジェクト名/branches/v3.0.x
                // ・/リポジトリ名/プロジェクト名/branches/v3.0.x/20180403001_タグ内容
                if (!BranchesUriRegex.IsMatch(item.Path))
                {
                    Console.WriteLine("ブランチの作成は以下の形式のいずれかでなければなりません。");
                    Console.WriteLine("/リポジトリ/branches/v1.0.x/20180403001_ブランチ名");
                    Console.WriteLine("/リポジトリ/プロジェクト/branches/v1.0.x/20180403001_ブランチ名");
                    return false;
                }
            }
            return true;
        }
    }
}
