# E01-06: config.json の enum を名前で書けるようにする

親: [E01: コード整理とテスト基盤の導入](../epics/E01-code-cleanup.md)

## 概要

E01-04 で見つけた問題の対応。`OverrideMode`/`ApplyMode` は数値(`"ApplyMode": 3`)で保存されていて、手編集で `"Sleep"` のように名前で書くと JSON の読み込み自体が失敗し、設定全体が初期値に戻っていた。`ScheduleItem` の「文字列で保存される」というコメントも実態と違っていた。

## 完了条件

- `config.json` で enum を名前(`"Focus"` など)で書いても読み込める
- 既存の数値形式の `config.json` もそのまま読める
- 保存時は名前で書き出す

## 依存関係

E01-04 の後。

## 設計の決定

- JSON の読み書き設定を `App` から `AppConfig.FromJson`/`AppConfig.ToJson` に移し、`JsonStringEnumConverter` を追加した。読み込みと保存で同じ設定を使う
- `JsonStringEnumConverter` は既定で数値も受け付けるので、古い形式の `config.json` は書き換えなくても読める。次に保存したとき(初期化・初回生成)に名前の形式になる

## 検証結果

- 2026-09-29: 手元の `bin/Debug`・`bin/Release` の `config.json` が数値形式であることを確認した
- 名前形式・数値形式の読み込み、保存して読み戻す往復のテスト3件を追加し、49件すべて成功

## 未検証のまま残したこと

- アプリ上で名前形式の `config.json` をリロードする確認はしていない

## ステータス

完了(2026-09-29)
