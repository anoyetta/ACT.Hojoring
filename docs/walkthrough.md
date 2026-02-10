# ウォークスルー: RazorEngine から RazorLight への移行検証 (Phase 0 完了)

## 目的
`RazorEngine` から `RazorLight` への移行において、タイムラインテンプレートの出力が正しく行われること、および既存のビルドプロセスが正常に機能することを検証する。

## 実施内容と解決した課題

### 1. RazorLight への置換と HTML エンコード回避
**課題:**
`RazorLight` はデフォルトで出力を HTML エンコードするため、XML タグ（`<` など）が破壊される（`&lt;` になる）。

**解決策:**
`RazorLightEngineBuilder` のプライベートフィールド `disableEncoding` をリフレクションで `true` に設定するロジックを実装しました。

```csharp
// TimelineModel.cs
var allFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
var field = builder.GetType().GetField("disableEncoding", allFlags);
if (field != null)
{
    field.SetValue(builder, true);
    return;
}
```

### 2. ビルドスクリプトの修正
**課題:**
`ACT.Hojoring.Debug` および `ACT.Hojoring` のビルド後イベントで、「ファイルが見つからない」エラーが発生しました。
原因は、`bin` フォルダに存在しない `FFXIV.Framework.pdb` を移動しようとしていたためでした。

**解決策:**
`.csproj` ファイルから誤った `move` コマンドを削除しました。

```xml
<!-- 削除した行 -->
<!-- @move /y "$(TargetDir)bin\FFXIV.Framework.pdb" "$(TargetDir)">nul -->
```

### 3. Razor コンパイルエラーの解消（メタデータ参照）
**課題:**
タイムライン読み込み時に `Razor Compile Error` が発生しました。
- `The type 'Attribute' is defined in an assembly that is not referenced...` (`netstandard`)
- `Missing compiler required member 'Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create'` (`Microsoft.CSharp`)

**解決策:**
`TimelineModel.cs` の `CreateTimelineEngine` メソッドにて、不足していたアセンブリへのメタデータ参照を明示的に追加しました。

```csharp
// TimelineModel.cs
builder.AddMetadataReferences(
    // ... 既存の参照 ...
    MetadataReference.CreateFromFile(typeof(RazorLightEngine).Assembly.Location), // RazorLight
    MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),      // netstandard (基本型)
    MetadataReference.CreateFromFile(Assembly.Load("Microsoft.CSharp").Location)  // dynamic型サポート
);
```

## 検証結果
- **ビルド**: `make.ps1` および Visual Studio でのリビルドが正常に完了することを確認。
- **動作確認**:
    - Razor テンプレートを使用しない XML タイムラインが正常にロードされること。
    - Razor テンプレート（`dynamic` や `ViewBag` を含むもの）を使用する XML タイムラインがエラーなくロードされ、正常に動作すること。
    - XML タグがエスケープされずに出力されていること（検証用スクリプトで確認済み）。

## 結論
Phase 0 の全タスクは完了しました。基盤となるビルド環境とテンプレートエンジンは正常に動作しています。
