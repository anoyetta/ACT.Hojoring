# スペスペたいむの構文 (Updated Reference)

本ドキュメントは、[GitHub Wiki](https://github.com/anoyetta/ACT.Hojoring/wiki/SpespeTime_Reference) の内容をベースに、現在のソースコード (`ACT.SpecialSpellTimer.Core`) の実装に合わせて修正・追記を行ったものです。

## ファイル属性

### `<timeline>`
ルート要素

```xml
<timeline>
```

... (Wikiの記述と同様) ...

## タイムライン制御

### `<a>` Activity

```xml
<a time="00:06" text="風雷波動" notice="次は、風雷波動。" />
```

Activityを示すタイムラインの主な定義要素。

#### name
string, 任意項目 このアクティビティの識別子。gotoやcallで指定するために使用する。

#### inherits
string, 任意項目 この属性で指定された name のアクティビティの各属性を継承する。

#### time
TimeSpan, **必須** アクティビティの発生時刻を示す。 `mm:ss`形式, `s`形式 どちらで書いてもよい。

#### text
string, 任意項目 オーバーレイに表示するアクティビティの表示テキスト。textを省略すると同期のみ、通知のみに使用されオーバーレイには表示されない。 `\n` で文字列中の改行を示す。
- sync 正規表現結果による置換 : 有効
- 変数よる置換 : 有効

#### sync
string(RegEx), 任意項目 タイムラインの時間経過を強制的にこのアクティビティの時刻に合わせるためのログマッチングキーワード。ここに指定されたパターンとログがマッチしたときタイムラインの現在時刻をこのアクティビティの時刻に合わせる。正規表現が使える。

#### sync-s
double, 任意項目, 既定値-12 syncマッチングを開始する時間のオフセット秒数。このアクティビティの12秒前から同期マッチングを開始する。

#### sync-e
double, 任意項目, 既定値12 syncマッチングを終了する時間のオフセット秒数。このアクティビティの12秒後まで同期マッチングを継続する。

#### goto
string, 任意項目 このアクティビティの時刻が到来したときにここで指定されたnameのアクティビティ、サブルーチンにジャンプする。

#### call
string, 任意項目 このアクティビティの時刻が到来したときにここで指定されたnameのサブルーチンをコールする。

#### notice
string, 任意項目 このアクティビティの時刻が到来したときに通知を行う。waveファイルを指定した場合はwaveを再生する。その他の文字列の場合はTTSとして発声する。
- sync 正規表現結果による置換 : 有効
- 変数よる置換 : 有効

#### notice-d
enum, 任意項目, 既定値Both 通知を再生するデバイスを指定します。
- `Both` : メイン、サブ両方で再生する。
- `Main` : メインデバイスでのみ再生する。
- `Sub` : サブデバイスでのみ再生する。

#### notice-o
double, 任意項目, 既定値-6 通知発生させる時間的オフセット秒数。既定値ではアクティビティの時刻が到来する6秒前に通知する。

#### notice-vol
float, 任意項目, 既定値1.0 サウンド通知の音量を指定する。0.0（ミュート）～ 1.0 で指定する。

#### notice-sync
bool, 任意項目, 既定値false サウンドを同期再生する。true に指定された通知同士は同時再生されずに順次再生されるようになる。

#### style
string, 任意項目 設定UIで定義したStyleを割り当てる。指定しない場合は規定のStyleが割当てられる。

#### icon
string, 任意項目 アイコン画像ファイル名、またはURL。Style定義よりも優先して表示される。

#### exec
string, 任意項目 このアクティビティが実行された（時刻が到来した）ときに指定されたパスを起動する。
- `.ps1`, `.bat` などのスクリプトも実行可能。
- URIを指定するとREST API呼び出しとみなされる (GET/POST/PUT/DELETE対応)。
- `/wait [秒数] [コマンド]` という書式で遅延実行も可能。

#### args
string, 任意項目 exec で指定したコマンドへの引数。

#### exec-hidden
bool, 任意項目, 既定値false exec で起動するアプリケーションのウィンドウを非表示にする (対応している場合)。

#### json
string (Inner XML), 任意項目 REST API呼び出し時のペイロード定義。
```xml
<a ...>
  <json>{ "key": "value" }</json>
</a>
```

#### enabled
bool, 任意項目, 既定値true このエレメントが有効か無効か。

---

### `<t>` Trigger

```xml
<t text="天雷掌" sync="白虎は「天雷掌」の構え。" notice="天雷掌" />
```

タイムラインの実行中に常駐するトリガ。

#### name, inherits, no, text, goto, call, notice, notice-d, notice-o, notice-vol, notice-sync, exec, args, exec-hidden, enabled
(Activityと同様のため省略)

#### sync
string(RegEx), **必須** このトリガのマッチングパターン。

#### sync-count
string, 任意項目, 既定値0 何回目のマッチングでこのトリガを実行するか。
- 単数値: `1` (1回目のみ)
- カンマ区切り: `1,3,5` (1,3,5回目のみ)
- 範囲指定: `1-5` (1回目から5回目まで)
- `0` 指定時は毎回実行される。

#### sync-interval
int, 任意項目, 既定値0 最後にマッチしてから次にマッチするまでの再判定禁止期間(秒)。

---

### `<hp-sync>` HP Sync **(New)**

敵のHP残量(%)をトリガーとして同期またはアクションを行うための定義。`<t>` や `<a>` の子要素として記述できるが、主に単独の `<t>` としてフェーズ移行検知などに使われる。

```xml
<t sync="^15:..........:ハルオーネ:">
  <hp-sync name="ハルオーネ" hpp="90" />
  <load target="フェーズ2" />
</t>
```

#### name
string(RegEx), **必須** HPを監視する対象 (`Combatant`) の名前（正規表現）。

#### hpp
double, **必須** HP率 (0.0 - 100.0)。指定したHP以下になった瞬間にマッチする。
内部的には `CurrentHPRate <= hpp` の条件で判定される。

#### enabled
bool, 任意項目, 既定値true。

---

### `<p-sync>` Position Sync

オブジェクトや敵の座標配置を条件とするトリガ。

#### interval
double, 任意項目, 既定値30 判定間隔。

#### 子要素: `<combatant>`
- `name`: string(RegEx), 必須。対象の名前。
- `X`, `Y`, `Z`: float, 任意。座標。
- `tolerance`: float, 既定値0.01。誤差許容範囲。

---

### `<video>` (未確認だがソースに無し)
(ソースコード上に Video に関するモデル定義は見当たりませんでしたが、機能として存在する可能性があります。要確認)

---

### その他
- `<v-notice>`, `<i-notice>` には `sync-to-hide` (非表示トリガー) がある。
- `<load>` 要素で `truncate="true"` にすると現在のアクティビティを破棄してロードする。
