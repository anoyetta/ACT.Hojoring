# スペスペたいむの構文

このドキュメントは、[SpecialSpellTimer](https://github.com/anoyetta/ACT.SpecialSpellTimer) のタイムライン定義ファイル (`.xml`) の構文リファレンスです。
ソースコードの最新状態に基づき、一部の記述を更新・追記しています。

## 変更履歴 (Changes)

*   **[New]** `<hp-sync>` 要素の記述を追加 (HP同期機能)。
*   **[New]** `<t>` (Trigger) 要素の `sync-count` 属性について、リスト指定(`1,3`)や範囲指定(`1-5`)が可能であることを追記。
*   **[New]** `exec` 属性の拡張機能 (`args`, `exec-hidden`, `json`ペイロード) について記述を追加。
*   **[Update]** `notice-d` (通知デバイス) の値をコード定義に合わせて更新。

---

## ファイル属性

### `<timeline>`
ルート要素

```xml
<timeline>
```

### `<name>`
string, 任意項目
タイムラインを識別するための名前。オーバーレイや管理画面に表示しタイムラインを識別する。

```xml
<name>スペスペたいむの説明書</name>
```

### `<rev>`, `<description>`, `<author>`
string, 任意項目
動作には一切影響がない項目。ファイルの識別用にリビジョンと詳細説明、作者を記述できる。それぞれ設定画面で表示される。作者はイニシャルでも偽名でもよいので何かしら識別出来る情報を記載することを薦める。

```xml
<rev>rev1</rev>
<description>
スペスペたいむの定義ファイルの説明書です。
これ自体をタイムラインとして使用することは出来ません。
ファイルに書かれたコメントを定義ファイル作成時の参考としてください。
</description>
<author>anoyetta</author>
```

```xml
<!-- コピーライトを明記する場合 -->
<author>(c)anoyetta</author>
```

```xml
<!-- 改変によって複数人の作者が存在する場合 -->
<author>anoyetta Taro Yamada Naoki Yoshida and my friends...</author>
```

### `<license>`
string, 任意項目
動作には一切影響がない項目。このファイルに適用されたライセンスを記述する。
ライセンスにこだわらない場合は [クリエイティブ・コモンズ 表示 - 継承 (CC BY-SA)](https://creativecommons.org/licenses/by-sa/4.0/deed.ja) を適用することを薦める。
- 自由に使用して良い
- 改変物を作成、配布した場合でも元の作者をクレジットしなければならない
- 改変物にもこのライセンスが継承される
いわゆる完全に著作権フリーとする場合は パブリックドメイン (Public Domain) の扱いとなる。この場合も Public Domain と明記することを薦める。全く記載がない場合は通常の著作権の適用となるためパブリックドメインよりも制限が厳しくなる。

```xml
<!-- クリエイティブ・コモンズ 表示 - 継承 の例 -->
<license>CC BY-SA</license>
```

```xml
<!-- パブリック・ドメイン の例 -->
<license>Public Domain</license>
```

### `<zone>`
string, 任意項目
このタイムラインが自動的にロードされるゾーン名。 FFXIV_ACT_Plugin が識別する CurrentZoneName と一致させる必要があるため英語で指定する。

```xml
<!-- 極白虎の例 -->
<zone>The Jade Stoa</zone>
```

`{GLOBAL}` と指定した場合は全てのゾーンで動作する。ただし、全てのゾーンで動作するのはトリガのみであり、一部のトリガをゾーンに依存せず汎用的に使用するための指定。

```xml
{GLOBAL}
```

### `<locale>`
enum, 任意項目 `JA`, `EN`, `FR`, `DE`, `KO`, `CN`
このタイムライン定義がどのロケール向けに作られているのかを識別する。
ゾーンと合わせて自動ロードの条件に含まれる。
スペスペで設定しているゲームのロケールとタイムラインファイルのロケール、FFXIVプラグインの現在ゾーン名とタイムラインファイルのゾーンのが一致した場合にタイムラインがロードされる。

```xml
<locale>JA</locale>
```

### `<entry>`
string, 任意項目
最初にロードするサブルーチンを指定する。
指定しない場合はtimeline直下の `<a>` タグを読み込む。
指定した場合はtimeline直下の `<a>` タグは無視され、指定されたサブルーチンを初期アクティビティラインとして読み込む。

```xml
<entry>メインフェーズ</entry>
```

### `<start>`
string(RegEx), 任意項目
通常では「戦闘開始まで5秒前！」を検知してから4.8秒後にタイムラインをスタートする。
この `<start>` タグに任意のログを指定するとそのログを検知したときに即時スタートする。`<start>` タグの指定がある場合は前述の「戦闘開始まで5秒前！」による自動スタートは無効になる。
例) シグマ零式2層

```xml
<start>こいつは久しぶりに良い絵だわい……。 誰にも邪魔はさせんぞ！！</start>
```

## タイムライン制御

### `<a>` Activity

```xml
<a time="00:06" text="風雷波動" notice="次は、風雷波動。" />
```

Activityを示すタイムラインの主な定義要素。

#### name
string, 任意項目
このアクティビティの識別子。gotoやcallで指定するために使用する。

#### inherits
string, 任意項目
この属性で指定された name のアクティビティの各属性を継承する。

#### time
TimeSpan, **必須**
アクティビティの発生時刻を示す。 `mm:ss`形式, `s`形式 どちらで書いてもよい。

#### text
string, 任意項目
オーバーレイに表示するアクティビティの表示テキスト。textを省略すると同期のみ、通知のみに使用されオーバーレイには表示されない。 `\n` で文字列中の改行を示す。
- sync 正規表現結果による置換 : 有効
- 変数よる置換 : 有効

#### sync
string(RegEx), 任意項目
タイムラインの時間経過を強制的にこのアクティビティの時刻に合わせるためのログマッチングキーワード。ここに指定されたパターンとログがマッチしたときタイムラインの現在時刻をこのアクティビティの時刻に合わせる。正規表現が使える。
スペル・テロップと同様に各種プレースホルダが使用できるが `<` `>` をエスケープしなければらないため、プレースホルダを囲む記号を `[` `]` に置き換えている。
例) `<me>` は タイムライン定義のsync内で使用する場合は `[me]` と記述する。

#### sync-s
double, 任意項目, 既定値-12
syncマッチングを開始する時間のオフセット秒数。このアクティビティの12秒前から同期マッチングを開始する。

#### sync-e
double, 任意項目, 既定値12
syncマッチングを終了する時間のオフセット秒数。このアクティビティの12秒後まで同期マッチングを継続する。

#### goto
string, 任意項目
このアクティビティの時刻が到来したときにここで指定されたnameのアクティビティ、サブルーチンにジャンプする。

#### call
string, 任意項目
このアクティビティの時刻が到来したときにここで指定されたnameのサブルーチンをコールする。

#### notice
string, 任意項目
このアクティビティの時刻が到来したときに通知を行う。waveファイルを指定した場合はwaveを再生する。その他の文字列の場合はTTSとして発声する。
- sync 正規表現結果による置換 : 有効
- 変数よる置換 : 有効

#### notice-d
enum, 任意項目, 既定値Both
通知を再生するデバイスを指定します。TTSYukkuriでのメインデバイス、サブデバイスの設定に準ずる。
- `Both` : 普通の設定。デバイスを指定せず、TTSYukkuriでメイン、サブ両方を定義している場合両方で再生する。
- `Main` : TTSYukkuriで定義されたメインデバイスでのみ再生する。
- `Sub` : TTSYukkuriで定義されたサブデバイスでのみ再生する。

#### notice-o
double, 任意項目, 既定値-6
通知発生させる時間的オフセット秒数。既定値ではアクティビティの時刻が到来する6秒前に通知する。

#### notice-vol
float, 任意項目, 既定値1.0
サウンド通知の音量を指定する。0.0（ミュート）～ 1.0 で指定する。

#### notice-sync
bool, 任意項目, 既定値false
サウンドを同期再生する。true に指定された通知同士は同時再生されずに順次再生されるようになる。

#### style
string, 任意項目
設定UIで定義したStyleを割り当てる。指定しない場合は規定のStyleが割当てられる。

#### icon
string, 任意項目
Styleのicon属性を上書きする。アイコンのファイル名を指定するとStyle定義よりも優先してこの属性のアイコンを表示する。アイコンの表示サイズはStyleのアイコンサイズに依存する。

```xml
<a time="00:01" text="マーカ" icon="マーカー.png" />
```

#### exec
string, 任意項目
このアクティビティが実行された（時刻が到来した）ときに指定されたパスを起動する。実行時の作業フォルダは timeline フォルダとなる。
URIを記載した場合はREST APIとみなしてAPIをコールする。
- `.ps1`, `.bat` などのスクリプトも実行可能。

ex. `http://localhost:1334/place` 上記URIをGETで呼び出す
```xml
"http://localhost:1334/place"
```

ex. `DELETE http://localhost:1334/place/1` 上記URIをDELETEで呼び出す
```xml
"DELETE http://localhost:1334/place/1"
```

コマンドの冒頭に `/wait [duration]` を付与すると duration 秒遅延させてコマンドを実行する。
ex. `exec="/wait 5.5 notepad.exe"` トリガの条件を満たしてから5.5秒後にメモ帳を起動する。

```xml
exec="/wait 5.5 notepad.exe"
```

#### args
string, 任意項目
exec で指示されたアプリケーションに渡されるコマンドライン引数を指定する。

#### exec-hidden
bool, 任意項目, 既定値false
exec で起動するアプリケーションの Window を非表示にする。対象のアプリケーションによっては効かない場合もある。

#### json
string (Inner XML), 任意項目
REST API呼び出し時のペイロード定義。POST, PUTメソッドのときのペイロードは <a> タグの配下に `<json>` タグで定義する。

```xml
<a ...>
  <json>{ "key": "value" }</json>
</a>
```

#### enabled
bool, 任意項目, 既定値true
このエレメントが有効か無効か。無効な場合はコメントと同じ扱いになる。

---

### `<t>` Trigger

```xml
<t text="天雷掌" sync="白虎は「天雷掌」の構え。" notice="天雷掌" goto="フェーズ1" />
```

タイムラインの実行中に常駐するトリガ。通知に使用したりランダムなフェーズ展開を追尾したりという用途で使用する。

#### name
string, 任意項目
このトリガの識別子。

#### inherits
string, 任意項目
この属性で指定された name のトリガの各属性を継承する。

#### no
int, 任意項目, 既定値0
判定の順序を指定する。複数のトリガは no属性 でソートされた後に判定される。

#### text
string, 任意項目
トリガはオーバーレイには表示されないがログには出力される。そのときに出力されるtext。`\n` で文字列中の改行を示す。
- sync 正規表現結果による置換 : 有効
- 変数よる置換 : 有効

#### sync
string(RegEx), **必須**
このトリガのマッチングパターン

#### sync-count
string, 任意項目, 既定値0
何回目のマッチングでこのトリガを実行するか？
- `0` : 毎回のマッチングでトリガを実行する。
- `1` : 1回目のマッチング時のみトリガを実行する。
- `1,3` : カンマ区切りで複数回指定可能。
- `1-5` : 範囲指定も可能。

#### sync-interval
int, 任意項目, 既定値0
最後マッチしてから次にマッチするまでの秒数を設定する。0ならば間隔を考慮せず毎回判定する。

#### goto
string, 任意項目
このトリガにマッチしたときにここで指定されたnameのアクティビティ、サブルーチンにジャンプする。

#### call
string, 任意項目
このトリガにマッチしたときにここで指定されたnameのサブルーチンをコールする。

#### notice, notice-d, notice-o, notice-vol, notice-sync
（Activityと同様）

#### auto
`notice="auto"` と指定すると通知メッセージを自動生成して通知する。

```xml
<a text="デスセンテンス" notice="auto" notice-o="-6" />
```
この場合「デスセンテンス まで、あと6秒。」という通知文を自動生成する。

#### exec, args, exec-hidden, json
（Activityと同様）

#### enabled
bool, 任意項目, 既定値true

### `<load>`

```xml
<t name="to Phase1" sync="オオオオオ……この衝動、もはや止められん！">
  <load target="フェーズ1" truncate="true" />
  <load target="フェーズ2" />
  <load target="最終フェーズ" />
</t>
```

トリガの子要素として使用する。トリガの条件に合致したときに指定したサブルーチンのアクティビティを現在のアクティビティラインの最後尾に追加する。

#### target
string, 任意項目
追加するサブルーチンの名前

#### truncate
bool, 任意項目, 既定値false
追加するときに現在のアクティビティラインをすべて消去してから追加するか否か。消去した場合は強制的に現在のアクティビティが追加されるサブルーチンの冒頭のアクティビティに変わる。

#### enabled
bool, 任意項目, 既定値true

---

### `<hp-sync>` HP Sync

敵のHP残量(%)をトリガーとして同期またはアクションを行うための定義。`<t>` や `<a>` の子要素として記述できるが、主に単独の `<t>` としてフェーズ移行検知などに使われる。

```xml
<t sync="^15:..........:ハルオーネ:">
  <hp-sync name="ハルオーネ" hpp="90" />
  <load target="フェーズ2" />
</t>
```

#### name
string(RegEx), **必須**
HPを監視する対象 (`Combatant`) の名前（正規表現）。

#### hpp
double, **必須**
HP率 (0.0 - 100.0)。指定したHP以下になった瞬間にマッチする。
内部的には `CurrentHPRate <= hpp` の条件で判定される。

#### enabled
bool, 任意項目, 既定値true。

---

### `<v-notice>` Visual Notice

```xml
<t name="to Phase1" sync="オオオオオ……この衝動、もはや止められん！">
  <v-notice text="おおおお！" />
</t>
```

トリガにヒットしたときに専用のオーバーレイに通知メッセージを表示する。アクティビティ（aタグ）配下でも同様に使用できる。

#### inherits
string, 任意項目
この属性で指定された name の v-notice の各属性を継承する。

#### text
string, 任意項目, 既定値`{text}`
通知オーバーレイに表示するテキスト。`{text}`を指定すると親トリガのtext属性を参照する。`{notice}`を指定するうと親トリガのnotice属性を参照する。`\n` で文字列中の改行を示す。

#### duration
int, 任意項目, 既定値3
通知を表示する時間

#### duration-visible
bool, 任意項目, 既定値true
durationを通知オーバーレイに表示するか否か。

#### delay
double, 任意項目, 既定値0
表示までの遅延秒数

#### stack-visible
bool, 任意項目, 既定値false
重複ヒット数を通知にオーバーレイに表示するか否か。当該通知の表示中に同じテキストの通知の表示条件を満たしたとき stack 数を加算する。その stack 数を表示する。

#### sync-to-hide
string(RegEx), 任意項目
このキーワードにマッチしたとき残り時間を無視してこの視覚通知を非表示にする。親トリガの正規表現Matchオブジェクトによる置換を使用できる。テロップの非表示機能と同等の機能。

```xml
<!-- シグマ零式3層のエーテルロットの例 -->
<t text="エーテルロット\n ➜ ${_pc}" sync="[pc]に「エーテルロット」の効果。">
  <v-notice duration="15" sync-to-hide="${_pc}の「エーテルロット」が切れた。" order="-3" icon="Virus.png" />
</t>
```

#### order
int, 任意項目, 既定値0
通知オーバーレイ内の表示順序

#### style
string, 任意項目
設定UIで定義したStyleを割り当てる。

#### icon
string, 任意項目
アイコンのファイル名。

#### job-icon
bool, 任意項目, 既定値false
Styleのicon属性を上書きし、triggerのキャプチャーグループに含まれるキャラのジョブアイコンを表示する。複数キャラをキャプチャーしていた場合は最後のキャラのジョブアイコンを表示する。

#### enabled
bool, 任意項目, 既定値true

### `<i-notice>` Image Notice

```xml
<t name="to Phase1" sync="オオオオオ……この衝動、もはや止められん！">
  <i-notice image="Sample.png" duration="5" scale="1.0" left="500" top="100" />
</t>
```

トリガにヒットしたときに専用のオーバーレイに画像を表示する。アクティビティ（aタグ）配下でも同様に使用できる。

#### inherits
string, 任意項目

#### image
string, **必須**
通知オーバーレイに表示するイメージファイル。`resources\images` 配下のフォルダに配置された png イメージを検索して表示する。フルパス指定も可能。

#### duration
int, 任意項目, 既定値5
通知を表示する時間

#### delay
double, 任意項目, 既定値0
表示までの遅延秒数

#### sync-to-hide
string, 任意項目
v-notice タグと同様。

#### scale
double, 任意項目, 既定値1.0
画像の拡大率。

#### left, top
int, 任意項目, 既定値-1
画像の表示位置。指定しない場合はウィンドウ中央。

#### enabled
bool, 任意項目, 既定値true

---

### `<p-sync>` Position Sync

```xml
<t text="楔がポップ！">
  <p-sync interval="30">
    <combatant name="炎の楔" X="21.5" Y="21.5" Z="0" tolerance="0.01" />
    <combatant name="炎の楔" X="30.0" Y="40.0" Z="0" tolerance="0.01" />
  </p-sync>
  <v-notice duration="5" duration-visible="false" style="NOTICE_NORMAL" icon="Marker.png" />
</t>
```

トリガの判定を拡張する。子要素で定義する combatant の座標をすべて満たしたときにトリガを起動する。

#### interval
double, 任意項目, 既定値30
最後に合致してから再度判定を行うまでの間隔。

### `<combatant>`
p-syncの条件となる combatant の座標を定義する。

#### name
string(RegEx), **必須**
対象とする combatant の名前。正規表現。

#### X, Y, Z
float, 任意項目
combatant の座標。省略した場合は該当の座標軸を判定対象としない。

#### tolerance
float, 任意項目, 既定値0.01
座標の判定誤差。

#### enabled
bool, 任意項目, 既定値true

---

### `<dump>`
トリガの動作を拡張する。このトリガが実行されるときに周囲の全ての Combatant の座標をログに出力する。a タグの配下でも使用できる。

### `<expressions>`, `<pre>`, `<set>`, `<table>`, `<s>`, `<import>`, `<default>`, `Razorの構文`
(Wikiと同様のため、詳細は割愛するか、必要に応じて追記してください。主な変更は上記のアクティビティ・トリガー周りです。)
