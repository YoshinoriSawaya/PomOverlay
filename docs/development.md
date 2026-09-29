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

- 実行中はトレイアイコン右クリック →「設定...」で設定ウィンドウを開き、「適用」で保存・反映できる
- 「設定ファイル (JSON) を開く」で `config.json` を直接編集することもできる
- 編集後は「設定をリロード」でアプリを再起動せずに反映できる
- 「設定を初期値に戻す」で `AppConfig.CreateDefault()` の内容に上書きされる

## デバッグ表示

- トレイメニュー「デバッグ表示設定」から、モニターごとにデバッグ情報(現在モード・残り時間・FPS・物理演算の値など)のON/OFFを切り替えられる
- デバッグ情報の文字列は `Managers/DebugManager.cs` で組み立て、`Views/DebugWindow` に表示する。負荷を抑えるため、テキストの更新は250msごと

## ログ

- 未処理例外は `error.log`(実行ファイルと同じフォルダ)に追記される(`DebugManager.LogError`)
- 描画ループ(`MainWindow.Update`)で例外が10回連続すると、そのモニターのループは停止する(`error.log` の肥大化を防ぐため)。オーバーレイが固まっていたら `error.log` を確認し、トレイメニューの「設定をリロード」で再開できる

## テスト

- `PomOverlay.Tests`(xUnit)に、WPF非依存のロジック層(`PomodoroStateCalculator`・`AuroraPhysicsCalculator`)のユニットテストがある
- リポジトリ直下で `dotnet test PomOverlay.slnx` を実行する(Visual Studio ではテストエクスプローラーからも実行できる)
- 本体が WPF の `Color` 型を使うため、テストプロジェクトも `net10.0-windows` / `UseWPF` にしている

## 負荷の計測

- `tools/measure.ps1` で PomOverlay と DWM の CPU・GPU・メモリを計測できる(PomOverlay を起動して30秒計測し、終了する)
- 事前に `dotnet build PomOverlay/PomOverlay.csproj -c Release` してから、リポジトリ直下で `pwsh tools/measure.ps1 -Mode Sleep` のように実行する
- スケジュールで時刻によってモードが変わるので、別の日の計測と比べるときは `-Mode`(Focus/Rest/Sleep)でモードを固定する。実行ファイルのフォルダを一時フォルダにコピーして `OverrideMode` を書き換えるので、元の `config.json` は変わらない
- `-ExePath` で別のビルド(旧版など)を指定して比べられる
- PomOverlay が起動中だと実行できない。計測結果は `issues/tasks/E02-01-baseline-measurement.md` に基準値がある
