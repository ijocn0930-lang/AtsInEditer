# ATS Plugin INI Editor

ATS シミュレータ用プラグインの INI 設定ファイルを安全かつ簡単に編集するための専用 GUI エディタです。

## 🌟 主な機能・特徴

- **Shift-JIS (CP932) 完全対応**: 文字化けを起こさず ANSI/Shift-JIS 形式で設定を読み書きします。
- **ファイル排他制御 (FileShare.Read)**: エディタで開いている間、メモ帳等による他プロセスからの誤上書きや不整合を防止（ATS 本体からの参照・読み込みは許可）。
- **[ATC] セクション＆パラメータ制御**:
  - `ATCFail` (故障有効/無効) と UI 連動（入力欄の自動グレーアウト）
  - `FailParameter` (故障率) の範囲チェック＆自動補正 (0.0000001% 未満の自動リセット)
- **[VehicleSpec] セクション＆パラメータ制御**:
  - `MaxBrake` 車両ごとのブレーキ段数の設定
  - `AtcType` ATCタイプの設定。WS-ATCとCS-ATCの両方とWS-ATCだけとCS-ATCだけに制限することができます。
- **多言語対応 (日本語 / 英語)**: ワンクリックで UI やバルーンチップの言語を切り替え可能。
- **堅牢なエラーハンドリング & WMI ログ記録**: 起動環境チェック失敗時は `Erroroutput.log` に詳細なシステム診断ログを出力。

## 🖥 動作環境

- **OS**: Windows 11 24H2 (Build 26100) 以降 (64-bit 専用)
- **アーキテクチャ**: x64

## 🚀 使い方

1. `Release` フォルダ内の `ATSPluginIniEditor.exe` を実行します。
2. [ファイル] ＞ [開く] から対象の `settings.ini` を読み込みます。
3. 必要なパラメータを変更し、[保存] を実行します。

## 🛠 開発環境

- **Language**: C# / .NET 10 (Windows Forms)
- **IDE**: Visual Studio
- **Publish Mode**: Self-contained (x64 Single File)