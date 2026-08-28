---
name: add-diagnostic
description: EventSourceToolkit に新しい Roslyn 診断（Analyzer/Source Generator が報告する警告・エラーの DiagnosticDescriptor、または DiagnosticSuppressor が使う SuppressionDescriptor）を定義するための定型作業を一括で行う。診断側は ID の採番、AnalyzerReleases.Unshipped.md への登録、docs/diagnostics 配下の解説文書、全言語のリソース文字列、helpLinkUri 付きの DiagnosticDescriptor までを、抑制側は ID の採番と Justification リソース、SuppressionDescriptor までを、リポジトリの命名規則と文体に揃えて行う。「診断を追加したい」「アナライザーに新しい規則を追加したい」「新しい警告やエラーを出したい」「EST0xx を増やしたい」「この診断を抑制したい」「SuppressionDescriptor を追加したい」「DiagnosticSuppressor に規則を足したい」といった依頼のときは必ずこのスキルを使うこと。診断や抑制理由のタイトル・メッセージ・文言の文体を書く、または既存のものを見直すときの文体ガイドラインとしても参照する。
---

# 診断・抑制の追加

新しい診断や抑制を 1 つ追加するために触るファイルは複数箇所に散っている。どれか 1 つを忘れると、ビルド警告（RS2000 番台）、実行時の `KeyNotFoundException` や AD0001、リンク切れの helpLinkUri、あるいは「英語では出るが日本語では出ない」といった形で後から効いてくる。この手順はその取りこぼしを防ぐためにある。

**このスキルは診断・抑制を「定義」するところまでで終わる。** 診断を報告する、あるいは抑制を判定するチェック ロジックの実装は含まない。定義が済んだ時点でユーザーに報告し、ロジックの実装は改めて指示を仰ぐ。

## 0. 診断か抑制かを見分ける

会話から明らかでなければ最初に確認する。触るファイルも文体のルールも別物なので、以降はどちらか一方の章だけを読めばよい。

- **診断 (Diagnostic) を追加する** — Analyzer や Source Generator が新しく警告・エラーを報告できるようにする。「診断 (Diagnostic) を追加する」章に進む
- **抑制 (Suppression) を追加する** — 既存の診断（このリポジトリの EST0xx でも、コンパイラーや他のアナライザーが出すものでもよい）を、特定の状況で `DiagnosticSuppressor` に抑制させる。「抑制 (Suppression) を追加する」章に進む

抑制の場合、`AnalyzerReleases.Unshipped.md` への登録と `docs/diagnostics` の解説文書は書かない。抑制は新しい診断 ID を利用者に公開するものではなく、既存診断の追加情報でしかないため、これらのリリース追跡・利用者向け文書の対象外になる。

---

## 診断 (Diagnostic) を追加する

### 1. 必要な情報を集める

会話から読み取れないものだけを `AskUserQuestion` で確認する。すでに分かっていることを聞き直さない。

- **何を検出する規則か** — must / must not / should / should not のどれで言い切れるかまで具体化する。ここが曖昧なままだと ID 名も Title も決まらない
- **追加先プロジェクト** — `Sources/Analyzers`（IDE とビルドで常時報告される）か `Sources/SourceGenerators`（生成処理の中で報告される）か
- **Analyzers の場合、どのアナライザー クラスか** — `Sources/Analyzers/*.cs` を一覧して、既存クラスに足すか新規クラスを作るかを選ばせる
- **Severity** — 既存の診断はすべて `Error`。生成が成立しなくなる規則なら `Error`、生成はできるが望ましくないだけなら `Warning`（その場合 Title は should 系になる）

### 2. 現状を読む

先に読む。番号や並び順を推測で決めないこと。

| 目的 | ファイル |
|---|---|
| 採番済みの ID | `Sources/Common/DiagnosticIds.cs` |
| リリース追跡 | `Sources/{Analyzers,SourceGenerators}/AnalyzerReleases.Unshipped.md` と `.Shipped.md` |
| 解説文書 | `docs/diagnostics/README.md` と `docs/diagnostics/EST0xx.md` |
| 文字列リソース | `Sources/<Project>/Properties/Resources.resx` と `Resources.<culture>.resx` |
| helpLinkUri の生成 | `Sources/Common/DiagnosticHelpLinks.cs` |
| Analyzer の記述子 | 対象の `Sources/Analyzers/*Analyzer.cs` |
| Generator の記述子 | `Sources/SourceGenerators/Diagnostics/DiagnosticDescriptors.cs` |

### 3. ID を採番する

`Sources/Common/DiagnosticIds.cs` に定数を追加する。このクラスは `IncludeCommon=true` によって Analyzers と SourceGenerators の両方にコンパイルされるので、**番号はリポジトリ全体で一意**でなければならない。プロジェクトごとに別の連番を振ってはいけない。

- 番号は `EST` + 3 桁。既存の最大値 + 1
- 定数名は Title と同じ規範系の短文にする。`MustBe` / `MustNotBe` / `ShouldBe` / `ShouldNotBe` を含める
  - `EventSourceClassMustBePartialClass`
  - `EventSourceClassMustNotBeAbstract`
  - `EventSourceClassMustNotBeFileLocalClass`
- 定数は番号順に並べ、間に空行を 1 行入れる

この定数名がそのままリソース キーの接頭辞になるので、長くても説明的な名前を選ぶ。

### 4. AnalyzerReleases.Unshipped.md に登録する

追加先プロジェクトの `AnalyzerReleases.Unshipped.md` の `### New Rules` の表に 1 行足す。ID 順に並べ、既存行と同じ桁で揃える。

`Notes` は空にしない。英語の Title をそのまま入れる。この表はリリース ノートの素材であり、ID と Category だけでは何の規則か分からないので、resx を開かずに一覧できる状態にしておく。

```
Rule ID | Category | Severity | Notes
--------|----------|----------|--------------------
EST001  | General  | Error    | An event source class must be a partial class
```

`.Shipped.md` は触らない。リリース時に Unshipped から移す運用なので、ここで書くと二重登録になる。

登録漏れと登録過剰はどちらも警告になる。`DiagnosticDescriptor` を定義したのに表に書かなければ RS2000（`Rule '{0}' is not part of any analyzer release`）、逆に表に書いたのに対応する `DiagnosticDescriptor` がどこにもなければ RS2002（`Rule '{0}' is part of the next unshipped analyzer release, but is not a supported diagnostic for any analyzer`）が出る。手順 7 と必ず両方やる。

### 5. 解説文書を書く

**先に文書を書くと後が楽になる。** ここには字数制限がないので、規則の根拠・修正方法・抑制の可否を一度きちんと言葉にできる。Description は要するにこの文書の要約なので、逆順にやると同じことを 2 回考えることになる。

文書は英語で書く。2 ファイルに手を入れる。

#### `docs/diagnostics/EST0xx.md`（新規）

ファイル名は ID と完全に一致させる。helpLinkUri がこの名前から機械的に組み立てられるので、`EST006.md` 以外の名前にするとリンクが切れる。

見出しは既存の文書と同じ構成にする。`docs/diagnostics/EST004.md` が最も情報量の多い例なので、迷ったらそれを写す。

```markdown
# EST0xx: <Title と同じ規範系の短文>

| | |
|---|---|
| Rule ID | EST0xx |
| Category | General |
| Severity | Error |
| Reported by | `<アナライザー クラス名>` |

## Cause
<Message と同じ「現状」を、プレースホルダーを埋めた形で 1〜2 文で>

## Rule description
<なぜこの規則があるのか。コンパイラーや EventSource の挙動まで踏み込んでよい>

## How to fix violations
<具体的な修正方法。選択肢が複数あるならすべて挙げる>

## When to suppress
<抑制してよいか、抑制すると何が起きるか>

## Example
### Violation
### Fix
```

`## Example` のコードは実際にコンパイルできる形で書く。`using` を省略したり、`[Event]` を付けた partial メソッドを省いたりすると、読者がコピーしてもそのまま動かない。

#### `docs/diagnostics/README.md`（追記）

索引の表に 1 行足す。ID 順に並べる。`Title` 列は英語 Title をそのまま入れる。

### 6. 文字列リソースを追加する

追加先プロジェクトの `Properties` にある**すべての** resx に、`<定数名>Title` / `<定数名>Message` / `<定数名>Description` の 3 つを追加する。現在は中立（en-US）の `Resources.resx` と `Resources.ja.resx` の 2 ファイル。

`Microsoft.CodeAnalysis.ResxSourceGenerator` が `Resources` クラスを生成するが、生成元は**中立の resx だけ**である。`Resources.resx` への追加を忘れると `nameof(Resources.XxxTitle)` がコンパイル エラーになり、逆に `Resources.ja.resx` への追加を忘れても黙って英語が出るだけで気づけない。両方に入れる。

- キーの並び順は全ファイルで揃える。既存エントリの末尾（`</root>` の直前）に 3 つまとめて足すのが安全
- 書式は既存に合わせる。`<data name="..." xml:space="preserve">` + 2 スペース インデント、改行は LF

文言は後述の文体ガイドラインに従う。英語版は手順 5 の文書から抜き出せる。

### 7. DiagnosticDescriptor を定義する

helpLinkUri は必ず `DiagnosticHelpLinks.GetHelpLinkUri(DiagnosticIds.Xxx)` で組み立てる。URL を直書きすると、リポジトリの移動や docs の再配置で全箇所を直すことになる。

#### Analyzers の場合

対象アナライザー クラスに `private static readonly DiagnosticDescriptor` フィールドを追加し、**`SupportedDiagnostics` にも追加する**。`SupportedDiagnostics` に載っていない ID を `ReportDiagnostic` すると Roslyn がその報告を例外扱いにする（AD0001）。ビルドは通るのでコンパイル時には気づけない。

```csharp
private static readonly DiagnosticDescriptor EventSourceClassMustNotBeFileLocalClass = new(
    DiagnosticIds.EventSourceClassMustNotBeFileLocalClass,
    Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustNotBeFileLocalClassTitle)),
    Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustNotBeFileLocalClassMessage)),
    DiagnosticCategories.General,
    DiagnosticSeverity.Error,
    true,
    Resources.GetLocalizableResourceString(nameof(Resources.EventSourceClassMustNotBeFileLocalClassDescription)),
    DiagnosticHelpLinks.GetHelpLinkUri(DiagnosticIds.EventSourceClassMustNotBeFileLocalClass));
```

フィールドと `SupportedDiagnostics` の並び順は ID 順に揃える。`isEnabledByDefault` は既存に合わせて位置指定の `true` で書く。

#### SourceGenerators の場合

`Diagnostics/DiagnosticDescriptors.cs` に `public static readonly` フィールドを追加し、**`Descriptors` 辞書にもエントリを追加する**。辞書への追加を忘れると `GetDescriptor` が実行時に落ちる。文字列は `CreateString(nameof(...))` を使う（こちらのプロジェクトの慣習）。

### 8. 検証する

`dotnet build -t:Rebuild` を実行する。増分ビルドだと対象プロジェクトが再コンパイルされず警告が出ないため、警告数を根拠にするなら必ず `-t:Rebuild` を使う。

確認すること:

- 新たなエラー・警告が増えていないこと。特に RS1031〜RS1033（文体）、RS2000〜RS2002（リリース追跡）
- 変更前から出ている警告は変更前後で件数が同じであること。既存の警告を「自分のせい」と誤認しないよう、変更前の件数を先に把握しておく
- helpLinkUri のファイル名と `docs/diagnostics/` に置いたファイル名が一致していること

チェック ロジックを実装していない段階では、記述子が定義されているだけで報告はされない。これは正常な中間状態であり、警告にはならない。

---

## 抑制 (Suppression) を追加する

`SuppressionDescriptor` は診断そのものではなく、「特定の状況ではこの診断は誤検出だから消してよい」という追加情報でしかない。そのため診断本体（Title / Message、リリース追跡、docs）は増えず、触るファイルも少ない。

### 1. 必要な情報を集める

会話から読み取れないものだけを `AskUserQuestion` で確認する。

- **抑制対象の診断 ID (`SuppressedDiagnosticId`)** — このリポジトリの `DiagnosticIds` の定数か、コンパイラーや他のアナライザーが出す ID（`CS0169` など）かを確認する
- **どんな状況なら抑制してよいか** — 条件と理由をセットで言い切れるまで具体化する。ここが曖昧だと Justification も定数名も決まらない
- **対象の `DiagnosticSuppressor` クラス** — `Sources/Analyzers/*Suppressor.cs` を一覧して、既存クラスに足すか新規クラスを作るかを選ばせる

### 2. 現状を読む

| 目的 | ファイル |
|---|---|
| 採番済みの Suppression ID | `Sources/Analyzers/SuppressionIds.cs`（まだ存在しなければ手順 3 で新規作成） |
| 文字列リソース | `Sources/Analyzers/Properties/Resources.resx` と `Resources.ja.resx` |
| Suppressor の記述子 | 対象の `Sources/Analyzers/*Suppressor.cs` |

### 3. ID を採番する

`Sources/Analyzers/SuppressionIds.cs` に定数を追加する（ファイルがなければ新規作成する）。`DiagnosticIds.cs` と違い、このファイルは Analyzers プロジェクトの**ローカル**ファイルであり、`IncludeCommon` で SourceGenerators 側にはコンパイルされない。Suppression ID はプロジェクト外から参照される理由がないため、共有クラスに置く必要がない。

- 番号は `EST`（DiagnosticId と同じアルファベット 3 文字）+ `S` + 数字 3 桁。例: `ESTS001`
- 採番はこのファイル内で完結する連番。既存の最大値 + 1（ファイルが空、または存在しないなら 001 から）
- 定数名は「何をどんな状況で抑制するか」が分かる短い名前にする
- クラスと定数は `internal` にする。`DiagnosticIds` と違い外部プロジェクトから参照されないため `public` にする理由がない
- namespace は対象 Suppressor クラスと同じにする（例: `Aetos.EventSourceToolkit.Analyzers`）

```csharp
namespace Aetos.EventSourceToolkit.Analyzers;

internal static class SuppressionIds
{
    internal const string Xxx = "ESTS001";
}
```

### 4. 文字列リソースに Justification を追加する

対象プロジェクトの**すべての** resx (`Resources.resx` と `Resources.ja.resx`) に `<定数名>Justification` を **1 つだけ**追加する。

`DiagnosticDescriptor` と違い、`SuppressionDescriptor` が使うローカライズ文字列は Justification だけである。Title や Message に相当するものはない。追加のやり方自体は診断のときと同じで、中立 resx (`Resources.resx`) が `Resources` クラスの生成元なので、そちらへの追加漏れはコンパイル エラーとして気付けるが、`Resources.ja.resx` への追加漏れは黙って英語が出るだけなので、両方に入れる。

書式・並び順は診断のときと同じ（既存エントリの末尾に追加、2 スペース インデント、LF）。

### 5. SuppressionDescriptor を定義する

対象の Suppressor クラスに `private static readonly SuppressionDescriptor` フィールドを追加し、**`SupportedSuppressions` にも追加する**。`SupportedSuppressions` に載っていない ID を使おうとすると Roslyn が例外を投げる。`DiagnosticDescriptor` における `SupportedDiagnostics` と同じ注意点である。

```csharp
private static readonly SuppressionDescriptor Xxx = new(
    SuppressionIds.Xxx,
    DiagnosticIds.Yyy, // または "CS0169" のようなコンパイラー診断 ID のリテラル
    Resources.GetLocalizableResourceString(nameof(Resources.XxxJustification)));

public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } =
    ImmutableArray.Create(Xxx);
```

第 2 引数の `SuppressedDiagnosticId` には、このリポジトリの `DiagnosticIds` の定数か、コンパイラー/他アナライザーの ID をそのまま文字列リテラルで書く。`SuppressionDescriptor` に helpLinkUri は存在しないので設定しない。

### 6. 検証する

`dotnet build -t:Rebuild` を実行し、新たな警告・エラーが増えていないことを確認する。`ReportSuppressions` の実装はこのスキルの範囲外なので、記述子が定義されているだけでは実際には何も抑制されない。これは正常な中間状態であり、警告にはならない。

---

## 文体ガイドライン

診断（Title / Message / Description）と抑制（Justification）で役割が違う。同じ文を使い回さない。

| | 役割 | 句点 |
|---|---|---|
| ID 名・Title | 規範。must / must not / should / should not の短文 | **付けない**（Title は文中も含めピリオドを一切使わない） |
| Message | 現状。`{0}` に具体的な要素名が入る | 1 文なら**付けない**、複数文なら付ける |
| Description | 理由・修正方針・抑制の判断材料 | **付ける** |
| 解説文書 | Description の完全版。字数制限がない | 通常の文章として |
| Justification | なぜこの状況ならその診断を抑制してよいか | **付ける** |

句点の規則は Roslyn の RS1031〜RS1033 の要求そのものである（「診断タイトルには、ピリオドや改行記号を使用することも、先頭または末尾に空白文字を含めることもできません」「診断メッセージは……末尾にピリオドが付いていない 1 つの文または末尾にピリオドが付いた複数の文にする必要があります」「診断の説明は、句読点で終わる 1 つまたは複数の文にする必要があり……」）。このリポジトリの文字列は resx にあるため、これらの規則がビルド時に検査してくれるとは限らない。手で守る。

### ID 名と Title は規範（診断のみ）

「こうあるべき」を述べる。強制なら must / must not、推奨なら should / should not を使い、Severity と一致させる（`Error` なら must 系、`Warning` なら should 系）。`{0}` は含めない。

- `An event source class must be a partial class` / 「イベント ソース クラスは partial クラスでなければなりません」
- `An event source class must not be a file-local class` / 「イベント ソース クラスは file ローカル クラスであってはなりません」

日本語は英語の法助動詞に対応させる。「〜できません」は can not であって must not ではないので使わない。

| | 日本語 |
|---|---|
| must | 〜でなければなりません |
| must not | 〜であってはなりません |
| should | 〜すべきです |
| should not | 〜すべきではありません |

### Message は現状のみ（診断のみ）

いま何がどうなっているかを述べる。**理由も修正方針も書かない。** どちらも Description の役割であり、Message はエラー一覧に 1 行で並ぶものなので、短く事実だけを言うのが最も読みやすい。

- `The type '{0}' is not declared partial` / 「型 '{0}' が partial として宣言されていません」
- `The event source class '{0}' is declared abstract` / 「イベント ソース クラス '{0}' が abstract として宣言されています」

Title を言い換えただけの Message にはしない。`The type '{0}' must be declared partial` は Title と重複して情報量がゼロで、しかもエラー一覧では規範よりも「どこがどうなっているか」のほうが役に立つ。

### `{0}` の主語の選び方（診断のみ）

`{0}` に何が入り得るかで語を変える。

- 常にイベント ソース クラス自身が入る → `The event source class '{0}'` / 「イベント ソース クラス '{0}'」
- 包含型など他の型も入り得る → `The type '{0}'` / 「型 '{0}'」

EST001 と EST004 は包含型の名前も入るため後者を使っている。ここを間違えると、入れ子クラスで「イベント ソース クラス 'Outer'」という嘘のメッセージが出る。

### Description は理由・修正方針・抑制の判断材料（診断のみ）

3 つとも書く。順序もこの順にする。

1. **理由** — なぜこの規則があるのか。コンパイラーや `EventSource` の挙動に踏み込む。「規則だから」で終わらせると、読者は回避策を探し始める
2. **修正方針** — 具体的に何をどう書き換えるか。選択肢が複数あるならすべて挙げる
3. **抑制の判断材料** — 抑制してよいか、抑制すると何が起きるか。既存の診断はすべて「抑制してもコードは生成されない」ので抑制する意味がないが、それを明示しておかないと読者は試す

IDE のツールチップに出るので、段落は分けず 1 つの文章として書く。詳細を尽くしたい部分は解説文書に回す。

### Justification は抑制理由のみ（抑制のみ）

診断の Description と違って 3 段構成にはしない。すでに「抑制する」という結論の話をしているので、「抑制してよいか」を繰り返す必要がなく、**なぜこの状況なら安全に抑制できるか**だけを 1 文で書けばよい。

- `The unused field is intentionally kept for the generated code to reference.` / 「未使用のフィールドは、生成されたコードから参照されるため意図的に保持されています。」

IDE 上で `SuppressedDiagnosticId` の脇に表示される短い文なので、Description のように詳細を尽くす必要はない。詳しい経緯を残したい場合でも、このスキルの範囲外である `ReportSuppressions` の実装コメントに書けばよく、Justification 自体を長くしない。

### 日本語（共通）

- **です・ます調**
- カタカナの複合語は半角スペースで分かち書きする（`イベント ソース クラス`、`ソース ジェネレーター`、`コンパイラー`）
- C# のキーワードや修飾子は英小文字のまま、引用符で囲まない（`partial`、`file`、`abstract`、`internal`）。`抽象クラス` のような訳語は使わず `abstract クラス` と書く
- 型名・メンバー名は `'{0}'` のように単一引用符で囲む
- 完全修飾名を使うのは曖昧さがある場合だけ（`System.Diagnostics.Tracing.EventSource`）。属性は `GeneratedEventSourceAttribute` のように Attribute サフィックス込みで書く

### 英語（中立 / en-US、共通）

- Title は不定冠詞つきの一般文 — `An event source class must be a partial class`
- Message は定冠詞つきの具体文 — `The type '{0}' is not declared partial`
- 現在形・能動態
- 引用符とキーワードの扱いは日本語と同じ

## 完成の目安

### 診断の場合

EST004（`EventSourceClassMustNotBeFileLocalClass`）を追加したときに変更したファイル。これが 1 件あたりの完全な差分になる。

1. `Sources/Common/DiagnosticIds.cs` — 定数 1 つ
2. `Sources/Analyzers/AnalyzerReleases.Unshipped.md` — 表に 1 行
3. `docs/diagnostics/EST004.md` — 新規
4. `docs/diagnostics/README.md` — 索引に 1 行
5. `Sources/Analyzers/Properties/Resources.resx` — 3 エントリ
6. `Sources/Analyzers/Properties/Resources.ja.resx` — 3 エントリ
7. `Sources/Analyzers/EventSourceClassSignatureAnalyzer.cs` — 記述子フィールド 1 つ（helpLinkUri 込み）と `SupportedDiagnostics` の 1 行

### 抑制の場合

1 件あたりの完全な差分は次の 4 箇所のみ。診断本体の追加より小さい。

1. `Sources/Analyzers/SuppressionIds.cs` — 定数 1 つ（ファイルがなければ新規作成）
2. `Sources/Analyzers/Properties/Resources.resx` — 1 エントリ（Justification のみ）
3. `Sources/Analyzers/Properties/Resources.ja.resx` — 1 エントリ（Justification のみ）
4. 対象の `*Suppressor.cs` — 記述子フィールド 1 つと `SupportedSuppressions` の 1 行

`AnalyzerReleases.Unshipped.md` と `docs/diagnostics` は触らない。

## やらないこと

- **チェック ロジック / 抑制ロジックの実装** — `ReportDiagnostic` や `ReportSuppressions` の中身を書くのはこのスキルの範囲外。定義完了を報告して指示を仰ぐ
- **Code Fix の追加** — 同様に別作業
- **`AnalyzerReleases.Shipped.md` の編集** — リリース時の作業
- **抑制における `AnalyzerReleases.Unshipped.md` と `docs/diagnostics` への追記** — 抑制は既存診断の追加情報であり、新しい診断 ID を利用者に公開するものではないため対象外
- **コミット** — 明示的に指示されるまでしない
