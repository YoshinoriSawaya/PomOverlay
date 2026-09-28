# 開発手順

## 環境

- Visual Studio 2022
- .NET SDK (`net10.0-windows` が動く環境)
- Windows(レイヤードウィンドウ・P/Invokeを使うためWindows専用)

## ビルド・実行

1. `PomOverlay.slnx` を Visual Studio 2022 で開く
2. `F5` でビルド・デバッグ実行
   - 初回実行時、実行ファイルと同じフォルダに `config.json` が自動生成される
   - マルチモニター環境では、接続されている全モニターにオーバーレイウィンドウが立ち上がる

## 設定ファイルの確認・編集

- 実行中はトレイアイコン右クリック →「設定ファイル (JSON) を開く」で `config.json` を直接編集できる
- 編集後は「設定をリロード」でアプリを再起動せずに反映できる
- 「設定を初期値に戻す」で `AppConfig.CreateDefault()` の内容に上書きされる

## デバッグ表示

- トレイメニュー「デバッグ表示設定」から、モニターごとにデバッグ情報(現在モード・残り時間・FPS・物理演算の値など)のON/OFFを切り替えられる
- デバッグ情報の描画ロジックは `Managers/DebugManager.cs` に集約されている

## ログ

- 未処理例外は `error.log`(実行ファイルと同じフォルダ)に追記される(`DebugManager.LogError`)
- 現状、同じ原因で例外が連続発生しても処理は止まらないため、`error.log` が肥大化する可能性がある点に注意(`issues/epics/E01-code-cleanup.md` で対応予定)

## テスト

- ロジック層(状態計算・物理演算)を切り出したあと、`PomOverlay.Tests` プロジェクト(xUnit想定)を追加する
- `dotnet test` で実行する想定(プロジェクト追加後にこの節を更新する)
