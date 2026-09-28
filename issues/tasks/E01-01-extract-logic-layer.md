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

- 計算式・フォールバック値は一切変えず、そのまま移動した(挙動を変えない純粋なリファクタリング)。ゼロ除算などの不正値対策は E01-04 で扱う
- `PomodoroStateCalculator` は状態を持たないので static クラス `Calculate(AppConfig, DateTime)` にした。`MainWindow` の `_currentModeName`/`_targetModeName` フィールドは、この計算の中でしか使われていなかったためローカル変数にした
- `AuroraPhysicsCalculator` は `_currentPhase`/`_currentFlow` をインスタンスの状態として持ち、`Update(PomodoroState, double delta)` で進める。`MainWindow`(モニターごとに1つ)が1インスタンスずつ持つ
- `MainWindow.Lerp`・`MainWindow.LerpColorStatic` は `Interpolation`(`Models/Domain`)へ移した。`PhaseConfig` が View の `MainWindow` を参照していた依存もこれで解消した
- 移動したメソッドのすぐ近くにあったコメントアウト済みの旧 `CalculatePomodoroState` は一緒に削除した。それ以外のコメントアウト(旧 `ApplyVisuals`・旧 `UpdateDebugText` など)は E01-05 の範囲として残している
- 使われなくなった `_transitionSec` フィールドを削除した(参照はコメントアウト内だけだった)

## 検証結果

- 2026-09-29: `dotnet build PomOverlay.slnx` が成功した(警告0・エラー0)

## 未検証のまま残したこと

- アプリを起動しての見た目の確認(オーロラの明滅・Focus/Restの切り替えフェード・スケジュール強制)は未実施。ロジックは移動しただけだが、手動で確認するまでは未検証扱い
- ロジックのユニットテストは E01-02 で追加する

## ステータス

完了(2026-09-29)
