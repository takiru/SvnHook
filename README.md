# 全般
1. ログメッセージの入力を強制させる。

# branches
1. 以下形式のみブランチ作成を認める。
    - /repository/branches/v1.0.x/20180403001_ブランチ名
    - /repository/プロジェクト名/branches/v1.0.x/20180403001_ブランチ名
 
# tags
1. 以下形式のみタグ作成を認める。
    - /repository/tags/v1.0/20180403001_タグ名
    - /repository/tags/v1.0/prod|dev/20180403001_タグ名
    - /repository/プロジェクト名/tags/v1.0/20180403001_タグ名
    - /repository/プロジェクト名/tags/v1.0/prod|dev/20180403001_タグ名

2. tags配下は一切変更不可とする。
