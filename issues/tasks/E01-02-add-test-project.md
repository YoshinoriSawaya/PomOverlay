# E01-02: テストプロジェクトの追加

親: [E01: コード整理とテスト基盤の導入](../epics/E01-code-cleanup.md)

## 概要

`PomOverlay.Tests`(xUnit想定)を追加し、E01-01で切り出したロジック層に対するユニットテストを書く。

## 完了条件

- ソリューション(`PomOverlay.slnx`)にテストプロジェクトが追加されている
- 最低限、以下がテストされている
  - `PomodoroStateCalculator`：Focus/Restのサイクル境界、`OverrideMode`優先、スケジュール重複時の挙動(先勝ち)
  - `AuroraPhysicsCalculator`：位相が2πでラップすること、Blur/Opacityが設定範囲を超えないこと
- `dotnet test` で実行できる

## 依存関係

E01-01の完了後に着手する。

## 設計の決定

- `dotnet new xunit` で作成(xUnit 2.9.3)。本体の `PhaseConfig` が `System.Windows.Media.Color` に依存しているため、テストプロジェクトも `net10.0-windows` / `UseWPF=true` にした
- 本体(WinExe)をプロジェクト参照して、`PomodoroStateCalculator`・`AuroraPhysicsCalculator` を直接テストする
- 位相 `_currentPhase` は private なので、外から見える `PulseTime`(= 位相/2π × PulseSec)を通して 2π でのラップを確かめる
- 完了条件に挙げた観点に加えて、次も現状の挙動としてテストで固定した:フェード率(`TransRatio`)、進捗率、日付をまたぐスケジュール、`Modes` に無いモードをスケジュールで指定したときサイクルに戻ること、`TransRatio` の範囲外の値がクランプされること

## 検証結果

- 2026-09-29: `dotnet test PomOverlay.slnx` で 25件すべて成功

## 未検証のまま残したこと

- 0以下の `PulseSec`/`FlowDuration`/`Min` といった不正な設定値のケースはテストしていない(E01-04 でバリデーションを入れるときに追加する)

## ステータス

完了(2026-09-29)
