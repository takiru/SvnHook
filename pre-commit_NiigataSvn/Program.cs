using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvnHook.PreCommit
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "test.log");

            using (var fw = new System.IO.StreamWriter(path))
            {
                try
                {
                    fw.WriteLine(System.Environment.CommandLine);

                    foreach (var str in args)
                    {
                        fw.WriteLine(str);
                    }

                    var svn_transaction_id = args[1];
                    var svn_repos = args[0];

                    SharpSvn.SvnHookArguments ha;
                    if (!SharpSvn.SvnHookArguments.ParseHookArguments(args, SharpSvn.SvnHookType.PreCommit, false, out ha))
                    {
                        Console.Error.WriteLine("Invalid arguments");
                        Environment.Exit(1);
                    }

                    // コミット前情報の取得
                    using (SharpSvn.SvnLookClient cl = new SharpSvn.SvnLookClient())
                    {
                        SharpSvn.SvnChangeInfoEventArgs ci;
                        cl.GetChangeInfo(ha.LookOrigin, out ci);

                        // メッセージ
                        fw.WriteLine(ci.LogMessage);

                        // tagsディレクトリを含む変更パス情報
                        foreach (SharpSvn.SvnChangeItem i in ci.ChangedPaths.Where(s => s.Path.IndexOf("tags") >= 0))
                        {
                            fw.WriteLine(i.Action + " " + i.Path);

                            var beforePathLevel = i.Path.Substring(0, i.Path.IndexOf("tags")).Split('/').Length;

                            // tags/タグ名/ファイル・フォルダ名だったら許可しない
                            if (beforePathLevel == 2)
                            {
                                fw.WriteLine("level 2");
                                if (i.Path.Substring(i.Path.IndexOf("tags")).Split('/').Length > 2)
                                {
                                    fw.Close();
                                    Console.WriteLine("tags直下への変更は許可されていません。");
                                    Environment.Exit(-1);
                                }
                            }

                            // project/tags/タグ名/ファイル・フォルダ名だったら許可しない
                            if (beforePathLevel == 3)
                            {
                                fw.WriteLine("level 3");
                                if (i.Path.Substring(i.Path.IndexOf("tags")).Split('/').Length > 2)
                                {
                                    fw.Close();
                                    Console.WriteLine("tags直下への変更は許可されていません。");
                                    Environment.Exit(-1);
                                }
                            }
                        }
                    }

                    //// コミット先は以下になる
                    //// trunk/directory
                    //// branches/directory
                    //// tags/directory
                    //// プロジェクト名/trunk/directory
                    //// プロジェクト名/trunk/directory
                    //// プロジェクト名/branches/directory
                    //// プロジェクト名/tags/directory

                    //// test2タグを作成したとき
                    //// A   TestProj/tags/test2/

                    //// test2タグ内をいじったとき
                    //// A   TestProj/tags/test2/新規 TXT ファイル (2).txt
                    //// U   TestProj/tags/test2/新規 TXT ファイル (2).txt
                    //// D   TestProj/tags/test2/新規 TXT ファイル.txt

                    //// よって、1階層目にtagsがある場合、A, Dのみ許可する。
                    //// 1階層目にtagsがなくて、2階層目にtagsがある場合、A, Dのみ許可する

                    //// A, Dが、tags/ディレクトリ名/hogehoge などと、ディレクトリ名より後ろが存在する場合は許可しない
                    //// (つまり、tagを切った後の操作である)








                    ////var cmd_get_comment = @"svnlook -t """ + svn_transaction_id + @""" log """ + svn_repos + @"""";
                    //var psi = new ProcessStartInfo("svnlook");
                    //psi.Arguments = @"-t """ + svn_transaction_id + @""" log """ + svn_repos + @"""";
                    //psi.CreateNoWindow = true;
                    //psi.UseShellExecute = false;
                    //psi.RedirectStandardOutput = true;
                    //psi.ErrorDialog = false;
                    //fw.WriteLine(psi.Arguments);

                    //var process = Process.Start(psi);
                    //process.WaitForExit();

                    //var result = process.StandardOutput.ReadToEnd();
                    //fw.WriteLine(result);


                    //psi = new ProcessStartInfo("svnlook");
                    //psi.Arguments = @"-t """ + svn_transaction_id + @""" changed """ + svn_repos + @"""";
                    //psi.CreateNoWindow = true;
                    //psi.UseShellExecute = false;
                    //psi.RedirectStandardOutput = true;
                    //psi.ErrorDialog = false;
                    //fw.WriteLine(psi.Arguments);

                    //process = Process.Start(psi);
                    //process.WaitForExit();

                    //fw.WriteLine("tagsだけ抽出");
                    //var result2 = process.StandardOutput.ReadToEnd().Split(new char[] { '\r', '\n' }).Where(s => s.IndexOf("tags") >= 0);
                    //foreach (var s in result2)
                    //{
                    //    fw.WriteLine(s);
                    //}






                    //psi = new ProcessStartInfo("svnlook");
                    //psi.Arguments = @"-t """ + svn_transaction_id + @""" dirs-changed """ + svn_repos + @"""";
                    //psi.CreateNoWindow = true;
                    //psi.UseShellExecute = false;
                    //psi.RedirectStandardOutput = true;
                    //psi.ErrorDialog = false;
                    //fw.WriteLine(psi.Arguments);

                    //process = Process.Start(psi);
                    //process.WaitForExit();

                    //result = process.StandardOutput.ReadToEnd();
                    //fw.WriteLine(result);
                    ////if (result.IndexOf("tags") >= 0)
                    ////{
                    ////    fw.Close();
                    ////    Environment.Exit(-1);
                    ////}

                    fw.Close();
                }
                catch
                {
                    fw.Close();
                }
            }


        }
    }
}