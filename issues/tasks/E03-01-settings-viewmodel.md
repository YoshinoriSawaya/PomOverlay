# E03-01: 設定画面の ViewModel

親: [E03: 設定ウィンドウ](../epics/E03-settings-window.md)

## 概要

設定ウィンドウの編集ロジックを WPF の画面に依存しないクラス(ViewModel)に置く。`AppConfig` から編集用の値を作り、編集後の値から `AppConfig` を組み立て、`ConfigValidator` で不正な値を見つける。

## 完了条件

- `AppConfig` → 編集用の値 → `AppConfig` の往復で値が変わらない
- 色の文字列(カンマ区切り)の分解、スケジュールの追加・削除ができる
- 不正な値があると適用できず、理由の一覧が得られる
- 上記がユニットテストされている

## 依存関係

なし

## 設計の決定

- `ViewModels/SettingsViewModel.cs` に、`SettingsViewModel`(全体)・`ModeSettingsViewModel`(モード1つ)・`ScheduleSettingsViewModel`(スケジュール1件)・変更通知の土台 `ObservableObject` を置いた。どれも WPF の画面に依存しない(`INotifyPropertyChanged`・`ObservableCollection` のみ)
- モードは `config.json` にあるものをそのまま並べる。モードの追加・削除はしない
- 色はカンマ区切りの1行で編集する。前後の空白と空の要素は取り除く
- スケジュールで選べるモードは Focus/Rest/Sleep。Auto はサイクルに戻るだけなので外した
- `OverrideMode` はトレイメニューで切り替えるものなので、編集対象にせず元の値を引き継ぐ
- 入力チェックは `ConfigValidator.Sanitize` を再利用する。組み立てた設定の複製にかけ、補正が1件でも出たら適用させず、その文言を理由として返す。入力中の値は書き換えない(ユーザーが自分で直す)

## 検証結果

- 2026-09-30:`SettingsViewModel` のテスト8件を追加し、83件すべて成功(往復で値が変わらないこと、編集の反映、色の分解、スケジュールの追加・削除、不正な値での失敗と理由、変更通知)

## 未検証のまま残したこと

- 画面との結びつき(E03-02 で扱う)

## ステータス

完了(2026-09-30)
