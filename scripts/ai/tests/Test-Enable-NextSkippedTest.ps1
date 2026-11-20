#Requires -Version 7.6
$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot '../Enable-NextSkippedTest.ps1'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$fixtureRoot = Join-Path $repositoryRoot ('artifacts/skip-script-tests/' + [guid]::NewGuid().ToString('N'))
$utf8 = [Text.UTF8Encoding]::new($false, $true)
$passed = 0
$failed = 0

function Assert-Equal($Actual, $Expected) {
    if ($Actual -cne $Expected) { throw "期待値: <$Expected> / 実際: <$Actual>" }
}

function Write-Fixture([string] $Name, [string] $Content, [bool] $Bom = $false) {
    $path = Join-Path $script:caseRoot $Name
    [IO.Directory]::CreateDirectory((Split-Path $path -Parent)) | Out-Null
    [IO.File]::WriteAllText($path, $Content, [Text.UTF8Encoding]::new($Bom))
    return $path
}

function Assert-Fails([scriptblock] $Action, [string] $Message) {
    $caught = $false
    try { & $Action | Out-Null } catch {
        if ($_.Exception.Message -notlike $Message) { throw }
        $caught = $true
    }
    if (-not $caught) { throw '失敗せず終了しました。' }
}

function Test-Case([string] $Name, [scriptblock] $Action) {
    $script:caseRoot = Join-Path $fixtureRoot ([guid]::NewGuid().ToString('N'))
    [IO.Directory]::CreateDirectory($script:caseRoot) | Out-Null
    try {
        & $Action
        $script:passed++
        Write-Output "PASS: $Name"
    } catch {
        $script:failed++
        Write-Output "FAIL: $Name / $_"
    }
}

try {
    Test-Case '既存の角括弧形式もグループと番号として認識する' {
        $path = Write-Fixture 'a.cs' 'class T { [Fact(Skip = "[空白責務分離 04] 後")] void A() {} [Fact(Skip = "[空白責務分離 01] 先")] void B() {} }'
        Assert-Fails { & $scriptPath -Path $caseRoot } '*該当するSkip*'
        $result = & $scriptPath -Path $caseRoot -Group 空白責務分離
        Assert-Equal $result.Group '空白責務分離'
        Assert-Equal $result.Number 1
        Assert-Equal ([IO.File]::ReadAllText($path)) 'class T { [Fact(Skip = "[空白責務分離 04] 後")] void A() {} [Fact] void B() {} }'
    }
    Test-Case '角括弧形式の無グループ番号にも対応する' {
        $path = Write-Fixture 'a.cs' 'class T { [Theory] [InlineData(2, Skip = "[002] 後")] [InlineData(1, Skip = "[001] 先")] void A(int n) {} }'
        $result = & $scriptPath -Path $caseRoot
        Assert-Equal $result.Group ''
        Assert-Equal $result.Number 1
        Assert-Equal ([IO.File]::ReadAllText($path)) 'class T { [Theory] [InlineData(2, Skip = "[002] 後")] [InlineData(1)] void A(int n) {} }'
    }
    Test-Case '角括弧とコロン形式で重複する番号も止める' {
        $source = 'class T { [Fact(Skip = "[G 01] A")] void A() {} [Fact(Skip = "G 01: B")] void B() {} }'
        $path = Write-Fixture 'a.cs' $source
        Assert-Fails { & $scriptPath -Path $caseRoot -Group G } '*重複*'
        Assert-Equal ([IO.File]::ReadAllText($path)) $source
    }
    Test-Case '指定グループの番号順で一件だけ解除する' {
        $a = Write-Fixture 'a.cs' 'class T { [Fact(Skip = "Group 02: 後で")] void A() {} }'
        $b = Write-Fixture 'b.cs' 'class T { [Fact(Skip = "Group 01: 先に")] void B() {} }'
        $other = Write-Fixture 'c.cs' 'class T { [Fact(Skip = "Other 01: 別")] void C() {} }'
        $result = & $scriptPath -Path $caseRoot -Group Group
        Assert-Equal $result.File $b
        Assert-Equal $result.Number 1
        Assert-Equal $result.Changed $true
        Assert-Equal ([IO.File]::ReadAllText($b)) 'class T { [Fact] void B() {} }'
        Assert-Equal ([IO.File]::ReadAllText($a)) 'class T { [Fact(Skip = "Group 02: 後で")] void A() {} }'
        Assert-Equal ([IO.File]::ReadAllText($other)) 'class T { [Fact(Skip = "Other 01: 別")] void C() {} }'
    }
    Test-Case 'グループ省略時は無グループの番号付きから選ぶ' {
        $path = Write-Fixture 'a.cs' 'class T { [Fact(Skip = "Named 01: 別")] void A() {} [Fact(Skip = "02: 後")] void B() {} [Fact(Skip = "01: 先")] void C() {} [Fact(Skip = "従来の理由")] void D() {} }'
        $result = & $scriptPath -Path $caseRoot
        Assert-Equal $result.Group ''
        Assert-Equal $result.Number 1
        Assert-Equal ([IO.File]::ReadAllText($path)) 'class T { [Fact(Skip = "Named 01: 別")] void A() {} [Fact(Skip = "02: 後")] void B() {} [Fact] void C() {} [Fact(Skip = "従来の理由")] void D() {} }'
    }
    Test-Case '番号なしはファイル名と出現順で選び次回は次へ進む' {
        $b = Write-Fixture 'b.cs' 'class T { [Fact(Skip = "B")] void B() {} }'
        $a = Write-Fixture 'a.cs' 'class T { [Fact(Skip = "A1")] void A() {} [Fact(Skip = "A2")] void B() {} }'
        $first = & $scriptPath -Path $caseRoot
        Assert-Equal $first.Reason 'A1'
        $second = & $scriptPath -Path $caseRoot
        Assert-Equal $second.Reason 'A2'
        Assert-Equal ([IO.File]::ReadAllText($a)) 'class T { [Fact] void A() {} [Fact] void B() {} }'
        Assert-Equal ([IO.File]::ReadAllText($b)) 'class T { [Fact(Skip = "B")] void B() {} }'
    }
    Test-Case '同じグループで番号が重複したら変更しない' {
        $source = 'class T { [Fact(Skip = "G 01: A")] void A() {} [Fact(Skip = "G 01: B")] void B() {} }'
        $path = Write-Fixture 'a.cs' $source
        Assert-Fails { & $scriptPath -Path $caseRoot -Group G } '*重複*'
        Assert-Equal ([IO.File]::ReadAllText($path)) $source
    }
    Test-Case '対象なしは別グループを解除しない' {
        $source = 'class T { [Fact(Skip = "G 01: A")] void A() {} }'
        $path = Write-Fixture 'a.cs' $source
        Assert-Fails { & $scriptPath -Path $caseRoot } '*該当するSkip*'
        Assert-Fails { & $scriptPath -Path $caseRoot -Group Other } '*該当するSkip*'
        Assert-Equal ([IO.File]::ReadAllText($path)) $source
    }
    Test-Case 'WhatIfは選択結果だけ返して書き込まない' {
        $source = 'class T { [Fact(Skip = "01: A")] void A() {} }'
        $path = Write-Fixture 'a.cs' $source
        $result = & $scriptPath -Path $caseRoot -WhatIf
        Assert-Equal $result.Changed $false
        Assert-Equal $result.Number 1
        Assert-Equal ([IO.File]::ReadAllText($path)) $source
    }
    Test-Case 'InlineDataの一行だけ解除し他の引数を保つ' {
        $path = Write-Fixture 'a.cs' 'class T { [Theory] [InlineData(1, Skip = "G 01: A")] [InlineData(2, Skip = "G 02: B")] void A(int value) {} }'
        & $scriptPath -Path $caseRoot -Group G | Out-Null
        Assert-Equal ([IO.File]::ReadAllText($path)) 'class T { [Theory] [InlineData(1)] [InlineData(2, Skip = "G 02: B")] void A(int value) {} }'
    }
    Test-Case 'Theoryの他の名前付き引数を保つ' {
        $path = Write-Fixture 'a.cs' 'class T { [Theory(Skip = "A", DisplayName = "名前")] void A(int n) {} }'
        & $scriptPath -Path $caseRoot | Out-Null
        Assert-Equal ([IO.File]::ReadAllText($path)) 'class T { [Theory( DisplayName = "名前")] void A(int n) {} }'
    }
    Test-Case 'MemberDataの参照先を保つ' {
        $path = Write-Fixture 'a.cs' 'class T { [Theory] [MemberData(nameof(Data), Skip = "A")] void A(int n) {} }'
        & $scriptPath -Path $caseRoot | Out-Null
        Assert-Equal ([IO.File]::ReadAllText($path)) 'class T { [Theory] [MemberData(nameof(Data))] void A(int n) {} }'
    }
    Test-Case 'TheoryDataRowのSkipプロパティだけ解除する' {
        $path = Write-Fixture 'a.cs' 'class T { object Data => new TheoryDataRow<int>(1) { Skip = "G 01: A", Label = "保持" }; }'
        & $scriptPath -Path $caseRoot -Group G | Out-Null
        Assert-Equal ([IO.File]::ReadAllText($path)) 'class T { object Data => new TheoryDataRow<int>(1) {  Label = "保持" }; }'
    }
    Test-Case '型が明示されたデータ列のtarget-typed newにも対応する' {
        $path = Write-Fixture 'a.cs' 'class T { TheoryDataRow<int>[] Data => [new(1) { Skip = "01: A" }]; }'
        & $scriptPath -Path $caseRoot | Out-Null
        Assert-Equal ([IO.File]::ReadAllText($path)) 'class T { TheoryDataRow<int>[] Data => [new(1) {  }]; }'
    }
    Test-Case 'WithSkipだけ解除しデータと他のメタデータを保つ' {
        $path = Write-Fixture 'a.cs' 'class T { object Data => new TheoryDataRow<int>(1).WithSkip("G 01: A").WithLabel("保持"); }'
        & $scriptPath -Path $caseRoot -Group G | Out-Null
        Assert-Equal ([IO.File]::ReadAllText($path)) 'class T { object Data => new TheoryDataRow<int>(1).WithLabel("保持"); }'
    }
    Test-Case '文字列・コメント内とテスト以外のSkipは変更しない' {
        $source = @'
class T {
    string Text = "[Fact(Skip = \"偽物\")]";
    // [Fact(Skip = "偽物")]
    object Config => new Options { Skip = "設定" };
    [Fact(Skip = "本物")] void A() {}
}
'@
        $path = Write-Fixture 'a.cs' $source
        & $scriptPath -Path $caseRoot | Out-Null
        Assert-Equal ([IO.File]::ReadAllText($path)) ($source.Replace('[Fact(Skip = "本物")]', '[Fact]'))
    }
    Test-Case 'UTF-8 BOMとCRLFを維持する' {
        $source = "class T {`r`n    [Fact(Skip = @`"未対応`")]`r`n    void A() {}`r`n}`r`n"
        $path = Write-Fixture 'a.cs' $source $true
        & $scriptPath -Path $caseRoot | Out-Null
        $bytes = [IO.File]::ReadAllBytes($path)
        Assert-Equal ([Convert]::ToHexString($bytes[0..2])) 'EFBBBF'
        Assert-Equal ([IO.File]::ReadAllText($path)) ($source.Replace('[Fact(Skip = @"未対応")]', '[Fact]'))
    }
    Test-Case '文字列定数の参照は評価せず変更しない' {
        $source = 'class T { const string Reason = "A"; [Fact(Skip = Reason)] void A() {} }'
        $path = Write-Fixture 'a.cs' $source
        Assert-Fails { & $scriptPath -Path $caseRoot } '*文字列リテラル*'
        Assert-Equal ([IO.File]::ReadAllText($path)) $source
    }
    Test-Case '構文エラーがあれば変更しない' {
        $source = 'class T { [Fact(Skip = "A")] void A() {'
        $path = Write-Fixture 'a.cs' $source
        Assert-Fails { & $scriptPath -Path $caseRoot } '*C#構文エラー*'
        Assert-Equal ([IO.File]::ReadAllText($path)) $source
    }
    Test-Case '外側のTheoryでSkipされたデータの解除は止める' {
        $source = 'class T { [Theory(Skip = "親")] [InlineData(1, Skip = "G 01: A")] void A(int n) {} }'
        $path = Write-Fixture 'a.cs' $source
        Assert-Fails { & $scriptPath -Path $caseRoot -Group G } '*Theory全体*'
        Assert-Equal ([IO.File]::ReadAllText($path)) $source
    }
} finally {
    $artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts')) + [IO.Path]::DirectorySeparatorChar
    if ($fixtureRoot.StartsWith($artifactsRoot, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $fixtureRoot)) {
        Remove-Item -LiteralPath $fixtureRoot -Recurse -Force
    }
}
Write-Output "結果: $passed 成功 / $failed 失敗"
if ($failed -gt 0) { exit 1 }
