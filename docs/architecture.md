# アーキテクチャ

PomOverlayの横断的な設計判断をまとめる。個別のタスクの経緯は `issues/` を参照。

## 現状の構成

```
PomOverlay/
├── Views/            App.xaml(.cs)、MonitorOverlay、EdgeBandWindow.xaml(.cs)、
│                     DebugWindow.xaml(.cs)、ClickThrough
├── Models/
│   ├── Configs/       AppConfig, PhaseConfig, ScheduleItem, ConfigValidator
│   ├── Domain/         PomodoroState, AuroraPhysics,
│   │                   PomodoroStateCalculator, AuroraPhysicsCalculator, Interpolation,
│   │                   GradientColors, EdgeBandLayout
│   └── Display/         DebugLabels
└── Managers/           DebugManager, ConsecutiveFailureLimiter
PomOverlay.Tests/       ロジック層のユニットテスト(xUnit)
```

- `App.xaml.cs`：起動時・リロード時に `config.json` を読み込み、`ConfigValidator` で不正な値を補正する。接続されている全モニター分の `MonitorOverlay` を生成する。トレイアイコン・メニューの管理もここ
- `MonitorOverlay`：1モニター分の配線(Window ではない)。`CompositionTarget.Rendering`(画面のリフレッシュごと)を `FramePacer` で `Fps`(既定30)に間引き、 `PomodoroStateCalculator`(状態計算)→ `AuroraPhysicsCalculator`(物理演算)→ `GradientColors`(色)を呼び、結果を縁の帯ウィンドウに反映する。連続して例外が出たらループを止める(`ConsecutiveFailureLimiter`)
- `EdgeBandWindow`：縁の帯1本分(上・下・左・右)のレイヤードウィンドウ。帯幅は `EdgeBandLayout` が全モードの `Thick + BlurMax` の最大値から決め、設定変更時だけ配置し直す。中にはモニター全体サイズの `Rectangle` を座標をずらして置き、帯に当たる部分だけを見せる(グラデーションと角のぼかしをつなげるため)
- `DebugWindow`：モニター左上のデバッグ表示
- 状態計算・物理演算・設定のバリデーションはWPFの画面に依存しないクラスに置き、`PomOverlay.Tests` でテストしている。色だけはWPFの `System.Windows.Media.Color` をそのまま使っている
- 透過表示は `AllowsTransparency="True"` + P/Invoke(`ClickThrough`)での `WS_EX_LAYERED`/`WS_EX_TRANSPARENT` 指定によるレイヤードウィンドウ

## 既知のアーキテクチャ上の課題

- **毎フレームの再描画・転送が重い。** `AllowsTransparency` はWPFのレンダリングをソフトウェア(CPU)パスに切り替え、レイヤードウィンドウ全体を毎フレーム描き直して転送する。負荷はピクセル数(帯の面積)とフレームレートにほぼ比例し、`BlurEffect` の計算は主因ではない(`issues/tasks/E02-03-prebaked-blur.md` の切り分け)。デバッグ表示の毎フレーム更新も大きい

## 今後の方向性(検討中・未着手)

- **描くピクセル数とフレーム数を減らす**:デバッグ表示の間引き、フレームレートの引き下げ、帯幅を今のモードに合わせる(E02-04〜06)
- ぼかしの事前ベイクは試したが CPU に効果が無かった(E02-03、中止)
- **設定ウィンドウ**：現在は`config.json`の直接編集のみ。GUIでの編集画面を追加する

上記はまだ設計段階で、実装のタイミングでissuesに起こす。
