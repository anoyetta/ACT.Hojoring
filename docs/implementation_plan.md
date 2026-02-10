# 実装計画書: Phase 1 タイムライン主要実装

## 現在のステータス
- **Phase 0 (基盤整備)**: 完了
    - CI環境構築 (GitHub Actions)
    - RazorEngine -> RazorLight 移行
    - ビルドスクリプトの修正

## Phase 1: Core Timeline Implementation

### 目標
タイムライン機能の中核となるデータモデルクラス群（`Chronology`, `Zone`, `Activity`, `Trigger`）を実装し、XMLおよびYAML形式でのシリアライズ/デシリアライズをサポートします。

### 提案される変更

#### 1. データモデルの実装
`TimelineBase` を継承し、以下のクラスを実装します。

- **[NEW] Chronology** (タイムラインのルート)
    - プロパティ: `Activities`, `Triggers`, `Metadata`
    - 役割: タイムライン全体のコンテナ。

- **[NEW] Zone**
    - プロパティ: `ZoneName`, `ZoneID`
    - 役割: タイムラインが適用されるゾーンの定義。

- **[NEW] Activity**
    - プロパティ: `Time`, `Text`, `Style`, `CallTarget`
    - 役割: 時間経過に伴うアクションや通知の定義。

- **[NEW] Trigger**
    - プロパティ: `Regex`, `Sync`, `Notice`
    - 役割: ログイベントに反応するトリガーの定義。

#### 2. シリアライズ・デシリアライズ
- **XML**: `System.Xml.Serialization` を使用して既存フォーマットとの互換性を考慮しつつ実装。
- **YAML**: `YamlDotNet` (または `Hjson`) を使用して、より記述しやすい形式をサポート（将来的な移行を見据えて）。

### 作業手順
1.  `ACT.SpecialSpellTimer.Core` プロジェクト内に新しいフォルダ `Models/Timeline` を作成。
2.  各クラスファイル（`Chronology.cs`, `Activity.cs` 等）を作成。
3.  `TimelineBase` クラスに必要な共通プロパティ（ID, Name, Enabled等）が不足している場合は追加・修正。
4.  シリアライズ用のヘルパークラスまたは拡張メソッドを実装。
5.  簡単なユニットテスト（またはコンソールアプリ）で、オブジェクトの保存・読み込みが正常に行えるか検証。

## 検証計画

### 自動テスト
- 新しいモデルクラスのシリアライズ・デシリアライズテストを作成・実行。
- XML/YAML の相互変換が正しく行われるか確認（オプション）。

### 手動検証
- ダミーデータを作成し、ファイルに保存できることを確認。
- 保存したファイルを読み込み、オブジェクトが正しく復元されることを確認。
