# E01-04: config.json値のバリデーション追加

親: [E01: コード整理とテスト基盤の導入](../epics/E01-code-cleanup.md)

## 概要

`config.json`は手編集される前提だが、`PulseSec`/`FlowDuration`が0以下、`Min`が負数などの壊れた値が、そのまま物理演算(ゼロ除算等)に流れ込む。読み込み時にガードし、不正値は安全なデフォルトにフォールバックする。

## 完了条件

- `PhaseConfig`の主要な数値項目(`PulseSec`, `FlowDuration`, `Min`, `Thick`, `BlurMin`/`BlurMax`等)に対して、0以下・不正な値が読み込まれた場合に安全な値へフォールバックする
- スケジュールの時刻文字列(`TimeSpan.TryParse`失敗時)が無視されている挙動について、ログか何らかの形で気づけるようにするか検討する

## 依存関係

E01-01と並行して進めて良い。

## 設計の決定

- WPF の画面に依存しない `ConfigValidator.Sanitize(AppConfig)`(`Models/Configs/`)を追加し、`App.LoadOrCreateConfig` の最後で呼ぶ。起動時もリロード時もここを通る
- 補正はメモリ上だけで行い、`config.json` は書き換えない(手編集した内容を勝手に消さない)。補正した項目は MessageBox でまとめて知らせる
- フォールバック先は `AppConfig.CreateDefault()` の同じモードの値。`Lunch` など既定に無いモード名は Focus の値を使う
- ルール
  - `PulseSec`・`FlowDuration`:0以下はフォールバック(ゼロ除算になるため)
  - `Min`・`Thick`・`BlurMin`・`BlurMax`:負数はフォールバック。0は許容(Sleep の `Min = 0` など意味のある値のため)
  - Focus と Rest の `Min` がどちらも0:サイクル長が0で NaN になるため、両方を既定値に戻す
  - `OpMin`・`OpMax`:0～1の範囲外はフォールバック
  - `BlurMin > BlurMax`・`OpMin > OpMax`:入れ替える
  - `TransitionSec`:負数はフォールバック(0は即時切り替えとして許容)
  - `Modes`・`Schedules` が null、要素が null:空のコレクション・既定値で置き換える
- 完了条件の数値項目に加えて、`ColorStrings` の読めない色名も取り除くことにした。放置すると `ApplyVisuals` で毎フレーム例外になり、E01-03 の停止にかかるため。色の判定には WPF の `ColorConverter` を使っている(`PhaseConfig` がすでに WPF の `Color` に依存しているのと同じ扱い)
- スケジュールの時刻が読めない場合:要素は残したまま(従来どおり無視される)、警告を出して気づけるようにした

## 検証結果

- 2026-09-29: `ConfigValidator` のテスト16件を含む46件すべて成功。補正後の設定で `PomodoroStateCalculator`/`AuroraPhysicsCalculator` を回し、値が NaN/無限大にならないことも確かめた

## 未検証のまま残したこと

- MessageBox が実際に表示されること(起動時・リロード時)はアプリ上で確かめていない
- `OverrideMode`/`ApplyMode` の enum を文字列で書いた場合の挙動は未確認。`JsonStringEnumConverter` を使っていないので、JSON 読み込み自体が失敗して全体が既定値になる可能性がある(`ScheduleItem` のコメントは「文字列で保存される」と言っているが、実際の保存形式とあわせて確認が必要)
- `Min` が60分の約数でない場合(サイクルが「時」の区切りと合わない)の挙動は仕様として扱い、補正していない

## ステータス

完了(2026-09-29)
