# アーキテクチャ

PomOverlayの横断的な設計判断をまとめる。個別のタスクの経緯は `issues/` を参照。

## 現状の構成

```
PomOverlay/
├── Views/            App.xaml(.cs)、MainWindow.xaml(.cs)
├── Models/
│   ├── Configs/       AppConfig, PhaseConfig, ScheduleItem
│   ├── Domain/         PomodoroState, AuroraPhysics
│   └── Display/         DebugLabels
└── Managers/           DebugManager
```

- `App.xaml.cs`：起動時に `config.json` を読み込み、接続されている全モニター分の `MainWindow` を生成する。トレイアイコン・メニューの管理もここ
- `MainWindow.xaml.cs`：現状は「状態計算(`CalculatePomodoroState`)」「物理演算(`CalculatePhysics`)」「描画反映(`ApplyVisuals`)」「デバッグ表示」を1クラスで担っている
- 透過表示は `AllowsTransparency="True"` + P/Invokeでの `WS_EX_LAYERED` 指定によるレイヤードウィンドウ

## 既知のアーキテクチャ上の課題

- **`MainWindow` にロジックが集中している。** WPFの `Window` は依存が重く単体テストしにくいため、状態計算・物理演算はWPF非依存のクラスへ分離する方針(詳細は `issues/epics/E01-code-cleanup.md`)
- **レイヤードウィンドウが全画面サイズ。** `AllowsTransparency` はWPFのレンダリングをソフトウェア(CPU)パスに切り替える。縁しか光らせていなくても画面全体分のサーフェスを毎フレーム転送しており、これが動作の重さの主因
- **毎フレームのBlurEffect適用が重い。** `BlurEffect`(ソフトウェア実装)を60FPSで画面全体にかけている

## 今後の方向性(検討中・未着手)

- **ウィンドウをフチの帯だけに絞る**：全モニター全画面ではなく、`Modes`内の`BlurMax`/`Thick`の最大値から逆算した幅の帯だけをウィンドウにする。設定変更時のみ再計算・リサイズし、毎フレームでは変更しない
- **ぼかしの事前ベイク**：起動時(帯幅が決まったあと)に、ぼかし半径×線の太さの組み合わせパターンをあらかじめ`RenderTargetBitmap`で焼いておき、`OpacityMask`として使う。色・Opacity・Flowはライブのグラデーションブラシ側で処理し、毎フレームのBlur計算自体をなくす
- **設定ウィンドウ**：現在は`config.json`の直接編集のみ。GUIでの編集画面を追加する

上記はまだ設計段階で、実装のタイミングでissuesに起こす。
