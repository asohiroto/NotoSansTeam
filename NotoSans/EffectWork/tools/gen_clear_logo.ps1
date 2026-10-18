# 「ゲームクリアー！」のロゴ画像（clear_logo.png）、文字だけの画像（clear_logo_text.png）、押し出した3Dモデル（clear_logo.obj）を作り直す（文字の部分は前へ飛び出す）
# 使い方：PowerShell でこのファイルを実行する（Blender などは不要。Windows の描画機能だけで作る）
# 文字・色・飾りを変えたいときは、ClearLogoGen.cs の DrawLogo の中を直す
# -FlipX を付けると、モデルの左右を反転して書き出す（Unity に取り込んだら文字が裏返っていたとき用）
param([switch]$FlipX = $true)
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$proj = Resolve-Path (Join-Path $here '..\..')
$font = Join-Path $proj 'Assets\Fonts\DelaGothicOne-Regular.ttf'
$out = Join-Path $proj 'Assets\GameObject\aso'
Add-Type -TypeDefinition ([System.IO.File]::ReadAllText((Join-Path $here 'ClearLogoGen.cs'))) -ReferencedAssemblies System.Drawing -Language CSharp -WarningAction SilentlyContinue
[ClearLogoGen]::Run($font, (Join-Path $out 'clear_logo.png'), (Join-Path $out 'clear_logo_text.png'), (Join-Path $out 'clear_logo.obj'), [bool]$FlipX)