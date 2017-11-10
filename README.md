# ACT.Hojoring
「補助輪」  
Advanced Combat Tracker の FFXIV向けプラグインの詰合せです。  
スペスペ・ウルスカ・TTSゆっくりをまとめたものです。これ自体が独立したプラグインではありません。

## 最新リリース
**[Lastest-release](https://github.com/anoyetta/ACT.Hojoring/releases/latest)**  
[pre-releease](https://github.com/anoyetta/ACT.Hojoring/releases)

## インストール
1. 各種ランタイムをインストールする  
**[Visual Studio 2017 用 Microsoft Visual C++ 再頒布可能パッケージ](https://go.microsoft.com/fwlink/?LinkId=746572)**  
**[Microsoft .NET Framework 4.7](https://www.microsoft.com/en-us/download/details.aspx?id=55170)**  
をインストールする。


3. 最新版を取得する  
最新リリースからダウンロードします。

4. 解凍する  
ダウンロードしたプラグイン一式を解凍し任意のフォルダに配置します。  
Discord への通知を使用する場合は、lib\libopus.dll, lib\libsodium.dll を **ACT本体と同じフォルダ** にコピーします。

5. ACTに追加する
ACTにプラグインとして追加します。3つのプラグインそれぞれを登録します。  
必要なものだけ登録してください。もちろんすべて登録しても問題ありません。  

  * ACT.SpecialSpellTimer.dll
  * ACT.UltraScouter.dll
  * ACT.TTSYukkuri.dll

## 使い方
**[Wiki](https://github.com/anoyetta/ACT.Hojoring/wiki)** を見てください。

## ライセンス
Copyright(C) 2017, anoyetta all rights reserved.  
[3-clause BSD license](LICENSE)  

ただし下記の行為を禁止します。
* 配布されたバイナリに対してリバースエンジニアリング等を行い内部を解析する行為
* 配布されたバイナリのすべてもしくは一部を本来の目的とは異なる目的に使用する行為

## お問合わせ
### なんかエラー出た
作者に尋ねる場合は、下記の情報を添えてください。
* ACT本体のログファイル  
* 当プラグインのログファイル
* （あれば）エラーダイアログのスクリーンショット

[Help] → [サポート情報を保存する] から必要な情報一式を保存できます。起動できないなどUIから取得できない場合は下記のフォルダから収集してください。  
%APPDATA%\Advanced Combat Tracker\Advanced Combat Tracker.log  
%APPDATA%\anoyetta\ACT\logs\ACT.Hojoring.YYYY-MM-DD.log  

### スペルが動かない
前述の情報に以下の情報も追加で必要になります。  
* 引っ掛けたい対象のログ
* 対象のスペルやテロップの設定

これらの情報がない場合は回答できません。

### 問合せフォーム
[https://github.com/anoyetta/ACT.Hojoring/issues/new](https://github.com/anoyetta/ACT.Hojoring/issues/new)  
からチケットを登録してください。[issues](https://github.com/anoyetta/ACT.Hojoring/issues) から既存の課題、現在の状況を確認出来ます。  
重複する質問はご遠慮ください。

### 連絡先
discord:  [Hojoring Forum](https://discord.gg/n6Mut3F)  
mail:     anoyetta(at)gmail.com  
twitter:  [@anoyetta](https://twitter.com/anoyetta)  
まで。  
基本的に issues からお願いします。issues の課題から優先的に対応します。  
どうしても直接連絡したい場合はなるべく discord を使用してください。 
