# アーキテクチャ

PomOverlayの横断的な設計判断をまとめる。個別のタスクの経緯は `issues/` を参照。

## 現状の構成

```
PomOverlay/
├── Views/            App.xaml(.cs)、MainWindow.xaml(.cs)
├── Models/
│   ├── Configs/       AppConfig, PhaseConfig, ScheduleItem, ConfigValidator
│   ├── Domain/         PomodoroState, AuroraPhysics,
│   │                   PomodoroStateCalculator, AuroraPhysicsCalculator, Interpolation
│   └── Display/         DebugLabels
└── Managers/           DebugManager, ConsecutiveFailureLimiter
PomOverlay.Tests/       ロジック層のユニットテスト(xUnit)
```

- `App.xaml.cs`：起動時・リロード時に `config.json` を読み込み、`ConfigValidator` で不正な値を補正する。接続されている全モニター分の `MainWindow` を生成する。トレイアイコン・メニューの管理もここ
- `MainWindow.xaml.cs`：16msごとのタイマーで `PomodoroStateCalculator`(状態計算)→ `AuroraPhysicsCalculator`(物理演算)を呼び、結果をXAML要素に反映する「配線」だけを持つ。連続して例外が出たらループを止める(`ConsecutiveFailureLimiter`)
- 状態計算・物理演算・設定のバリデーションはWPFの画面に依存しないクラスに置き、`PomOverlay.Tests` でテストしている。色だけはWPFの `System.Windows.Media.Color` をそのまま使っている
- 透過表示は `AllowsTransparency="True"` + P/Invokeでの `WS_EX_LAYERED` 指定によるレイヤードウィンドウ

## 既知のアーキテクチャ上の課題

- **レイヤードウィンドウが全画面サイズ。** `AllowsTransparency` はWPFのレンダリングをソフトウェア(CPU)パスに切り替える。縁しか光らせていなくても画面全体分のサーフェスを毎フレーム転送しており、これが動作の重さの主因
- **毎フレームのBlurEffect適用が重い。** `BlurEffect`(ソフトウェア実装)を60FPSで画面全体にかけている

## 今後の方向性(検討中・未着手)

- **ウィンドウをフチの帯だけに絞る**：全モニター全画面ではなく、`Modes`内の`BlurMax`/`Thick`の最大値から逆算した幅の帯だけをウィンドウにする。設定変更時のみ再計算・リサイズし、毎フレームでは変更しない
- **ぼかしの事前ベイク**：起動時(帯幅が決まったあと)に、ぼかし半径×線の太さの組み合わせパターンをあらかじめ`RenderTargetBitmap`で焼いておき、`OpacityMask`として使う。色・Opacity・Flowはライブのグラデーションブラシ側で処理し、毎フレームのBlur計算自体をなくす
- **設定ウィンドウ**：現在は`config.json`の直接編集のみ。GUIでの編集画面を追加する

上記はまだ設計段階で、実装のタイミングでissuesに起こす。
