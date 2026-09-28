# E01-01: 状態計算・物理演算のロジック層分離

親: [E01: コード整理とテスト基盤の導入](../epics/E01-code-cleanup.md)

## 概要

`MainWindow.xaml.cs`内の`CalculatePomodoroState`・`CalculatePhysics`を、WPFに依存しない独立したクラスへ切り出す。

- `PomodoroStateCalculator`：`AppConfig`と`DateTime`を受け取り`PomodoroState`を返す
- `AuroraPhysicsCalculator`：`PomodoroState`と`delta`を受け取り`AuroraPhysics`を返す(`_currentPhase`/`_currentFlow`はインスタンスの状態として保持)

`MainWindow`側には、これらを呼び出して結果を`ApplyVisuals`でXAML要素に反映する「配線」だけを残す。

## 完了条件

- `MainWindow.xaml.cs`から状態計算・物理演算のロジックが除かれ、上記2クラスに移動している
- `AuroraPhysics`の色計算(`System.Windows.Media.Color`依存)は、WPFの型のまま許容する(個人用Windows専用アプリのため、独自Color構造体への置き換えはしない)

## 設計の決定

(未着手のため、着手時にここを埋める)

## 検証結果

(未着手)

## 未検証のまま残したこと

(未着手)

## ステータス

未着手
