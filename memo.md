* l => mrb に変換
* paramsに対応

* TypeCache, ObjectCacheをMrbStateごとに独立させる
* ガベージコレクション対策
* rubyのDictinay, ArrayなどをC#内で便利に使えるようにする

? ArrayをC#オブジェクトのまま返すか, RubyのArrayに変換するか・・・
? Value の継承を検討する(ArrayValue, DictionaryValue ...)

* ConverterをConverter,Utilに分割

* 例外まわりを整理する(C#の例外をrubyで補足、rubyの例外をC#で補足)
* GCまわりをちゃんとする
* generic対応 => Genericクラスだけ対応。Generic関数は無理か

* enumに対応
* delegate対応
* eventに対応
* directFuncは消したけど、なんだっけ？
* [], []=に対応
* 拡張メソッドに対応 => いらん？
* ref, out, inに対応 => いらん？
* 予約語の回避(CodeGenerator.NormalName)
* Fiberのテスト
* bindingが対応する型を増やす
* 複数のMrbStateを使えるようにする
* CodeGenしなくても、リフレクションで触れる対応
* preserve(ビルド時にUnityにコードを消されないこと)の対応

## 引数の数

拡張メソッドはselfがあるので、引数が１つずれる

paramsの種類には対応しない（大変なので）

overloadは、ツリー状に判断する？ => 速度的には有効なので、そのうち最適化する

```
(int) (int,int) (int,string) (string) (int, params[])

int -> int
    -> string
    -> params
string
```

reflectionでやっちゃうのもよい？

## generic

genericと拡張メソッド、overloadは、すべて、同時に存在しうる・・・
拡張メソッドと通常メソッドが被った場合、通常メソッド(generic含む)が優先される。ただし、overloadはできる
genericと通常メソッドは、通常メソッドが優先される

通常メソッド > generic > 拡張 > 拡張generic

関数コード作成のときに、
１つのとき
　引数の数のチェック(+paramsの処理)
　関数本体
2つ以上の時
　引数のマッチング(+paramsの処理)
　xN
    関数本体


# OLD TODO

- extension methodに対応
- overload対応 => とりあえず、一番基本的な感じで
- RuntimeClassDescをつくって、実行時に順番を管理するようにする
- FindByTypeで、ないものは足すように
- a*の番号を1はじまりから、0はじまりにする
- CodeGenのコード整理: overload, 拡張メソッドなどの対応をしやすくするために、一度独自のクラスにクラスの情報をいれる
- ここらで一回リファクタか？
- 引数のarray対応 => 一部の型のみ
- デフォルト引数に対応 =>いまは、一部の型のみ
- 引数がDLL.MRB_ARGS_OPT(4)固定になってるのを修正
- スタックトレースをだせるようにする
- Value classがmrbを持つように（いちいち、mrbを渡さなくて済むように）
- module, classをリストアップして、先に作るようにする
- 継承
- Send()がエラーを返した時の処理をちゃんとする
- mrb_funcall_argvが例外をabort()(==C#の例外)で処理するように変更
- 名前をスネークケースに
- 継承が動いていないのを修正
- mrb_load_string()がエラー時にnilを返すのが困る。raiseしてほしい
