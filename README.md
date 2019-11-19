# ACT.Hojoring
[![Downloads](https://img.shields.io/github/downloads/anoyetta/ACT.Hojoring/total.svg)](https://github.com/anoyetta/ACT.Hojoring/releases)
[![License](https://img.shields.io/badge/license-BSD--3--Clause-blue.svg)](https://github.com/anoyetta/ACT.Hojoring/blob/master/LICENSE)

「補助輪」  
Advanced Combat Tracker の FFXIV向けプラグインの詰合せです。  
スペスペ・ウルスカ・TTSゆっくりをまとめて「補助輪」 という名称でリリースしています。  

#### [SpecialSpellTimer](https://github.com/anoyetta/ACT.Hojoring/wiki/SpecialSpellTimer)
通称「スペスペ」  
アビリティなどのリキャストを表示するためのプラグインです。基本的にログに対するトリガとして動作します。

#### [UltraScouter](https://github.com/anoyetta/ACT.Hojoring/wiki/UltraScouter)
通称「ウルスカ」  
ターゲットのHPや距離、周囲のモブの検知などを行うプラグインです。リアルタイムにメモリ情報を読み取って表示などを行います。

#### [TTSYukkuri](https://github.com/anoyetta/ACT.Hojoring/wiki/Yukkuri)
通称「ゆっくり」  
ACT本体のTTS機能をゆっくり実況などで有名な AquesTalk&trade; などに置き換えます。ほぼすべてのTTSエンジンを網羅しています。またTTSをDISCORD BOTを経由してDISCORDのボイスチャットとして通知することもできます。

## 最新リリース
### **[DOWNLOAD Lastest-Release](https://github.com/anoyetta/ACT.Hojoring/releases/latest)**
[pre-release](https://github.com/anoyetta/ACT.Hojoring/releases)

## インストール
### マニュアルインストール
1. 各種ランタイムをインストールする  
**[Visual Studio 2017 用 Microsoft Visual C++ 再頒布可能パッケージ](https://go.microsoft.com/fwlink/?LinkId=746572)**  
**[.NET Framework 4.7.2](https://www.microsoft.com/net/download/thank-you/net472)**  
をインストールする。

2. 最新版を取得する  
[最新リリース](https://github.com/anoyetta/ACT.Hojoring/releases/latest) からダウンロードします。

3. 解凍する  
ダウンロードしたプラグイン一式を解凍し任意のフォルダに配置します。

4. ACTに追加する  
ACTにプラグインとして追加します。3つのプラグインそれぞれを登録します。  
必要なものだけ登録してください。もちろんすべて登録しても問題ありません。  
    * ACT.SpecialSpellTimer.dll
    * ACT.UltraScouter.dll
    * ACT.TTSYukkuri.dll

詳細な手順は **[anoyetta の開発記録 - ACTおよび補助輪のインストール手順（完全版）](https://www.anoyetta.com/entry/2019/04/08/132603)** を読んでください。

### 動作環境
* [Windows 10](https://www.microsoft.com/software-download/windows10) 以降（Windows 7/8/8.1 では原則的に動作しません）
* .NET Framework 4.7.1 以降

## 使い方
**基本的に [Wiki](https://github.com/anoyetta/ACT.Hojoring/wiki) を見てください。**

#### おすすめ設定とか教えて
下記でサンプルなどを共有しています。   
* **[ACT共有フォルダ](https://drive.google.com/drive/folders/1nPBuqd8z3jzk0RDOx6NqK6NYUoy08_k-?usp=sharing)**  
[ACT共有フォルダの使い方](https://drive.google.com/open?id=1dl4dMoBONNz-NRZLCqU7YbkmkEpLKAllN92U-SCwY3M) **[必読]**  

* **[スペスペたいむ共有レポジトリ](https://github.com/anoyetta/spespetime)**

##### Hojoring の設定ファイルあれこれ
1. 格納場所  
```
%APPDATA%\anoyetta\ACT
```  
にすべて格納されています。よって、OSの再インストール等のときはこのディレクトリを丸ごとバックアップして再インストール後に配置しなおせば、再インストール前の設定を復元できます。

2. 設定ファイル  
```
ACT.SpecialSpellTimer.config
ACT.TTSYukkuri.config
ACT.UltraScouter.config
```
それぞれ、スペスペ・ゆっくり・ウルスカの設定ファイルです。  
スペスペではトリガ以外のオプション関係の設定が保存されています。他のプラグインではプラグインそのものの設定が保存されています。  

3. スペスペのトリガの設定ファイル  
```
ACT.SpecialSpellTimer.Panels.xml
ACT.SpecialSpellTimer.Spells.xml
ACT.SpecialSpellTimer.Telops.xml
ACT.SpecialSpellTimer.Tags.xml
```  
スペスペの各種トリガの設定ファイルです。それぞれ、スペルパネル・スペル・テロップ・タグの設定ファイルになります。

## アップデート
Help タブからアップデートしてください。  

![how_to_update](https://github.com/anoyetta/ACT.Hojoring/blob/master/images/how_to_update.png?raw=true)

自分で独自に編集しているから上書きしたくないファイルがある場合は **update_hojoring.ps1** スクリプトを編集してください。
```powershell
# update_hojoring.ps1

# 更新の除外リスト
$updateExclude = @(
    "_dummy.txt",
    "_sample.txt"
)
```

## ライセンス
[3-Clause BSD License](LICENSE)  
&copy; 2014-2018 anoyetta  

ただし下記の行為を禁止します。
* 配布されたバイナリに対してリバースエンジニアリング等を行い内部を解析する行為
* 配布されたバイナリのすべてもしくは一部を本来の目的とは異なる目的に使用する行為

## お問合わせ
### なんかエラー出た
開発者に質問する場合は、下記の情報を添えてください。
* ACT本体のログファイル  
* 当プラグインのログファイル
* （あれば）エラーダイアログのスクリーンショット

##### [Help] → [サポート情報を保存する] から必要な情報一式を保存できます。
![help](https://github.com/anoyetta/ACT.Hojoring/blob/master/images/help.png?raw=true)

##### 起動できないなどUIから取得できない場合は下記のフォルダから収集してください。
```
%APPDATA%\Advanced Combat Tracker\Advanced Combat Tracker.log
%APPDATA%\anoyetta\ACT\logs\ACT.Hojoring.YYYY-MM-DD.log
```

### スペルが動かない
前述の情報に以下の情報も追加で必要になります。  
* 引っ掛けたい対象のログ
* 対象のスペルやテロップの設定

これらの情報がない場合は回答できません。

### まったく分かっていない
リモートでサポートすることも出来ます。  
下記のいずれかのリモート機能を使用して [DISCORD](https://discord.gg/n6Mut3F) 経由でリモートサポートを依頼してください。  
* [TeamViewer](https://www.teamviewer.com/ja/)
* [Google リモートデスクトップ](https://support.google.com/chrome/answer/1649523?co=GENIE.Platform%3DDesktop&hl=ja)  
* [クイックアシスト機能](https://support.microsoft.com/ja-jp/help/27919/windows-10-get-help-with-pc-problems)

また、リモートサポートの依頼ではなくても説明が困難な場合はリモートによる設定を案内する場合があります。その場合は案内に従ってください。

### 問合せフォーム
**[New Issue](https://github.com/anoyetta/ACT.Hojoring/issues/new)**  
からチケットを登録してください。[issues](https://github.com/anoyetta/ACT.Hojoring/issues) から既存の課題、現在の状況を確認出来ます。  
重複する質問はご遠慮ください。

### 連絡先
discord:  [Hojoring Forum](https://discord.gg/n6Mut3F)  
mail:     anoyetta(at)gmail.com  
twitter:  [@anoyetta](https://twitter.com/anoyetta)  
まで。  
基本的に issues からお願いします。issues の課題から優先的に対応します。  
どうしても直接連絡したい場合はなるべく discord を使用してください。 

### どこに振り込んだらいいんだ？
無償で提供しています。  
どうしても寄付したい方は anoyetta(at)gmail.com 宛てに  
**[Amazonギフト券（Eメールタイプ）](https://www.amazon.co.jp/dp/BT00DHI8G4)**  
を送ってください。  
大好きな コカ・コーラ ゼロ を買って開発時に役立てます。

### DONATION
I can receive only  **[Amazon eGift Card](https://www.amazon.com/dp/B004LLIKVU)**   
sendto: anoyetta(at)gmail.com

---
記載されている会社名・製品名・システム名などは、各社の商標、または登録商標です。  
Copyright &copy; 2010 - 2018 SQUARE ENIX CO., LTD. All Rights Reserved.
