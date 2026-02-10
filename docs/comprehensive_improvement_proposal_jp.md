# ACT.Hojoring 全体改善提案書

本ドキュメントでは、ACT.Hojoringソリューション全体（FFXIV.Framework, SpecialSpellTimer, UltraScouter等）のコード解析に基づいた改善案を提示します。

## 1. エグゼクティブサマリ

現状のソリューションは長年の機能追加により、一部で設計の肥大化・レガシーな実装パターンが見受けられます。特に「非同期処理の近代化（Task/Asyncへの移行）」と「巨大クラスの分割（責務の分離）」が、今後の保守性とパフォーマンス向上における鍵となります。

## 2. FFXIV.Framework (共通基盤) の改善

最も重要な共通ライブラリであり、ここの改善は全プラグインに波及効果があります。

### 2.1. `XIVPluginHelper` の再設計 (God Classの解消)
**現状:**
`XIVPluginHelper.cs` は1700行を超え、以下の責務が混在しています。
- FFXIVプロセス監視 (`RefreshCurrentFFXIVProcess`)
- ログパースとイベント発火 (`SubscribeXIVLog`, `OnLogLineRead`)
- データ管理 (スキル、ゾーン、ワールド情報のロード)
- 戦闘状態の追跡 (`RefreshInCombat`, `RefreshBoss`)

**提案:**
責務ごとに以下のクラスへ分割・移譲することを推奨します。
- `ProcessMonitor`: プロセスのライフサイクル管理
- `LogReaderService`: ログの読み取りとイベント配信
- `GameDataManager`: 静的データ（スキル、ゾーン等）の管理
- `CombatMonitor`: 戦闘状態やLPSの計算

### 2.2. スレッドモデルの近代化 (`ThreadWorker`)
**現状:**
`ThreadWorker.cs` で `System.Threading.Thread` を直接制御しており、`Thread.Abort()` や `Thread.Sleep()` が使用されています。これらは現代の.NET開発（特に.NET Core以降）では非推奨かつ危険なAPIです。

**提案:**
- `Task` と `CancellationToken` を用いたパターンへ移行する。
- `Thread.Sleep` の代わりに `await Task.Delay` を使用し、スレッドブロックを回避する。

### 2.3. WPF初期化ロジックの見直し
**現状:**
`WPFHelper.cs` にて `RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;` が設定されています。

**提案:**
- ソフトウェアレンダリングの強制はパフォーマンスに悪影響を与える可能性があります。特定の環境不具合の回避策でない限り、ハードウェアアクセラレーションを有効にすることを検討すべきです。
- `Application` オブジェクトの生成ロジックが、ACT本体や他プラグインとの競合を考慮した堅牢なものか再確認が必要です。

## 3. ACT.SpecialSpellTimer (スペスペ) の改善

### 3.1. メインループと非同期処理
**現状:**
`PluginMainWorker.cs` や `PluginCore.cs` にて、`Application.DoEvents()` の使用や、独自のスレッド管理が見られます。`DoEvents` は予期せぬ再入可能性を引き起こし、バグの温床となります。

**提案:**
- `Application.DoEvents()` を完全に廃止し、`async/await` パターンへ移行する。
- UI更新は `Dispatcher.InvokeAsync` を適切に使用し、メインロジックからUI操作を分離する。
- `BackgroundCore` (ログ監視) を `Task.Run` ベースの長時間実行タスクに変更する。

### 3.2. 設定保存の非同期化
**現状:**
`SaveSettingsAsync` は存在しますが、内部で `Dispatcher` を経由しています。

**提案:**
- ファイルI/O自体はUIスレッドに依存させる必要はありません。純粋な `Task` として実装し、完了通知のみをUIに行う設計が望ましいです。

## 4. ACT.UltraScouter (ウルスカ) の改善

### 4.1. アーキテクチャの統一
**現状:**
Spespeと同様に `FFXIV.Framework` に強く依存していますが、プラグイン初期化時のAssembly解決ロジック (`Plugin.cs` の `AssemblyResolve`) がやや複雑です。

**提案:**
- 基本的な構造はSpespeと共通化し、メンテナンスコストを下げる。
- `PluginCore` の構造をSpespeの改善案に合わせて近代化する。

## 5. 全体的なエンジニアリング

### 5.1. 依存性注入 (Dependency Injection)
`TinyIoC` が導入されていますが、一部でシングルトンパターン (`Instance` プロパティ) と混在しています。コンストラクタ注入を基本とする設計へ徐々に移行することで、テスト容易性が向上します。

### 5.2. ビルドとCI
`make.ps1` が存在しますが、GitHub Actions等のCI環境で自動ビルド・テストが実行できる構成を整備することで、プルリクエスト時の品質担保が可能になります。

## 6. 推奨ロードマップ（改定版）

質問への回答（CI環境、RazorEngine）を踏まえ、以下の順序での実施を推奨します。

1.  **フェーズ0（基盤整備とセキュリティ）**: 
    - **CI環境の構築**: GitHub Actions と Secrets を用いて、プルリクエストごとの自動ビルド・テスト環境を整備します。これにより、以降のリファクタリング時の退行（リグレッション）を防ぎます。
    - **RazorLight への移行**: セキュリティリスクのある `RazorEngine` を `RazorLight` に置き換え、警告を解消します。

2.  **フェーズ1（非同期処理の近代化）**: 
    - `ThreadWorker` の `Task` 化、`Thread.Sleep` / `Thread.Abort` の排除。
    - `Application.DoEvents` の廃止と `Dispatcher` パターンの適用。

3.  **フェーズ2（アーキテクチャ再設計）**: 
    - `XIVPluginHelper` の責務分割（ProcessMonitor, LogReader 等へ）。
    - 依存性注入（DI）の整理。

4.  **フェーズ3（UXとパフォーマンス）**: 
    - ソフトウェアレンダリング設定の適正化。
    - UIレスポンスのチューニング。

## 7. 特定の課題への対応（ユーザー質問への回答）

### 7.1. ビルド用鍵ファイルとCI環境
**課題:**
ビルドに必要な鍵ファイル（Strong Name Key等）が含まれていないため、GitHub Actions等のCI環境でビルドできない。

**解決策:**
**GitHub Actions Secrets** を利用する手法が一般的かつ安全です。

1.  手元の鍵ファイル（例: `Hojoring.snk`）をBase64文字列に変換します（PowerShell等で可能）。
2.  GitHubリポジトリの Settings > Secrets and variables > Actions にて、`HOJORING_SNK_BASE64` という名前でその文字列を登録します。
3.  GitHub Actionsのワークフロー（YAML）内で、ビルド前にこのSecretからファイルを復元するステップを追加します。

```yaml
- name: Restore Signing Key
  run: |
    $bytes = [Convert]::FromBase64String("${{ secrets.HOJORING_SNK_BASE64 }}")
    [IO.File]::WriteAllBytes("source/ACT.Hojoring.Common/Hojoring.snk", $bytes)
    # 必要なパスに合わせてファイルを配置
  shell: pwsh
```

これにより、鍵本体をリポジトリに公開することなく、CI上でのみ署名付きビルドが可能になります。

### 7.2. RazorEngine のセキュリティとメンテナンス
**課題:**
`RazorEngine.dll` は開発が停止しており、セキュリティリスクやビルド警告の問題がある。

**解決策:**
**[RazorLight](https://github.com/toddams/RazorLight)** への移行を推奨します。

- **理由:** `RazorLight` は現在もメンテナンスされており、.NET Standard 2.0 / .NET Core 以降をサポートするモダンなライブラリです。`RazorEngine` と同様に、文字列からのRazorコンパイルをサポートしています。
- **互換性:** 基本的なRazor構文（C#埋め込み）は互換性がありますが、初期化コードや呼び出しAPI（`IRazorEngineService` vs `RazorLightEngine`）は異なるため、`TimelineModel.cs` 内のラッパー部分の書き換えが必要です。
- **暫定対応:** 直近のビルド警告のみを抑制したい場合は、該当プロジェクトのプロパティまたは `#pragma warning` で抑制可能ですが、根本解決にはライブラリの置換が必要です。

もしユーザー定義タイムラインでの「任意のC#コード実行」が不要であれば、よりセキュアで軽量なテンプレートエンジン（**Scriban** 等）への移行も検討の余地がありますが、既存タイムラインとの互換性を維持するには `RazorLight` が現実的な選択肢です。
