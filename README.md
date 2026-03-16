直AIのため修正予定。

# PomOverlay

ポモドーロテクニックをサポートする、画面の縁（フチ）の色が変わるC#製オーバーレイタイマーです。

## 📝 概要 (About)
集中状態（フロー）を途切れさせないことを目的としたポモドーロタイマーです。
画面の中央にポップアップや数字を表示するのではなく、**画面の境界線の色を変化させる**ことで、視界の端で自然に時間経過と休憩のタイミングを知らせます。

イラスト制作やプログラミングなど、作業領域を広く使いつつ、作業の邪魔をされずに時間管理をしたい方に最適です。

## ✨ 主な機能 (Features)
- 画面の縁（フチ）への控えめなカラーオーバーレイ表示
- ポモドーロのサイクル（集中 / 休憩）に応じた色の変化
- 作業の妨げにならないクリックスルー（マウス操作の透過）対応

## 🛠 技術スタック (Tech Stack)
- **言語:** C#
- **IDE:** Visual Studio 2022 (.slnx)

## 🚀 使い方 (Usage)
1. [Releases](https://github.com/YoshinoriSawaya/PomOverlay/releases) ページから最新の実行ファイルをダウンロードします。
2. `PomOverlay.exe` を起動します。
3. [例：起動すると自動的に25分の集中タイムが開始され、画面の縁が〇〇色になります]
4. [例：時間が来ると縁の色が〇〇色に変わり、5分の休憩タイムをお知らせします]

## 💻 ローカル開発環境の構築 (Getting Started)

1. リポジトリをクローンします。
```bash
   git clone [https://github.com/YoshinoriSawaya/PomOverlay.git](https://github.com/YoshinoriSawaya/PomOverlay.git)
```

2. リポジトリのルートにある `PomOverlay.slnx` を Visual Studio 2022 で開きます。
3. `F5` キーを押してビルドおよびデバッグを実行します。

## 🗺 今後の展望 (TODO)

* [ ] 集中時間と休憩時間の長さのカスタマイズ機能
* [ ] 色のカスタマイズ機能（お好みのテーマカラーへの変更）
* [ ] マルチモニター環境への対応（特定のモニターのみ、または全モニターの縁を光らせる等）

## 🐛 既知の課題・イシュー (Issues)

* [ ] [例：特定のフルスクリーンゲームなどを起動している際、背面に隠れてしまう問題]
* [ ] [例：高DPI（4Kモニター等）環境での縁の太さの調整]

## 📄 ライセンス (License)

This project is licensed under the [MIT License](https://www.google.com/search?q=LICENSE).

