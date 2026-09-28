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

(未着手)

## 検証結果

(未着手)

## 未検証のまま残したこと

(未着手)

## ステータス

未着手
